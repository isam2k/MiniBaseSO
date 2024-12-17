using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Database;
using HarmonyLib;
using Klei;
using Klei.AI;
using KMod;
using MiniBase.Model;
using ProcGen;
using ProcGenGame;
using static MiniBase.MiniBaseConfig;

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
                Traverse.Create(minibaseWorld).Property("worldsize").SetValue(new Vector2I(baseSize.x + 2 * BorderSize, baseSize.y + 2 * BorderSize + TopMargin));
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
                    baseSize.x + (2 * BorderSize),
                    baseSize.y + (2 * BorderSize) + TopMargin);
                
                var colonizableBaseSize = new Vector2I(
                    50 + (2 * BorderSize), 
                    60 + (2 * BorderSize) + TopMargin + ColonizableExtraMargin);

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

                void AddPOI(string name, int distance)
                {
                    var poi = new SpaceMapPOIPlacement()
                    {
                        numToSpawn = 1,
                        avoidClumping = true,
                        allowedRings = new MinMaxI(distance, distance)
                    };
                    Traverse.Create(poi).Property("pois").SetValue(new List<string> { name });
                    cluster.poiPlacements.Insert(0, poi);
                };

                // spawn the poi for renewable resin
                if (MiniBaseOptions.Instance.ResinPOI)
                {
                    AddPOI("HarvestableSpacePOI_ResinAsteroidField", MiniBaseOptions.Instance.ResinPOIDistance);
                }
                
                // spawn the poi for renewable niobium
                if (MiniBaseOptions.Instance.NiobiumPOI)
                {
                    AddPOI("HarvestableSpacePOI_NiobiumAsteroidField", MiniBaseOptions.Instance.NiobiumPOIDistance);
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
        /// Immigration Speed.
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
        /// Add care package drops.
        /// </summary>
        [HarmonyPatch(typeof(Immigration), "ConfigureCarePackages")]
        public static class Immigration_ConfigureCarePackages_Patch
        {
            public static void Postfix(ref CarePackageInfo[] ___carePackages)
            {
                if (!MoonletData.IsMiniBaseCluster())
                {
                    return;
                }
                
                // Add new care packages
                var packageList = ___carePackages.ToList();
                void AddElement(SimHashes element, float amount, int cycle = -1)
                {
                    AddItem(ElementLoader.FindElementByHash(element).tag.ToString(), amount, cycle);
                }
                void AddItem(string name, float amount, int cycle = -1)
                {
                    packageList.Add(new CarePackageInfo(name, amount, cycle < 0 ? () => true : (Func<bool>)(() => CycleCondition(cycle))));
                }

                // Minerals
                AddElement(SimHashes.Granite, 2000f);
                AddElement(SimHashes.IgneousRock, 2000f);
                AddElement(SimHashes.Obsidian, 2000f, 24);
                AddElement(SimHashes.Salt, 2000f);
                AddElement(SimHashes.BleachStone, 2000f, 12);
                AddElement(SimHashes.Fossil, 1000f, 24);
                // Metals
                AddElement(SimHashes.IronOre, 1000f);
                AddElement(SimHashes.FoolsGold, 1000f, 12);
                AddElement(SimHashes.Wolframite, 500f, 24);
                AddElement(SimHashes.Lead, 1000f, 36);
                AddElement(SimHashes.AluminumOre, 500f, 24);
                AddElement(SimHashes.UraniumOre, 400f, 36);
                // Liquids
                AddElement(SimHashes.DirtyWater, 2000f, 12);
                AddElement(SimHashes.CrudeOil, 1000f, 24);
                AddElement(SimHashes.Petroleum, 1000f, 48);
                // Gases
                AddElement(SimHashes.ChlorineGas, 50f);
                AddElement(SimHashes.Methane, 50f, 24);
                // Plants
                AddItem("BasicSingleHarvestPlantSeed", 4f);             // Mealwood
                AddItem("SeaLettuceSeed", 3f);                          // Waterweed
                AddItem("SaltPlantSeed", 3f);                           // Dasha Saltvine
                AddItem("BulbPlantSeed", 3f);                           // Buddy Bud
                AddItem("ColdWheatSeed", 8f);                           // Sleet Wheat      TODO: solve invisible sleetwheat / nosh bean
                AddItem("BeanPlantSeed", 5f);                           // Nosh Bean
                AddItem("EvilFlowerSeed", 1f, 36);                 // Sporechid
                AddItem("WormPlantSeed", 3f);                           // Grubfruit Plant
                AddItem("SwampHarvestPlantSeed", 3f);                   // Bog Bucket Plant
                AddItem("CritterTrapPlantSeed", 1f, 36);           // Satturn Critter Trap
                // Critters
                AddItem("PacuEgg", 3f);                                 // Pacu
                AddItem("BeeBaby", 1f, 36);                        // Beetiny
                ___carePackages = packageList.ToArray();
            }

            private static bool CycleCondition(int cycle) => GameClock.Instance.GetCycle() >= cycle;
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
                
                __result = MiniBaseWorldGen.CreateWorld(__instance, writer, ref cells, ref dc, baseId,
                    ref placedStoryTraits, isStartingWorld);
                
                if (!DlcManager.IsExpansion1Active())
                {
                    return;
                }
                
                AfterWorldGen = true;
                
                if (FoundTemporalTearOpener || __instance.POISpawners == null)
                {
                    return;
                }
                
                foreach (var spawner in __instance.POISpawners.Where(s => s.container.buildings != null))
                {
                    if (spawner.container.buildings.Exists(b => b.id == "TemporalTearOpener"))
                    {
                        FoundTemporalTearOpener = true;
                        break;
                    }
                }
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

                void AddPrefab(HarvestablePOIConfig.HarvestablePOIParams poi_config)
                {
                    var prefab = HarvestablePOIConfig.CreateHarvestablePOI(poi_config.id, poi_config.anim, (string)Strings.Get(poi_config.nameStringKey), poi_config.descStringKey, poi_config.poiType.idHash, poi_config.poiType.canProvideArtifacts);
                    KPrefabID component = prefab.GetComponent<KPrefabID>();
                    component.prefabInitFn += new KPrefabID.PrefabFn(config.OnPrefabInit);
                    component.prefabSpawnFn += new KPrefabID.PrefabFn(config.OnSpawn);
                    Assets.AddPrefab(component);
                }
                AddPrefab(new HarvestablePOIConfig.HarvestablePOIParams("metallic_asteroid_field", new HarvestablePOIConfigurator.HarvestablePOIType("NiobiumAsteroidField", new Dictionary<SimHashes, float>()
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
                AddPrefab(new HarvestablePOIConfig.HarvestablePOIParams("gilded_asteroid_field", new HarvestablePOIConfigurator.HarvestablePOIType("ResinAsteroidField", new Dictionary<SimHashes, float>()
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