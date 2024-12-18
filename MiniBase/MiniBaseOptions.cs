using System.Collections.Generic;
using MiniBase.Model;
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
        
        [Option("Western Feature", "The geyser, vent, or volcano on the left side of the map", WorldGenCategory)]
        [JsonProperty]
        public FeatureType FeatureWest { get; set; }

        [Option("Eastern Feature", "The geyser, vent, or volcano on the right side of the map", WorldGenCategory)]
        [JsonProperty]
        public FeatureType FeatureEast { get; set; }

        [Option("Southern Feature", "The geyser, vent, or volcano at the bottom of the map\nInaccessible until the abyssalite boundary is breached", WorldGenCategory)]
        [JsonProperty]
        public FeatureType FeatureSouth { get; set; }

        [Option("Main Biome", "The main biome of the map\nDetermines available resources, flora, and fauna", WorldGenCategory)]
        [JsonProperty]
        public BiomeType Biome { get; set; }

        [Option("Southern Biome", "The small biome at the bottom of the map\nProtected by a layer of abyssalite", WorldGenCategory)]
        [JsonProperty]
        public CoreType CoreBiome { get; set; }
        
        [Option("Resource Density", "Modifies the density of available resources", WorldGenCategory)]
        [JsonProperty]
        public ResourceModifier ResourceMod { get; set; }

        [Option("Space Access", "Allows renewable resources to be collected from meteorites\nDoes not significantly increase the liveable area", WorldGenCategory)]
        [JsonProperty]
        public AccessType SpaceAccess { get; set; }

        [Option("Map Size", "The size of the liveable area\nSelect 'Custom' to define a custom size", SizeCategory)]
        [JsonProperty]
        public BaseSize Size { get; set; }

        [Option("Space radiation intensity", "How many rads from the cosmic radiation", WorldGenCategory)]
        [JsonProperty]
        public Intensity SpaceRads { get; set; }

        [Option("Meteor showers", "The type of meteor shower that will fall on the minibase", WorldGenCategory)]
        [JsonProperty]
        public MeteorShowerType MeteorShower { get; set; }

        [Option("Gilded asteroid distance", "Distance from the center of starmap the gilded asteroid is located (fullerene and gold)", WorldGenCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int GildedAsteroidDistance { get; set; }

        [Option("Oil mini-moonlet", "A neabry planetoid with oil wells and a volcano", WorldGenCategory)]
        [JsonProperty]
        public bool OilMoonlet { get; set; }

        [Option("Oil mini-moonlet distance", "Distance from the center of starmap the oil mini-moonlet is located", WorldGenCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int OilMoonletDistance { get; set; }

        [Option("Resin mini-moonlet", "A planetoid with the Experiment 52B (resin tree)", WorldGenCategory)]
        [JsonProperty]
        public bool ResinMoonlet { get; set; }

        [Option("Resin mini-moonlet distance", "Distance from the center of starmap the resin mini-moonlet is located", WorldGenCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int ResinMoonletDistance { get; set; }

        [Option("Niobium mini-moonlet", "A planetoid with niobium, and magma, lots of magma", WorldGenCategory)]
        [JsonProperty]
        public bool NiobiumMoonlet { get; set; }

        [Option("Niobium mini-moonlet distance", "Distance from the center of starmap the niobium mini-moonlet is located", WorldGenCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int NiobiumMoonletDistance { get; set; }
        
        [Option("Frozen forest mini-moonlet", "A frigid location marked by inhospitably low temperatures throughout.", WorldGenCategory)]
        [JsonProperty]
        public bool FrozenForestMoonlet { get; set; }
        
        [Option("Frozen forest mini-moonlet distance", "Distance from the center of the starmap the frozen forest mini-moonlet is located", WorldGenCategory)]
        [Limit(3, 11)]
        public int FrozenForestMoonletDistance { get; set; }

        [Option("Badlands mini-moonlet", "A rocky and barren mini-moonlet with an overabundance of minerals.", WorldGenCategory)]
        [JsonProperty]
        public bool BadlandsMoonlet { get; set; }
        
        [Option("Badlands mini-moonlet distance", "Distance from the center of the starmap the badlands mini-moonlet is located", WorldGenCategory)]
        [Limit(3, 11)]
        public int BadlandsMoonletDistance { get; set; }
        
        [Option("Flipped mini-moonlet", "An asteroid in which the surface is molten hot lava and the core is livable.", WorldGenCategory)]
        [JsonProperty]
        public bool FlippedMoonlet { get; set; }
        
        [Option("Flipped mini-moonlet distance", "Distance from the center of the starmap the flipped mini-moonlet is located", WorldGenCategory)]
        [Limit(3, 11)]
        public int FlippedMoonletDistance { get; set; }
        
        [Option("Metallic swampy mini-moonlet", "A small swampy world with an abundance of renewable metal.", WorldGenCategory)]
        [JsonProperty]
        public bool MetallicSwampyMoonlet { get; set; }
        
        [Option("Metallic swampy mini-moonlet distance", "Distance from the center of the starmap the metallic swampy mini-moonlet is located", WorldGenCategory)]
        [Limit(3, 11)]
        public int MetallicSwampyMoonletDistance { get; set; }
        
        [Option("Radioactive ocean mini-moonlet", "An irradiated world with renewable water sources.", WorldGenCategory)]
        [JsonProperty]
        public bool RadioactiveOceanMoonlet { get; set; }
        
        [Option("Radioactive ocean mini-moonlet distance", "Distance from the center of the starmap the radioactive ocean mini-moonlet is located", WorldGenCategory)]
        [Limit(3, 11)]
        public int RadioactiveOceanMoonletDistance { get; set; }

        [Option("Resin POI", "A new type of space POI where resin can be harvested", WorldGenCategory)]
        [JsonProperty]
        public bool ResinPOI { get; set; }

        [Option("Resin POI distance", "Distance from the center of starmap the resin poi is located", WorldGenCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int ResinPOIDistance { get; set; }

        [Option("Niobium POI", "A new type of space POI where niobium can be harvested", WorldGenCategory)]
        [JsonProperty]
        public bool NiobiumPOI { get; set; }

        [Option("Niobium POI distance", "Distance from the center of starmap the niobium poi is located", WorldGenCategory)]
        [Limit(3, 11)]
        [JsonProperty]
        public int NiobiumPOIDistance { get; set; }

        [Option("Custom Width", "The width of the liveable area\nMap Size must be set to 'Custom' for this to apply", SizeCategory)]
        [Limit(20, 100)]
        [JsonProperty]
        public int CustomWidth { get; set; }

        [Option("Custom Height", "The height of the liveable area\nMap Size must be set to 'Custom' for this to apply", SizeCategory)]
        [Limit(20, 100)]
        [JsonProperty]
        public int CustomHeight { get; set; }

        [Option("Care Package Timer (Cycles)", "Period of care package drops, in cycles\nLower values give more frequent drops", AnytimeCategory)]
        [Limit(1, 10)]
        [JsonProperty]
        public int CarePackageFrequency { get; set; }

        [Option("Tunnel Access", "Adds tunnels for access to left and right sides", WorldGenCategory)]
        [JsonProperty]
        public TunnelAccessType TunnelAccess { get; set; }

        [Option("Teleporter placement", "Controls where teleporters are placed if enabled.")]
        [JsonProperty]
        public WarpPlacementType TeleporterPlacement { get; set; }

        #endregion

        #region Debug
        [JsonProperty]
        public bool DebugMode;
        [JsonProperty]
        public bool FastImmigration;
        [JsonProperty]
        public bool SkipLiveableArea;
        #endregion
        
        #region Constructor
        
        public MiniBaseOptions()
        {
            MeteorShower = MeteorShowerType.Mixed;
            SpaceRads = Intensity.MED;

            OilMoonlet = false;
            OilMoonletDistance = 3;

            ResinMoonlet = false;
            ResinMoonletDistance = 5;

            NiobiumMoonlet = false;
            NiobiumMoonletDistance = 10;

            BadlandsMoonlet = false;
            BadlandsMoonletDistance = 5;
            
            FlippedMoonlet = false;
            FlippedMoonletDistance = 5;
            
            MetallicSwampyMoonlet = false;
            MetallicSwampyMoonletDistance = 5;
            
            RadioactiveOceanMoonlet = false;
            RadioactiveOceanMoonletDistance = 5;

            ResinPOI = true;
            ResinPOIDistance = 5;

            NiobiumPOI = true;
            NiobiumPOIDistance = 10;

            GildedAsteroidDistance = 8;

            FeatureWest = FeatureType.PollutedWater;
            FeatureEast = FeatureType.RandomUseful;
            FeatureSouth = FeatureType.OilReservoir;
            Biome = BiomeType.Temperate;
            CoreBiome = CoreType.Magma;
            ResourceMod = ResourceModifier.Normal;
            SpaceAccess = AccessType.Classic;
            Size = BaseSize.Normal;
            CustomWidth = 70;
            CustomHeight = 40;
            CarePackageFrequency = 2;
            TunnelAccess = TunnelAccessType.None;
            TeleporterPlacement = WarpPlacementType.Corners;

            DebugMode = false;
            FastImmigration = false;
            SkipLiveableArea = false;
        }
        
        #endregion
        
        #region Methods

        public static void Reload()
        {
            _instance = POptions.ReadSettings<MiniBaseOptions>() ?? new MiniBaseOptions();
        }

        public Vector2I GetBaseSize()
        {
            if (Size == BaseSize.Custom)
            {
                return new Vector2I(CustomWidth, CustomHeight);
            }
            return BaseSizeDictionary.ContainsKey(Size) ? BaseSizeDictionary[Size] : BaseSizeDictionary[BaseSize.Normal];
        }

        public MiniBaseBiomeProfile GetBiome()
        {
            return BiomeTypeMap.ContainsKey(Biome) ? BiomeTypeMap[Biome] : TemperateProfile;
        }

        public bool HasCore()
        {
            return CoreBiome != CoreType.None;
        }

        public MiniBaseBiomeProfile GetCoreBiome()
        {
            return HasCore() ? CoreTypeMap[CoreBiome] : MagmaCoreProfile;
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
                case Intensity.VERY_VERY_LOW:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_VERY_LOW);
                    break;
                case Intensity.VERY_LOW:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_LOW);
                    break;
                case Intensity.LOW:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.LOW);
                    break;
                case Intensity.MED_LOW:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.MED_LOW);
                    break;
                case Intensity.MED:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.MED);
                    break;
                case Intensity.MED_HIGH:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.MED_HIGH);
                    break;
                case Intensity.HIGH:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.HIGH);
                    break;
                case Intensity.VERY_HIGH:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_HIGH);
                    break;
                case Intensity.VERY_VERY_HIGH:
                    world.fixedTraits.Add(TUNING.FIXEDTRAITS.COSMICRADIATION.NAME.VERY_VERY_HIGH);
                    break;
                case Intensity.NONE:
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
        
        #region Enums

        public enum Intensity
        {
            [Option("Very very low", "62.5 rads")]
            VERY_VERY_LOW,
            [Option("Very low", "125 rads")]
            VERY_LOW,
            [Option("Low", "187.5 rads")]
            LOW,
            [Option("Med low", "218.75 rads")]
            MED_LOW,
            [Option("Med (Default)", "250 rads")]
            MED,
            [Option("Med high", "312.5 rads")]
            MED_HIGH,
            [Option("High", "375 rads")]
            HIGH,
            [Option("Very high", "500 rads")]
            VERY_HIGH,
            [Option("Very very high", "750 rads")]
            VERY_VERY_HIGH,
            [Option("None", "0 rads")]
            NONE,
        }

        public enum MeteorShowerType
        {
            [Option("Vanilla", "Vanilla meteors (Regolith + alternating Iron, Gold or Copper). Long duration, medium cooldown")]
            Classic,
            [Option("Spaced Out Classic", "Spaced Out classic-style meteors (Copper, Ice, Slime). Short duration, long cooldown")]
            SpacedOut,
            [Option("Radioactive", "Radioactive moonlet meteors (Uranium Ore). Short duration, long cooldown")]
            Radioactive,
            [Option("Mixed", "Mixed meteors (Uranium Ore, Regolith, Iron, Copper, Gold). Short duration, long cooldown")]
            Mixed,
            [Option("Fullerene", "Fullerene meteors (Fullerene, Regolith). Short duration, long cooldown")]
            Fullerene,
            [Option("None", "No meteor showers")]
            None,
        }

        public enum FeatureType
        {
            [Option("Warm Water", "95C fresh water geyser")]
            WarmWater,
            [Option("Salt Water", "95C salt water geyser")]
            SaltWater,
            [Option("Cool Slush Salt Water", "-10C brine geyser")]
            SlushSaltWater,
            [Option("Polluted Water", "30C polluted water geyser")]
            PollutedWater,
            [Option("Cool Slush", "-10C polluted water geyser")]
            CoolSlush,
            [Option("Cool Steam", "110C steam vent")]
            CoolSteam,
            [Option("Hot Steam", "500C steam vent")]
            HotSteam,
            [Option("Natural Gas", "150C natural gas vent")]
            NaturalGas,
            [Option("Hydrogen", "500C hydrogen vent")]
            Hydrogen,
            [Option("Oil Fissure", "327C leaky oil fissure")]
            OilFissure,
            [Option("Oil Reservoir", "Requires an oil well to extract 90C+ crude oil")]
            OilReservoir,
            [Option("Minor Volcano", "Minor volcano")]
            SmallVolcano,
            [Option("Volcano", "Standard volcano")]
            Volcano,
            [Option("Copper Volcano", "Copper volcano")]
            Copper,
            [Option("Gold Volcano", "Gold volcano")]
            Gold,
            [Option("Iron Volcano", "Iron volcano")]
            Iron,
            [Option("Cobalt Volcano", "Cobalt volcano")]
            Cobalt,
            [Option("Aluminum Volcano", "Aluminum volcano")]
            Aluminum,
            [Option("Tungsten Volcano", "Tungsten volcano")]
            Tungsten,
            [Option("Niobium Volcano", "Niobium volcano")]
            Niobium,
            [Option("Liquid Sulfur", "165.2C liquid sulfur geyser")]
            Sulfur,
            [Option("CO2 Geyser", "-55C carbon dioxide geyser")]
            ColdCO2,
            [Option("CO2 Vent", "500C carbon dioxide vent")]
            HotCO2,
            [Option("Infectious PO2", "60C infectious polluted oxygen vent")]
            InfectedPO2,
            [Option("Hot PO2 Vent", "500C polluted oxygen vent")]
            HotPO2,
            [Option("Chlorine Vent", "60C chlorine vent")]
            Chlorine,
            [Option("Random (Any)", "It could be anything! (excluding niobium volcano)")]
            RandomAny,
            [Option("Random (Water)", "Warm water, salt water, polluted water, or cool slush geyser")]
            RandomWater,
            [Option("Random (Useful)", "Random water or power feature\nExcludes volcanoes and features like chlorine, CO2, sulfur, etc")]
            RandomUseful,
            [Option("Random (Volcano)", "Random lava or metal volcano (excluding niobium volcano)")]
            RandomVolcano,
            [Option("Random (Metal Volcano)", "Random metal volcano (excluding niobium volcano)")]
            RandomMetalVolcano,
            [Option("Random (Magma Volcano)", "Regular or minor volcano")]
            RandomMagmaVolcano,
            None,
        }

        public enum BaseSize
        {
            [Option("Tiny", "30x20")]
            Tiny,
            [Option("Small", "50x40")]
            Small,
            [Option("Normal", "70x40")]
            Normal,
            [Option("Large", "90x50")]
            Large,
            [Option("Square", "50x50")]
            Square,
            [Option("Medium Square", "70x70")]
            MediumSquare,
            [Option("Large Square", "90x90")]
            LargeSquare,
            [Option("Inverted", "40x70")]
            Inverted,
            [Option("Tall", "40x100")]
            Tall,
            [Option("Skinny", "26x100")]
            Skinny,
            [Option("Custom", "Select to define custom size")]
            Custom,
        }
        
        public enum ResourceModifier
        {
            [Option("Poor", "50% fewer resources")]
            Poor,
            [Option("Normal", "Standard amount of resources")]
            Normal,
            [Option("Rich", "50% more resources")]
            Rich,
        }

        public enum AccessType
        {
            [Option("None", "Fully contained and protected")]
            None,
            [Option("Classic", "Limited space access ports")]
            Classic,
            [Option("Full", "Space access across the top border")]
            Full,
        }

        public enum TunnelAccessType
        {
            [Option("None", "No Side Tunnels")]
            None,
            [Option("Left Only", "Adds Left Side Tunnel")]
            LeftOnly,
            [Option("Right Only", "Adds Right Side Tunnel")]
            RightOnly,
            [Option("Left and Right", "Adds Left and Right Side Tunnels")]
            BothSides
        }
        
        public enum SideType
        {
            Space,
            Terrain,
        }
        
        public enum BiomeType
        {
            [Option("Temperate", "Inviting and inhabitable")]
            Temperate,
            [Option("Forest", "Temperate and earthy")]
            Forest,
            [Option("Swamp", "Beware the slime!")]
            Swamp,
            [Option("Frozen", "Cold, but at least you get some jackets")]
            Frozen,
            [Option("Desert", "Hot and sandy")]
            Desert,
            [Option("Barren", "Hard rocks, hard hatches, hard to survive")]
            Barren,
            [Option("Str?nge", "#%*@&#^$%(_#$&%^#@*&")]
            Strange,
            [Option("Deep Essence", "Filled with vibes")]
            DeepEssence,
        }
        
        public enum CoreType
        {
            [Option("Molten", "Magma, diamond, and a smattering of tough metals")]
            Magma,
            [Option("Ocean", "Saltwater, bleachstone, sand, and crabs")]
            Ocean,
            [Option("Frozen", "Cold, cold, and more cold")]
            Frozen,
            [Option("Oil", "A whole lot of crude")]
            Oil,
            [Option("Metal", "Ores and metals of all varieties")]
            Metal,
            [Option("Fertile", "Dirt, water, algae, and iron")]
            Fertile,
            [Option("Boneyard", "Cool remains of an ancient world")]
            Boneyard,
            [Option("Aesthetic", "Filled with  V I B E S")]
            Aesthetic,
            [Option("Pearl Inferno", "Molten inferno of aluminum, glass, steam, \nand some high temperature materials")]
            Pearl,
            [Option("Radioactive", "Bees!!! ... and some uranium")]
            Radioactive,
            [Option("None", "No core or abyssalite border")]
            None,
        }

        public enum WarpPlacementType
        {
            [Option("Center", "Teleporters spawn in the center.")]
            Center,
            [Option("Corners", "Teleporters spawn in the bottom corners.")]
            Corners
        }
        
        #endregion
        
        #region Dictionaries
        
        private static Dictionary<BaseSize, Vector2I> BaseSizeDictionary = new Dictionary<BaseSize, Vector2I>()
        {
            { BaseSize.Tiny, new Vector2I(30, 20) },
            { BaseSize.Small, new Vector2I(50, 30) },
            { BaseSize.Normal, new Vector2I(70, 40) },
            { BaseSize.Large, new Vector2I(90, 50) },
            { BaseSize.Square, new Vector2I(50, 50) },
            { BaseSize.MediumSquare, new Vector2I(70, 70) },
            { BaseSize.LargeSquare, new Vector2I(90, 90) },
            { BaseSize.Inverted, new Vector2I(40, 70) },
            { BaseSize.Tall, new Vector2I(40, 100) },
            { BaseSize.Skinny, new Vector2I(26, 100) },
        };

        private static Dictionary<BiomeType, MiniBaseBiomeProfile> BiomeTypeMap = new Dictionary<BiomeType, MiniBaseBiomeProfile>()
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

        private static Dictionary<CoreType, MiniBaseBiomeProfile> CoreTypeMap = new Dictionary<CoreType, MiniBaseBiomeProfile>()
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
        private const string WorldGenCategory = "These options are only applied when the map is generated";
        private const string SizeCategory = "These options change the size of the liveable area\nTo define a custom size, set Map Size to 'Custom'";
        private const string AnytimeCategory = "These options may be changed at any time";
        #endregion
    }
}
