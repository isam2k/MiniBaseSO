using System.Collections.Generic;
using static MiniBase.MiniBaseOptions;

namespace MiniBase
{
    public class MiniBaseConfig
    {
        //public static int WORLD_WIDTH = 128;
        //public static int WORLD_HEIGHT = 128;
        public const int BorderSize = 3;
        public const int TopMargin = 3;
        public const int ColonizableExtraMargin = 8; // extra space to land rockets
        public const int CornerSize = 7;
        public const int DiagonalBorderSize = 4;

        public const int SpaceAccessSize = 8;
        public const int SideAccessSize = 5;
        public const int CoreMin = 0;
        public const int CoreDeviation = 3;
        public const int CoreBorder = 3;

        public enum DiseaseID { NONE, SLIMELUNG, FOOD_POISONING };

        public const string ClusterName = "MiniBase";
        public const string ClusterMainWorld = "MiniBase";
        public const string ClusterDescription = "An encapsulated location with just enough to get by.\n\n<smallcaps>Customize this location by clicking MiniBase Options in the Mods menu.</smallcaps>\n\n";
        public const string ClusterIconName = "Asteroid_minibase";

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
            { FeatureType.ColdCO2, "GeyserGeneric_" + GeyserGenericConfig.LiquidCO2 },
            { FeatureType.HotCO2, "GeyserGeneric_" + GeyserGenericConfig.HotCO2 },
            { FeatureType.InfectedPO2, "GeyserGeneric_" + GeyserGenericConfig.SlimyPO2 },
            { FeatureType.HotPO2, "GeyserGeneric_" + GeyserGenericConfig.HotPO2 },
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
    }
}
