using System.Collections.Generic;
using Klei.CustomSettings;
using MiniBase.Model;
using MiniBase.Model.Enums;
using Newtonsoft.Json;
using PeterHan.PLib.Options;
using ProcGen;
using static MiniBase.Model.Profiles.MiniBaseBiomeProfiles;
using static MiniBase.Model.Profiles.MiniBaseCoreBiomeProfiles;

namespace MiniBase
{
    [ModInfo("")]
    [ConfigFile("config.json", true)]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class MiniBaseOptions
    {
        #region Properties
        
        #region Worldgen
        
        [Option("Western Feature", "The geyser, vent, or volcano on the left side of the map", WorldGenCategory)]
        [JsonProperty]
        public FeatureType FeatureWest { get; set; } = FeatureType.PollutedWater;

        [Option("Eastern Feature", "The geyser, vent, or volcano on the right side of the map", WorldGenCategory)]
        [JsonProperty]
        public FeatureType FeatureEast { get; set; } = FeatureType.RandomUseful;

        [Option("Southern Feature", "The geyser, vent, or volcano at the bottom of the map\nInaccessible until the abyssalite boundary is breached", WorldGenCategory)]
        [JsonProperty]
        public FeatureType FeatureSouth { get; set; } = FeatureType.OilReservoir;

        [Option("Main Biome", "The main biome of the map\nDetermines available resources, flora, and fauna", WorldGenCategory)]
        [JsonProperty]
        public BiomeType Biome { get; set; } = BiomeType.Temperate;

        [Option("Southern Biome", "The small biome at the bottom of the map\nProtected by a layer of abyssalite", WorldGenCategory)]
        [JsonProperty]
        public CoreType CoreBiome { get; set; } = CoreType.Magma;

        [Option("Resource Density", "Modifies the density of available resources", WorldGenCategory)]
        [JsonProperty]
        public ResourceModifier ResourceMod { get; set; } = ResourceModifier.Normal;

        [Option("Space Access", "Allows renewable resources to be collected from meteorites\nDoes not significantly increase the liveable area", WorldGenCategory)]
        [JsonProperty]
        public AccessType SpaceAccess { get; set; } = AccessType.Classic;

        [Option("Meteor showers", "The type of meteor shower that will fall on the minibase", WorldGenCategory)]
        [JsonProperty]
        public MeteorShowerType MeteorShower { get; set; } = MeteorShowerType.Mixed;

        [Option("Tunnel Access", "Adds tunnels for access to left and right sides", WorldGenCategory)]
        [JsonProperty]
        public TunnelAccessType TunnelAccess { get; set; } = TunnelAccessType.None;
        
        #endregion
        
        #region Size options
        
        [Option("Base Width", "The width of the liveable area\nMap Size must be set to 'Custom' for this to apply", SizeCategory)]
        [Limit(20, 100)]
        [JsonProperty]
        public int BaseWidth { get; set; } = 76;

        [Option("Base Height", "The height of the liveable area\nMap Size must be set to 'Custom' for this to apply", SizeCategory)]
        [Limit(20, 100)]
        [JsonProperty]
        public int BaseHeight { get; set; } = 49;
        
        [Option("Buildable space top", "The number of buildable tiles above the neutronium layer of the starting asteroid.", SizeCategory)]
        [Limit(0, 5)]
        [JsonProperty]
        public int BuildableSpaceTop { get; set; }
        
        #endregion
        
        #region Spaced out only options

        [Option("Space radiation intensity", "How many rads from the cosmic radiation", SpacedOutOnlyCategory)]
        [JsonProperty]
        public Intensity SpaceRads { get; set; } = Intensity.Med;
        
        [Option("Gilded asteroid distance", "Distance from the center of starmap the gilded asteroid is located (fullerene and gold)", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int GildedAsteroidDistance { get; set; } = 8;

        [Option("Resin POI", "A new type of space POI where resin can be harvested", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool ResinPoi { get; set; } = true;

        [Option("Resin POI distance", "Distance from the center of starmap the resin poi is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int ResinPoiDistance { get; set; } = 5;

        [Option("Niobium POI", "A new type of space POI where niobium can be harvested", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool NiobiumPoi { get; set; } = true;

        [Option("Niobium POI distance", "Distance from the center of starmap the niobium poi is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int NiobiumPoiDistance { get; set; } = 10;
        
        [Option("Oil mini-moonlet", "A neabry planetoid with oil wells and a volcano", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool OilMoonlet { get; set; }

        [Option("Oil mini-moonlet distance", "Distance from the center of starmap the oil mini-moonlet is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int OilMoonletDistance { get; set; } = 3;

        [Option("Resin mini-moonlet", "A planetoid with the Experiment 52B (resin tree)", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool ResinMoonlet { get; set; }

        [Option("Resin mini-moonlet distance", "Distance from the center of starmap the resin mini-moonlet is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int ResinMoonletDistance { get; set; } = 5;

        [Option("Niobium mini-moonlet", "A planetoid with niobium, and magma, lots of magma", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool NiobiumMoonlet { get; set; }

        [Option("Niobium mini-moonlet distance", "Distance from the center of starmap the niobium mini-moonlet is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int NiobiumMoonletDistance { get; set; } = 10;

        [Option("Frozen forest mini-moonlet", "A frigid location marked by inhospitably low temperatures throughout.", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool FrozenForestMoonlet { get; set; }
        
        [Option("Frozen forest mini-moonlet distance", "Distance from the center of the starmap the frozen forest mini-moonlet is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        public int FrozenForestMoonletDistance { get; set; }

        [Option("Badlands mini-moonlet", "A rocky and barren mini-moonlet with an overabundance of minerals.", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool BadlandsMoonlet { get; set; }

        [Option("Badlands mini-moonlet distance", "Distance from the center of the starmap the badlands mini-moonlet is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        public int BadlandsMoonletDistance { get; set; } = 5;

        [Option("Flipped mini-moonlet", "An asteroid in which the surface is molten hot lava and the core is livable.", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool FlippedMoonlet { get; set; }

        [Option("Flipped mini-moonlet distance", "Distance from the center of the starmap the flipped mini-moonlet is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        public int FlippedMoonletDistance { get; set; } = 5;

        [Option("Metallic swampy mini-moonlet", "A small swampy world with an abundance of renewable metal.", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool MetallicSwampyMoonlet { get; set; }

        [Option("Metallic swampy mini-moonlet distance", "Distance from the center of the starmap the metallic swampy mini-moonlet is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        public int MetallicSwampyMoonletDistance { get; set; } = 5;

        [Option("Radioactive ocean mini-moonlet", "An irradiated world with renewable water sources.", SpacedOutOnlyCategory)]
        [JsonProperty]
        public bool RadioactiveOceanMoonlet { get; set; }

        [Option("Radioactive ocean mini-moonlet distance", "Distance from the center of the starmap the radioactive ocean mini-moonlet is located", SpacedOutOnlyCategory)]
        [Limit(3, 11)]
        public int RadioactiveOceanMoonletDistance { get; set; } = 5;
        
        [Option("Teleporter placement", "Controls where teleporters are placed if enabled.", SpacedOutOnlyCategory)]
        [JsonProperty]
        public WarpPlacementType TeleporterPlacement { get; set; } = WarpPlacementType.Corners;
        
        #endregion
        
        #region Anytime options
        
        [Option("Care Package Timer (Cycles)", "Period of care package drops, in cycles\nLower values give more frequent drops", AnytimeCategory)]
        [Limit(1, 10)]
        [JsonProperty]
        public int CarePackageFrequency { get; set; } = 2;
        
        #endregion
        
        [JsonIgnore]
        public bool TeleportersEnabled =>
            OilMoonlet &&
            CustomGameSettings.Instance
                .GetCurrentQualitySetting(CustomGameSettingConfigs.Teleporters).id == "Enabled";

        #endregion
        
        #region Constants
        
        /// <summary>Thickness of the neutronium border.</summary>
        public const int BorderSize = 3;
        /// <summary>Non-colonizable margin at the top of the map.</summary>
        public const int TopMargin = 3;
        /// <summary>Margin at the top of the map that can be built in or be used to land rockets.</summary>
        public const int ColonizableExtraMargin = 8;
        public const int CornerSize = 7;
        public const int DiagonalBorderSize = 4;
        public const int SpaceAccessSize = 8;
        public const int SideAccessSize = 5;
        public const int CoreMin = 0;
        public const int CoreDeviation = 3;
        public const int CoreBorder = 3;
        
        #endregion

        #region Debug
        [JsonProperty]
        public bool DebugMode;
        [JsonProperty]
        public bool FastImmigration;
        [JsonProperty]
        public bool SkipLiveableArea;
        #endregion
        
        #region Methods

        public Vector2I GetWorldSize(Moonlet type)
        {
            if (type == Moonlet.Start)
            {
                return new Vector2I(
                    BaseWidth + (2 * BorderSize) + 36,
                    BaseHeight + (2 * BorderSize) + TopMargin + ColonizableExtraMargin);
            }
            
            return new Vector2I(
                50 + (2 * BorderSize),
                60 + (2 * BorderSize) + TopMargin + ColonizableExtraMargin);
        }

        public Vector2I GetBaseSize(Moonlet type)
        {
            if (type == Moonlet.Start)
            {
                return new Vector2I(BaseWidth, BaseHeight);
            }
            
            var worldSize = GetWorldSize(type);
            return new Vector2I(
                worldSize.x - 2 * BorderSize,
                worldSize.y - 2 * BorderSize - TopMargin - ColonizableExtraMargin);
        }

        public MiniBaseBiomeProfile GetBiome()
        {
            return _biomeTypeMap.TryGetValue(Biome, out var profile) ? profile : TemperateProfile;
        }

        public bool HasCore()
        {
            return CoreBiome != CoreType.None;
        }

        public MiniBaseBiomeProfile GetCoreBiome()
        {
            return HasCore() ? _coreTypeMap[CoreBiome] : MagmaCoreProfile;
        }

        public float GetResourceModifier()
        {
            switch(ResourceMod)
            {
                case ResourceModifier.Poor: return 0.5f;
                case ResourceModifier.Rich: return 1.5f;
                default: return 1.0f;
            }
        }

        public void Configure(ProcGen.World world)
        {
            // configure cosmic radiation intensity
            switch (SpaceRads)
            {
                case Intensity.VeryVeryLow:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_VERY_LOW);
                    break;
                case Intensity.VeryLow:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_LOW);
                    break;
                case Intensity.Low:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.LOW);
                    break;
                case Intensity.MedLow:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.MED_LOW);
                    break;
                case Intensity.Med:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.MED);
                    break;
                case Intensity.MedHigh:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.MED_HIGH);
                    break;
                case Intensity.High:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.HIGH);
                    break;
                case Intensity.VeryHigh:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_HIGH);
                    break;
                case Intensity.VeryVeryHigh:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_VERY_HIGH);
                    break;
                case Intensity.None:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.NONE);
                    break;
            }
            
            // configure meteor showers
            world.seasons.Clear();
            switch (MeteorShower)
            {
                case MeteorShowerType.Classic:
                    world.seasons.Add("VanillaMinibaseShower");
                    break;
                case MeteorShowerType.SpacedOut:
                    world.seasons.Add("ClassicStyleStartMeteorShowers");
                    break;
                case MeteorShowerType.Radioactive:
                    world.seasons.Add("MiniRadioactiveOceanMeteorShowers");
                    break;
                case MeteorShowerType.Fullerene:
                    world.seasons.Add("FullereneMinibaseShower");
                    break;
                default:
                    world.seasons.Add("MixedMinibaseShower");
                    break;
            }
        }

        /// <summary>
        /// Determines whether a world should be spawned and at what distance.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="distance">Distance from the cluster center at which to spawn this world.</param>
        /// <returns>True if the world should be spawned, false otherwise.</returns>
        public bool GetWorldParameters(WorldPlacement world, out MinMaxI distance)
        {
            switch (world.world)
            {
                case MoonletData.DlcSecondMap:
                    distance = new MinMaxI(OilMoonletDistance, OilMoonletDistance);
                    return OilMoonlet;
                case MoonletData.DlcMarshyMap:
                    distance = new MinMaxI(ResinMoonletDistance, ResinMoonletDistance);
                    return ResinMoonlet;
                case MoonletData.DlcNiobiumMap:
                    distance = new MinMaxI(NiobiumMoonletDistance, NiobiumMoonletDistance);
                    return NiobiumMoonlet;
                case MoonletData.DlcFrozenForestMap:
                    distance = new MinMaxI(FrozenForestMoonletDistance, FrozenForestMoonletDistance);
                    return FrozenForestMoonlet;
                case MoonletData.DlcBadlandsMap:
                    distance = new MinMaxI(BadlandsMoonletDistance, BadlandsMoonletDistance);
                    return BadlandsMoonlet;
                case MoonletData.DlcFlippedMap:
                    distance = new MinMaxI(FlippedMoonletDistance, FlippedMoonletDistance);
                    return FlippedMoonlet;
                case MoonletData.DlcMetallicSwampyMap:
                    distance = new MinMaxI(MetallicSwampyMoonletDistance, MetallicSwampyMoonletDistance);
                    return MetallicSwampyMoonlet;
                case MoonletData.DlcRadioactiveOceanMap:
                    distance = new MinMaxI(RadioactiveOceanMoonletDistance, RadioactiveOceanMoonletDistance);
                    return RadioactiveOceanMoonlet;
                default: distance = world.allowedRings; return true;
            }
        }
        
        #endregion
        
        #region Static functions
        
        public static void Reload()
        {
            _instance = POptions.ReadSettings<MiniBaseOptions>() ?? new MiniBaseOptions();
        }
        
        #endregion
        
        #region Dictionaries

        private static Dictionary<BiomeType, MiniBaseBiomeProfile> _biomeTypeMap = new Dictionary<BiomeType, MiniBaseBiomeProfile>()
        {
            { BiomeType.Temperate, TemperateProfile },
            { BiomeType.Forest, ForestProfile },
            { BiomeType.Swamp, SwampProfile },
            { BiomeType.Frozen, FrozenProfile },
            { BiomeType.Desert, DesertProfile },
            { BiomeType.Barren, BarrenProfile },
            { BiomeType.Strange, StrangeProfile },
            { BiomeType.DeepEssence, DeepEssenceProfile },
        };

        private static Dictionary<CoreType, MiniBaseBiomeProfile> _coreTypeMap = new Dictionary<CoreType, MiniBaseBiomeProfile>()
        {
            { CoreType.Magma, MagmaCoreProfile },
            { CoreType.Ocean, OceanCoreProfile },
            { CoreType.Frozen, FrozenCoreProfile },
            { CoreType.Oil, OilCoreProfile },
            { CoreType.Metal, MetalCoreProfile },
            { CoreType.Fertile, FertileCoreProfile },
            { CoreType.Boneyard, BoneyardCoreProfile },
            { CoreType.Aesthetic, AestheticCoreProfile },
            { CoreType.Pearl, PearlCoreProfile },
            { CoreType.Radioactive, RadioactiveCoreProfile },
        };
        
        public static Dictionary<FeatureType, string> GeyserDictionary = new Dictionary<FeatureType, string>()
        {
            { FeatureType.WarmWater, "GeyserGeneric_" + GeyserGenericConfig.HotWater },
            { FeatureType.SaltWater, "GeyserGeneric_" + GeyserGenericConfig.SaltWater },
            { FeatureType.SlushSaltWater, "GeyserGeneric_" + GeyserGenericConfig.SlushSaltWater },
            { FeatureType.PollutedWater, "GeyserGeneric_" + GeyserGenericConfig.FilthyWater },
            { FeatureType.CoolSlush, "GeyserGeneric_" + GeyserGenericConfig.SlushWater },
            { FeatureType.CoolSteam, "GeyserGeneric_" + GeyserGenericConfig.Steam },
            { FeatureType.HotSteam, "GeyserGeneric_" + GeyserGenericConfig.HotSteam },
            { FeatureType.NaturalGas, "GeyserGeneric_" + GeyserGenericConfig.Methane },
            { FeatureType.Hydrogen, "GeyserGeneric_" + GeyserGenericConfig.HotHydrogen },
            { FeatureType.OilFissure, "GeyserGeneric_" + GeyserGenericConfig.OilDrip },
            { FeatureType.OilReservoir, "OilWell" },
            { FeatureType.SmallVolcano, "GeyserGeneric_" + GeyserGenericConfig.SmallVolcano },
            { FeatureType.Volcano, "GeyserGeneric_" + GeyserGenericConfig.BigVolcano },
            { FeatureType.Copper, "GeyserGeneric_" + GeyserGenericConfig.MoltenCopper },
            { FeatureType.Gold, "GeyserGeneric_" + GeyserGenericConfig.MoltenGold },
            { FeatureType.Iron, "GeyserGeneric_" + GeyserGenericConfig.MoltenIron },
            { FeatureType.Cobalt, "GeyserGeneric_" + GeyserGenericConfig.MoltenCobalt },
            { FeatureType.Aluminum, "GeyserGeneric_" + GeyserGenericConfig.MoltenAluminum },
            { FeatureType.Tungsten, "GeyserGeneric_" + GeyserGenericConfig.MoltenTungsten },
            { FeatureType.Niobium, "GeyserGeneric_" + GeyserGenericConfig.MoltenNiobium },
            { FeatureType.Sulfur, "GeyserGeneric_" + GeyserGenericConfig.LiquidSulfur },
            { FeatureType.ColdCo2, "GeyserGeneric_" + GeyserGenericConfig.LiquidCO2 },
            { FeatureType.HotCo2, "GeyserGeneric_" + GeyserGenericConfig.HotCO2 },
            { FeatureType.InfectedPo2, "GeyserGeneric_" + GeyserGenericConfig.SlimyPO2 },
            { FeatureType.HotPo2, "GeyserGeneric_" + GeyserGenericConfig.HotPO2 },
            { FeatureType.Chlorine, "GeyserGeneric_" + GeyserGenericConfig.ChlorineGas },
        };

        public static FeatureType[] RandomWaterFeatures =
        {
            FeatureType.WarmWater,
            FeatureType.SaltWater,
            FeatureType.PollutedWater,
            FeatureType.CoolSlush,
            FeatureType.SlushSaltWater,
        };

        public static FeatureType[] RandomUsefulFeatures =
        {
            FeatureType.WarmWater,
            FeatureType.SaltWater,
            FeatureType.SlushSaltWater,
            FeatureType.PollutedWater,
            FeatureType.CoolSlush,
            FeatureType.CoolSteam,
            FeatureType.HotSteam,
            FeatureType.NaturalGas,
            FeatureType.Hydrogen,
            FeatureType.OilFissure,
            FeatureType.OilReservoir,
        };

        public static FeatureType[] RandomMagmaVolcanoFeatures =
        {
            FeatureType.SmallVolcano,
            FeatureType.Volcano,
        };

        public static FeatureType[] RandomVolcanoFeatures =
        {
            FeatureType.SmallVolcano,
            FeatureType.Volcano,
            FeatureType.Copper,
            FeatureType.Gold,
            FeatureType.Iron,
            FeatureType.Cobalt,
            FeatureType.Aluminum,
            FeatureType.Tungsten,
        };

        public static FeatureType[] RandomMetalVolcanoFeatures =
        {
            FeatureType.Copper,
            FeatureType.Gold,
            FeatureType.Iron,
            FeatureType.Cobalt,
            FeatureType.Aluminum,
            FeatureType.Tungsten,
        };
        
        #endregion
        
        #region Singleton implementation
        public static MiniBaseOptions Instance
        {
            get
            {
                if (_instance == null)
                {
                    Reload();   
                }
                return _instance;
            }
        }
        private static MiniBaseOptions _instance;
        #endregion
        
        #region Fields
        private const string SizeCategory = "1. Change the size of the liveable area.";
        private const string WorldGenCategory = "2. Map generation";
        private const string AnytimeCategory = "3. These options may be changed at any time";
        private const string SpacedOutOnlyCategory = "4. For Spaced Out! only.";
        #endregion
    }
}
