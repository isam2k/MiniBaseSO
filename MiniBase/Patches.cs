using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Database;
using HarmonyLib;
using Klei;
using Klei.AI;
using KMod;
using ProcGen;
using ProcGenGame;
using static MiniBase.MiniBaseConfig;
using static MiniBase.MiniBaseUtils;

namespace MiniBase
{
    internal class Patches
    {
        public static List<WorldPlacement> DefaultWorldPlacements = null;
        public static List<SpaceMapPOIPlacement> DefaultPOIPlacements = null;
        
        // Reload mod options at asteroid select screen, before world gen happens
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
                var minibaseWorld = SettingsCache.worlds.worldCache["worlds/MiniBase"];
                var baseSize = MiniBaseOptions.Instance.GetBaseSize();
                Traverse.Create(minibaseWorld).Property("worldsize").SetValue(new Vector2I(baseSize.x + 2 * BorderSize, baseSize.y + 2 * BorderSize + TopMargin));
            }

            private static void Expansion1OnSpawn()
            {
                const string miniBasePath = "expansion1::worlds/MiniBase";
                const string babyOilPath = "expansion1::worlds/BabyOilyMoonlet";
                const string babyMarshyPath = "expansion1::worlds/BabyMarshyMoonlet";
                const string babyNiobiumPath = "expansion1::worlds/BabyNiobiumMoonlet";
                
                var minibaseWorld = SettingsCache.worlds.worldCache[miniBasePath];
                var oilyMinibaseWorld = SettingsCache.worlds.worldCache[babyOilPath];
                var marshyMinibaseWorld = SettingsCache.worlds.worldCache[babyMarshyPath];
                var niobiumMinibaseWorld = SettingsCache.worlds.worldCache[babyNiobiumPath];

                var baseSize = MiniBaseOptions.Instance.GetBaseSize();
                var colonizableBaseSize = new Vector2I(50, 60);

                Traverse.Create(minibaseWorld).Property("worldsize").SetValue(new Vector2I(baseSize.x + 2 * BorderSize, baseSize.y + 2 * BorderSize + TopMargin));
                Traverse.Create(oilyMinibaseWorld).Property("worldsize").SetValue(new Vector2I(colonizableBaseSize.x + 2 * BorderSize, colonizableBaseSize.y + 2 * BorderSize + TopMargin + ColonizableExtraMargin));
                Traverse.Create(marshyMinibaseWorld).Property("worldsize").SetValue(new Vector2I(colonizableBaseSize.x + 2 * BorderSize, colonizableBaseSize.y + 2 * BorderSize + TopMargin + ColonizableExtraMargin));
                Traverse.Create(niobiumMinibaseWorld).Property("worldsize").SetValue(new Vector2I(colonizableBaseSize.x + 2 * BorderSize, colonizableBaseSize.y + 2 * BorderSize + TopMargin + ColonizableExtraMargin));

                minibaseWorld.seasons.Clear();
                
                switch (MiniBaseOptions.Instance.SpaceRads)
                {
                    case MiniBaseOptions.Intensity.VERY_VERY_LOW:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_VERY_LOW);
                        break;
                    case MiniBaseOptions.Intensity.VERY_LOW:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_LOW);
                        break;
                    case MiniBaseOptions.Intensity.LOW:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.LOW);
                        break;
                    case MiniBaseOptions.Intensity.MED_LOW:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.MED_LOW);
                        break;
                    case MiniBaseOptions.Intensity.MED:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.MED);
                        break;
                    case MiniBaseOptions.Intensity.MED_HIGH:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.MED_HIGH);
                        break;
                    case MiniBaseOptions.Intensity.HIGH:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.HIGH);
                        break;
                    case MiniBaseOptions.Intensity.VERY_HIGH:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_HIGH);
                        break;
                    case MiniBaseOptions.Intensity.VERY_VERY_HIGH:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_VERY_HIGH);
                        break;
                    case MiniBaseOptions.Intensity.NONE:
                        minibaseWorld.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.NONE);
                        break;
                }

                switch (MiniBaseOptions.Instance.MeteorShower)
                {
                    case MiniBaseOptions.MeteorShowerType.Classic:
                        minibaseWorld.seasons.Add("VanillaMinibaseShower");
                        break;
                    case MiniBaseOptions.MeteorShowerType.SpacedOut:
                        minibaseWorld.seasons.Add("ClassicStyleStartMeteorShowers");
                        break;
                    case MiniBaseOptions.MeteorShowerType.Radioactive:
                        minibaseWorld.seasons.Add("MiniRadioactiveOceanMeteorShowers");
                        break;
                    case MiniBaseOptions.MeteorShowerType.Fullerene:
                        minibaseWorld.seasons.Add("FullereneMinibaseShower");
                        break;
                    default:
                        minibaseWorld.seasons.Add("MixedMinibaseShower");
                        break;
                }

                Dictionary<string, ClusterLayout> clusterCache = SettingsCache.clusterLayouts.clusterCache;
                var minibase_layout = clusterCache["expansion1::clusters/MiniBase"];

                if (DefaultWorldPlacements == null)
                {
                    DefaultWorldPlacements = minibase_layout.worldPlacements;
                }

                if (DefaultPOIPlacements == null)
                {
                    DefaultPOIPlacements = minibase_layout.poiPlacements;
                }

                minibase_layout.worldPlacements = new List<WorldPlacement>();
                minibase_layout.poiPlacements = new List<SpaceMapPOIPlacement>(DefaultPOIPlacements);
                minibase_layout.startWorldIndex = 0;

                foreach (var world in DefaultWorldPlacements)
                {
                    switch (world.world)
                    {
                        case babyOilPath:
                            if (MiniBaseOptions.Instance.OilMoonlet)
                            {
                                world.allowedRings = new MinMaxI(MiniBaseOptions.Instance.OilMoonletDisance, MiniBaseOptions.Instance.OilMoonletDisance);
                            }
                            break;
                        case babyMarshyPath:
                            if (MiniBaseOptions.Instance.ResinMoonlet)
                            {
                                world.allowedRings = new MinMaxI(MiniBaseOptions.Instance.ResinMoonletDisance, MiniBaseOptions.Instance.ResinMoonletDisance);
                            }
                            break;
                        case babyNiobiumPath:
                            if (MiniBaseOptions.Instance.NiobiumMoonlet)
                            {
                                world.allowedRings = new MinMaxI(MiniBaseOptions.Instance.NiobiumMoonletDisance, MiniBaseOptions.Instance.NiobiumMoonletDisance);
                            }
                            break;
                    }
                    minibase_layout.worldPlacements.Add(world);
                }
                void AddPOI(string name, int distance)
                {
                    FileHandle f = new FileHandle();
                    var poi = YamlIO.Parse<SpaceMapPOIPlacement>($"pois:\n  - {name}\nnumToSpawn: 1\navoidClumping: true\nallowedRings:\n  min: {distance}\n  max: {distance}", f);
                    minibase_layout.poiPlacements.Insert(0, poi);
                };

                if (MiniBaseOptions.Instance.ResinPOI)
                {
                    AddPOI("HarvestableSpacePOI_ResinAsteroidField", MiniBaseOptions.Instance.ResinPOIDistance);
                }
                if (MiniBaseOptions.Instance.NiobiumPOI)
                {
                    AddPOI("HarvestableSpacePOI_NiobiumAsteroidField", MiniBaseOptions.Instance.NiobiumPOIDistance);
                }

                foreach (var poiPlacement in minibase_layout.poiPlacements.Where(poiPlacement => poiPlacement.pois.Count == 1 && poiPlacement.pois[0] == "HarvestableSpacePOI_GildedAsteroidField"))
                {
                    poiPlacement.allowedRings = new MinMaxI(MiniBaseOptions.Instance.GildedAsteroidDistance, MiniBaseOptions.Instance.GildedAsteroidDistance);
                    break;
                }
            }
        }

        // Reload mod options when game is reloaded from save
        [HarmonyPatch(typeof(Game), "OnPrefabInit")]
        public static class Game_OnPrefabInit_Patch
        {
            public static void Prefix()
            {
                MiniBaseOptions.Reload();
            }
        }

        // Reveal map on startup
        [HarmonyPatch(typeof(MinionSelectScreen), "OnProceed")]
        public static class MinionSelectScreen_OnProceed_Patch
        {
            public static void Postfix()
            {
                if (!IsMiniBaseCluster())
                {
                    return;
                }
                int radius = (int)(Math.Max(Grid.WidthInCells, Grid.HeightInCells) * 1.5f);
                GridVisibility.Reveal(0, 0, radius, radius - 1);
            }
        }
        
        [HarmonyPatch(typeof(ClusterPOIManager), "RegisterTemporalTear")]
        public static class ClusterPOIManager_RegisterTemporalTear_Patch
        {
            public static void Postfix(TemporalTear temporalTear, ClusterPOIManager __instance)
            {
                if (!IsMiniBaseCluster())
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

        // Immigration Speed
        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class Game_OnSpawn_Patch
        {
            public static void Postfix()
            {
                if (!IsMiniBaseCluster())
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

        // Add care package drops
        [HarmonyPatch(typeof(Immigration), "ConfigureCarePackages")]
        public static class Immigration_ConfigureCarePackages_Patch
        {
            public static void Postfix(ref CarePackageInfo[] ___carePackages)
            {
                if (!IsMiniBaseCluster())
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
                    packageList.Add(new CarePackageInfo(name, amount, cycle < 0 ? IsMiniBaseCluster : (Func<bool>)(() => CycleCondition(cycle) && IsMiniBaseCluster())));
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

        // Remove the need to discover items for them to be available in the printing pod
        [HarmonyPatch(typeof(Immigration), "DiscoveredCondition")]
        public static class Immigration_DiscoveredCondition_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (IsMiniBaseCluster())
                {
                    __result = true;
                }
            }
        }
        
        #endregion

        #region WorldGen

        // Bypass and rewrite world generation
        [HarmonyPatch(typeof(WorldGen), "RenderOffline")]
        public static class WorldGen_RenderOffline_Patch
        {
            public static bool AfterWorldGen = false;
            public static bool FoundTemporalTearOpener = false;

            public static bool Prefix(WorldGen __instance)
            {
                // Skip the original method if on minibase world
                return !IsMiniBaseWorld(__instance);
            }
            
            public static void Postfix(WorldGen __instance, ref bool __result, bool doSettle, BinaryWriter writer, ref Sim.Cell[] cells, ref Sim.DiseaseCell[] dc, int baseId, ref List<WorldTrait> placedStoryTraits, bool isStartingWorld)
            {
                if (IsMiniBaseWorld(__instance))
                {
                    __result = MiniBaseWorldGen.CreateWorld(__instance, writer, ref cells, ref dc, baseId,
                        ref placedStoryTraits, isStartingWorld);
                }

                if (!DlcManager.IsExpansion1Active() || !IsMiniBaseCluster())
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
            
            private static bool IsMiniBaseWorld(WorldGen instance) =>
                instance.Settings.world.filePath == "worlds/MiniBase" ||
                instance.Settings.world.filePath == "expansion1::worlds/MiniBase" ||
                instance.Settings.world.filePath == "expansion1::worlds/BabyOilyMoonlet" ||
                instance.Settings.world.filePath == "expansion1::worlds/BabyMarshyMoonlet" ||
                instance.Settings.world.filePath == "expansion1::worlds/BabyNiobiumMoonlet";
        }

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