using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Database;
using HarmonyLib;
using Klei.AI;
using KMod;
using MiniBase.Model;
using ProcGen;
using ProcGenGame;

namespace MiniBase
{
    internal class Patches
    {
        /// <summary>
        /// Reload mod options at asteroid select screen, before world gen happens
        /// </summary>
        [HarmonyPatch(typeof(ColonyDestinationSelectScreen), "LaunchClicked")]
        public static class ColonyDestinationSelectScreen_LaunchClicked_Patch
        {
            public static void Prefix()
            {
                MiniBaseOptions.Reload();
            }
        }

        [HarmonyPatch(typeof(GameplaySeasons), "Expansion1Seasons")]
        public static class GameplaySeasons_Expansion1Seasons_Patch
        {
            public static void Postfix(ref GameplaySeasons __instance)
            {
                // Custom meteor showers
                var mixedMinibaseShower = Db.Get().GameplayEvents.Add((GameplayEvent)new MeteorShowerEvent(
                        "ClusterMinibaseShower",
                        150f,
                        4.5f,
                        TUNING.METEORS.BOMBARDMENT_OFF.NONE,
                        TUNING.METEORS.BOMBARDMENT_ON.UNLIMITED,
                        ClusterMapMeteorShowerConfig.GetFullID("HeavyDust"))
                    .AddMeteor(CopperCometConfig.ID, 1f)
                    .AddMeteor(IronCometConfig.ID, 1f)
                    .AddMeteor(GoldCometConfig.ID, 1f)
                    .AddMeteor(UraniumCometConfig.ID, 1f)
                    .AddMeteor(RockCometConfig.ID, 1f));

                var fullereneMinibaseShower = Db.Get().GameplayEvents.Add((GameplayEvent)new MeteorShowerEvent(
                        "FullereneMinibaseShower",
                        150f,
                        4.5f,
                        TUNING.METEORS.BOMBARDMENT_OFF.NONE,
                        TUNING.METEORS.BOMBARDMENT_ON.UNLIMITED,
                        ClusterMapMeteorShowerConfig.GetFullID("HeavyDust"))
                    .AddMeteor(FullereneCometConfig.ID, 1f)
                    .AddMeteor(RockCometConfig.ID, 1f));

                // Vanilla meteor event cannot be used with spaced out starmap, need to redefine them
                var vanillaMeteorShowerGoldEvent = Db.Get().GameplayEvents.Add(new MeteorShowerEvent(
                        "MiniBaseVanillaMeteorShowerGoldEvent",
                        3000f,
                        0.4f,
                        clusterMapMeteorShowerID: ClusterMapMeteorShowerConfig.GetFullID("Iron"),
                        secondsBombardmentOn: new MathUtil.MinMax(50f, 100f),
                        secondsBombardmentOff: new MathUtil.MinMax(800f, 1200f))
                    .AddMeteor(GoldCometConfig.ID, 2f)
                    .AddMeteor(RockCometConfig.ID, 0.5f)
                    .AddMeteor(DustCometConfig.ID, 5f));
                
                var vanillaMeteorShowerCopperEvent = Db.Get().GameplayEvents.Add(new MeteorShowerEvent(
                        "MiniBaseVanillaMeteorShowerCopperEvent",
                        4200f,
                        5.5f,
                        clusterMapMeteorShowerID: ClusterMapMeteorShowerConfig.GetFullID("Copper"),
                        secondsBombardmentOn: new MathUtil.MinMax(100f, 400f),
                        secondsBombardmentOff: new MathUtil.MinMax(300f, 1200f))
                    .AddMeteor(CopperCometConfig.ID, 1f)
                    .AddMeteor(RockCometConfig.ID, 1f));
                
                var vanillaMeteorShowerIronEvent = Db.Get().GameplayEvents.Add(new MeteorShowerEvent(
                        "MiniBaseVanillaMeteorShowerIronEvent",
                        6000f,
                        1.25f,
                        clusterMapMeteorShowerID: ClusterMapMeteorShowerConfig.GetFullID("Gold"),
                        secondsBombardmentOn: new MathUtil.MinMax(100f, 400f),
                        secondsBombardmentOff: new MathUtil.MinMax(300f, 1200f))
                    .AddMeteor(IronCometConfig.ID, 1f)
                    .AddMeteor(RockCometConfig.ID, 2f)
                    .AddMeteor(DustCometConfig.ID, 5f));

                __instance.Add(new MeteorShowerSeason(
                        "FullereneMinibaseShower",
                        GameplaySeason.Type.World,
                        "EXPANSION1_ID",
                        20f,
                        false,
                        startActive: true,
                        clusterTravelDuration: 6000f)
                    .AddEvent(fullereneMinibaseShower));
                
                __instance.Add(new MeteorShowerSeason(
                        "MixedMinibaseShower",
                        GameplaySeason.Type.World,
                        "EXPANSION1_ID",
                        20f,
                        false,
                        startActive: true,
                        clusterTravelDuration: 6000f)
                    .AddEvent(mixedMinibaseShower));
                
                __instance.Add(new MeteorShowerSeason(
                        "VanillaMinibaseShower",
                        GameplaySeason.Type.World,
                        "EXPANSION1_ID",
                        20f,
                        false,
                        startActive: true,
                        clusterTravelDuration: 6000f)
                    .AddEvent(vanillaMeteorShowerIronEvent)
                    .AddEvent(vanillaMeteorShowerGoldEvent)
                    .AddEvent(vanillaMeteorShowerCopperEvent));
            }
        }

        [HarmonyPatch(typeof(ColonyDestinationSelectScreen), "OnSpawn")]
        public static class ColonyDestinationSelectScreen_OnSpawn_Patch
        {
            public static void Prefix()
            {
                MiniBaseOptions.Reload();
                
                WorldGen_RenderOffline_Patch.FoundTemporalTearOpener = false;
                WorldGen_RenderOffline_Patch.AfterWorldGen = false;
                
                if (DlcManager.IsExpansion1Active())
                {
                    Expansion1OnSpawn();
                }
                else
                {
                    VanillaOnSpawn();
                }
            }

            private static void VanillaOnSpawn()
            {
                var minibaseWorld = SettingsCache.worlds.worldCache[MoonletData.VanillaStartMap];
                var baseSize = MiniBaseOptions.Instance.GetBaseSize();
                Traverse.Create(minibaseWorld).Property("worldsize").SetValue(new Vector2I(baseSize.x + 2 * MiniBaseOptions.BorderSize, baseSize.y + 2 * MiniBaseOptions.BorderSize + MiniBaseOptions.TopMargin));
            }

            private static void Expansion1OnSpawn()
            {
                var minibaseWorld = SettingsCache.worlds.worldCache[MoonletData.DlcStartMap];
                var oilyMinibaseWorld = SettingsCache.worlds.worldCache[MoonletData.DlcSecondMap];
                var marshyMinibaseWorld = SettingsCache.worlds.worldCache[MoonletData.DlcMarshyMap];
                var niobiumMinibaseWorld = SettingsCache.worlds.worldCache[MoonletData.DlcNiobiumMap];

                var baseSize = MiniBaseOptions.Instance.GetBaseSize();
                
                // we're getting a reference and should create a new
                // instance of the vector, otherwise we'll change the
                // default option sizes
                baseSize = new Vector2I(
                    baseSize.x + (2 * MiniBaseOptions.BorderSize),
                    baseSize.y + (2 * MiniBaseOptions.BorderSize) + MiniBaseOptions.TopMargin);
                
                var colonizableBaseSize = new Vector2I(
                    50 + (2 * MiniBaseOptions.BorderSize), 
                    60 + (2 * MiniBaseOptions.BorderSize) + MiniBaseOptions.TopMargin + MiniBaseOptions.ColonizableExtraMargin);

                Traverse.Create(minibaseWorld).Property("worldsize").SetValue(baseSize);
                Traverse.Create(oilyMinibaseWorld).Property("worldsize").SetValue(colonizableBaseSize);
                Traverse.Create(marshyMinibaseWorld).Property("worldsize").SetValue(colonizableBaseSize);
                Traverse.Create(niobiumMinibaseWorld).Property("worldsize").SetValue(colonizableBaseSize);
                MiniBaseOptions.Instance.Configure(minibaseWorld);
                
                var cluster = SettingsCache.clusterLayouts.clusterCache[MoonletData.MiniBaseCluster];
                
                cluster.startWorldIndex = 0;
                
                // get the list of worlds to spawn, which will also contain
                // some worlds the user can disallow in the options. process
                // the list according to the options and save the updated list
                var defaultPlacements = cluster.worldPlacements;
                cluster.worldPlacements = new List<WorldPlacement>();
                foreach (var world in defaultPlacements)
                {
                    if (!MiniBaseOptions.Instance.GetWorldParameters(world, out var distance))
                    {
                        continue;
                    }
                    world.allowedRings = distance;
                    cluster.worldPlacements.Add(world);
                }

                var addPoi = new Action<string, int>((name, distance) =>
                {
                    var poi = new SpaceMapPOIPlacement()
                    {
                        numToSpawn = 1,
                        avoidClumping = true,
                        allowedRings = new MinMaxI(distance, distance)
                    };
                    Traverse.Create(poi).Property("pois").SetValue(new List<string> { name });
                    cluster.poiPlacements.Insert(0, poi);
                });

                // spawn the poi for renewable resin
                if (MiniBaseOptions.Instance.ResinPOI)
                {
                    addPoi("HarvestableSpacePOI_ResinAsteroidField", MiniBaseOptions.Instance.ResinPOIDistance);
                }
                
                // spawn the poi for renewable niobium
                if (MiniBaseOptions.Instance.NiobiumPOI)
                {
                    addPoi("HarvestableSpacePOI_NiobiumAsteroidField", MiniBaseOptions.Instance.NiobiumPOIDistance);
                }
                
                // modify the distance to the fullerene poi
                var placement = cluster.poiPlacements
                    .FirstOrDefault(p => p.pois.Contains("HarvestableSpacePOI_GildedAsteroidField"));
                if (placement != null)
                {
                    placement.allowedRings = new MinMaxI(MiniBaseOptions.Instance.GildedAsteroidDistance, MiniBaseOptions.Instance.GildedAsteroidDistance);
                }
            }
        }

        /// <summary>
        /// Reload mod options when game is reloaded from save.
        /// </summary>
        [HarmonyPatch(typeof(Game), "OnPrefabInit")]
        public static class Game_OnPrefabInit_Patch
        {
            public static void Prefix()
            {
                MiniBaseOptions.Reload();
            }
        }

        /// <summary>
        /// Reveal the map on startup.
        /// </summary>
        [HarmonyPatch(typeof(MinionSelectScreen), "OnProceed")]
        public static class MinionSelectScreen_OnProceed_Patch
        {
            public static void Postfix()
            {
                if (!MoonletData.IsMiniBaseCluster())
                {
                    return;
                }
                int radius = (int)(Math.Max(Grid.WidthInCells, Grid.HeightInCells) * 1.5f);
                GridVisibility.Reveal(0, 0, radius, radius - 1);
            }
        }
        
        /// <summary>
        /// Open the temporal tear if no opener has been found in the cluster.
        /// </summary>
        [HarmonyPatch(typeof(ClusterPOIManager), "RegisterTemporalTear")]
        public static class ClusterPOIManager_RegisterTemporalTear_Patch
        {
            public static void Postfix(TemporalTear temporalTear, ClusterPOIManager __instance)
            {
                if (!MoonletData.IsMiniBaseCluster())
                {
                    return;
                }
                if (!temporalTear.IsOpen() &&
                    !WorldGen_RenderOffline_Patch.FoundTemporalTearOpener &&
                    WorldGen_RenderOffline_Patch.AfterWorldGen)
                {
                    temporalTear.Open();
                }
                WorldGen_RenderOffline_Patch.AfterWorldGen = false;
                WorldGen_RenderOffline_Patch.FoundTemporalTearOpener = false;
            }
        }
        
        /// <summary>
        /// Load translatable mod strings.
        /// </summary>
        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class Localization_Initialize_Patch
        {
            public static void Postfix() => Translate(typeof(STRINGS));

            private static void Translate(Type root)
            {
                // basic intended way to register strings, keeps namespace
                Localization.RegisterForTranslation(root);
                
                // load user created translation files
                LoadStrings();
                
                // register strings without namespace
                // because we already loaded user translations
                // custom languages will overwrite these
                LocString.CreateLocStringKeys(root, null);
                
                // creates template for users to edit
                Localization.GenerateStringsTemplate(root, System.IO.Path.Combine(Manager.GetDirectory(), "strings_templates"));
            }

            private static void LoadStrings()
            {
                string code = Localization.GetLocale()?.Code;
                if (code.IsNullOrWhiteSpace())
                {
                    return;
                }
                
                string path = System.IO.Path.Combine(ModUtils.Constants.ModPath, "translations",
                    code + ".po");
                if (File.Exists(path))
                {
                    Localization.OverloadStrings(Localization.LoadStringsFile(path, false));
                }
            }
        }
        
        #region CarePackages
        
        /// <summary>
        /// Patching the frequency at which care packages become available.
        /// </summary>
        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class Game_OnSpawn_Patch
        {
            public static void Postfix()
            {
                if (!MoonletData.IsMiniBaseCluster())
                {
                    return;
                }
                var immigration = Immigration.Instance;
                const float SecondsPerDay = 600f;
                float frequency = MiniBaseOptions.Instance.FastImmigration ? 10f : (MiniBaseOptions.Instance.CarePackageFrequency * SecondsPerDay);
                immigration.spawnInterval = new float[] { frequency, frequency };
                immigration.timeBeforeSpawn = Math.Min(frequency, immigration.timeBeforeSpawn);
            }
        }

        /// <summary>
        /// Remove the need to discover items for them to be available in the printing pod.
        /// </summary>
        [HarmonyPatch(typeof(Immigration), "DiscoveredCondition")]
        public static class Immigration_DiscoveredCondition_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (MoonletData.IsMiniBaseCluster())
                {
                    __result = true;
                }
            }
        }
        
        #endregion

        #region WorldGen

        /// <summary>
        /// Bypass and rewrite world generation.
        /// </summary>
        [HarmonyPatch(typeof(WorldGen), "RenderOffline")]
        public static class WorldGen_RenderOffline_Patch
        {
            public static bool AfterWorldGen = false;
            public static bool FoundTemporalTearOpener = false;

            public static bool Prefix(WorldGen __instance)
            {
                // Skip the original method if on minibase world
                return !MoonletData.IsMiniBaseWorld(__instance);
            }
            
            public static void Postfix(WorldGen __instance, ref bool __result, bool doSettle, BinaryWriter writer, ref Sim.Cell[] cells, ref Sim.DiseaseCell[] dc, int baseId, ref List<WorldTrait> placedStoryTraits, bool isStartingWorld)
            {
                if (!MoonletData.IsMiniBaseWorld(__instance))
                {
                    return;
                }

                try
                {
                    MiniBaseWorldGen.CreateWorld(__instance, writer, ref cells, ref dc, baseId,
                        ref placedStoryTraits, isStartingWorld);
                    __result = true;
                }
                catch (Exception e)
                {
                    __instance.ReportWorldGenError(e);
                    __result = false;
                    return;
                }
                
                if (!DlcManager.IsExpansion1Active())
                {
                    return;
                }
                
                AfterWorldGen = true;
                
                if (FoundTemporalTearOpener || __instance.POISpawners == null)
                {
                    return;
                }

                var spawner = __instance.POISpawners
                    .Where(s => s.container.buildings != null)
                    .FirstOrDefault(s => s.container.buildings.Exists(b => b.id == "TemporalTearOpener"));
                FoundTemporalTearOpener = spawner != null;
            }
        }

        /// <summary>
        /// Add additional harvestable space POI for spaced out to make niobium and resin renewable.
        /// </summary>
        [HarmonyPatch(typeof(EntityConfigManager), "RegisterEntities")]
        public static class EntityConfigManager_RegisterEntities_Patch
        {
            public static void Postfix(IMultiEntityConfig config)
            {
                if (!(config is HarvestablePOIConfig))
                {
                    return;
                }

                var addPrefab = new Action<HarvestablePOIConfig.HarvestablePOIParams>((poiConfig) =>
                {
                    var prefab = HarvestablePOIConfig.CreateHarvestablePOI(poiConfig.id, poiConfig.anim,
                        (string)Strings.Get(poiConfig.nameStringKey), poiConfig.descStringKey,
                        poiConfig.poiType.idHash, poiConfig.poiType.canProvideArtifacts);
                    KPrefabID component = prefab.GetComponent<KPrefabID>();
                    component.prefabInitFn += config.OnPrefabInit;
                    component.prefabSpawnFn += config.OnSpawn;
                    Assets.AddPrefab(component);
                });
                addPrefab(new HarvestablePOIConfig.HarvestablePOIParams("metallic_asteroid_field", new HarvestablePOIConfigurator.HarvestablePOIType("NiobiumAsteroidField", new Dictionary<SimHashes, float>()
                {
                    {
                        SimHashes.Obsidian,
                        5.0f
                    },
                    {
                        SimHashes.MoltenTungsten,
                        3.0f
                    },
                    {
                        SimHashes.Niobium,
                        0.03f
                    }
                })));
                addPrefab(new HarvestablePOIConfig.HarvestablePOIParams("gilded_asteroid_field", new HarvestablePOIConfigurator.HarvestablePOIType("ResinAsteroidField", new Dictionary<SimHashes, float>()
                {
                    {
                        SimHashes.Fossil,
                        0.2f
                    },
                    {
                        SimHashes.CrudeOil,
                        0.4f
                    },
                    {
                        SimHashes.Resin,
                        0.4f
                    }
                }, 56250, 56250, 30000, 30000)));
            }
        }
        
        #endregion
    }
}