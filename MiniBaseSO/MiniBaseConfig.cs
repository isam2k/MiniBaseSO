using System.Collections.Generic;
using static MiniBaseSO.MiniBaseOptions;

namespace MiniBaseSO
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

        public enum DiseaseID
        {
            None,
            Slimelung,
            FoodPoisoning
        };

        public static Dictionary<MiniBaseOptions.FeatureType, string> GeyserDictionary = new Dictionary<MiniBaseOptions.FeatureType, string>()
        {
            { MiniBaseOptions.FeatureType.WarmWater, "GeyserGeneric_" + GeyserGenericConfig.HotWater },
            { MiniBaseOptions.FeatureType.SaltWater, "GeyserGeneric_" + GeyserGenericConfig.SaltWater },
            { MiniBaseOptions.FeatureType.SlushSaltWater, "GeyserGeneric_" + GeyserGenericConfig.SlushSaltWater },
            { MiniBaseOptions.FeatureType.PollutedWater, "GeyserGeneric_" + GeyserGenericConfig.FilthyWater },
            { MiniBaseOptions.FeatureType.CoolSlush, "GeyserGeneric_" + GeyserGenericConfig.SlushWater },
            { MiniBaseOptions.FeatureType.CoolSteam, "GeyserGeneric_" + GeyserGenericConfig.Steam },
            { MiniBaseOptions.FeatureType.HotSteam, "GeyserGeneric_" + GeyserGenericConfig.HotSteam },
            { MiniBaseOptions.FeatureType.NaturalGas, "GeyserGeneric_" + GeyserGenericConfig.Methane },
            { MiniBaseOptions.FeatureType.Hydrogen, "GeyserGeneric_" + GeyserGenericConfig.HotHydrogen },
            { MiniBaseOptions.FeatureType.OilFissure, "GeyserGeneric_" + GeyserGenericConfig.OilDrip },
            { MiniBaseOptions.FeatureType.OilReservoir, "OilWell" },
            { MiniBaseOptions.FeatureType.SmallVolcano, "GeyserGeneric_" + GeyserGenericConfig.SmallVolcano },
            { MiniBaseOptions.FeatureType.Volcano, "GeyserGeneric_" + GeyserGenericConfig.BigVolcano },
            { MiniBaseOptions.FeatureType.Copper, "GeyserGeneric_" + GeyserGenericConfig.MoltenCopper },
            { MiniBaseOptions.FeatureType.Gold, "GeyserGeneric_" + GeyserGenericConfig.MoltenGold },
            { MiniBaseOptions.FeatureType.Iron, "GeyserGeneric_" + GeyserGenericConfig.MoltenIron },
            { MiniBaseOptions.FeatureType.Cobalt, "GeyserGeneric_" + GeyserGenericConfig.MoltenCobalt },
            { MiniBaseOptions.FeatureType.Aluminum, "GeyserGeneric_" + GeyserGenericConfig.MoltenAluminum },
            { MiniBaseOptions.FeatureType.Tungsten, "GeyserGeneric_" + GeyserGenericConfig.MoltenTungsten },
            { MiniBaseOptions.FeatureType.Niobium, "GeyserGeneric_" + GeyserGenericConfig.MoltenNiobium },
            { MiniBaseOptions.FeatureType.Sulfur, "GeyserGeneric_" + GeyserGenericConfig.LiquidSulfur },
            { MiniBaseOptions.FeatureType.ColdCO2, "GeyserGeneric_" + GeyserGenericConfig.LiquidCO2 },
            { MiniBaseOptions.FeatureType.HotCO2, "GeyserGeneric_" + GeyserGenericConfig.HotCO2 },
            { MiniBaseOptions.FeatureType.InfectedPO2, "GeyserGeneric_" + GeyserGenericConfig.SlimyPO2 },
            { MiniBaseOptions.FeatureType.HotPO2, "GeyserGeneric_" + GeyserGenericConfig.HotPO2 },
            { MiniBaseOptions.FeatureType.Chlorine, "GeyserGeneric_" + GeyserGenericConfig.ChlorineGas },
        };

        public static MiniBaseOptions.FeatureType[] RandomWaterFeatures =
        {
            MiniBaseOptions.FeatureType.WarmWater,
            MiniBaseOptions.FeatureType.SaltWater,
            MiniBaseOptions.FeatureType.PollutedWater,
            MiniBaseOptions.FeatureType.CoolSlush,
            MiniBaseOptions.FeatureType.SlushSaltWater,
        };

        public static MiniBaseOptions.FeatureType[] RandomUsefulFeatures =
        {
            MiniBaseOptions.FeatureType.WarmWater,
            MiniBaseOptions.FeatureType.SaltWater,
            MiniBaseOptions.FeatureType.SlushSaltWater,
            MiniBaseOptions.FeatureType.PollutedWater,
            MiniBaseOptions.FeatureType.CoolSlush,
            MiniBaseOptions.FeatureType.CoolSteam,
            MiniBaseOptions.FeatureType.HotSteam,
            MiniBaseOptions.FeatureType.NaturalGas,
            MiniBaseOptions.FeatureType.Hydrogen,
            MiniBaseOptions.FeatureType.OilFissure,
            MiniBaseOptions.FeatureType.OilReservoir,
        };

        public static MiniBaseOptions.FeatureType[] RandomMagmaVolcanoFeatures =
        {
            MiniBaseOptions.FeatureType.SmallVolcano,
            MiniBaseOptions.FeatureType.Volcano,
        };

        public static MiniBaseOptions.FeatureType[] RandomVolcanoFeatures =
        {
            MiniBaseOptions.FeatureType.SmallVolcano,
            MiniBaseOptions.FeatureType.Volcano,
            MiniBaseOptions.FeatureType.Copper,
            MiniBaseOptions.FeatureType.Gold,
            MiniBaseOptions.FeatureType.Iron,
            MiniBaseOptions.FeatureType.Cobalt,
            MiniBaseOptions.FeatureType.Aluminum,
            MiniBaseOptions.FeatureType.Tungsten,
        };

        public static MiniBaseOptions.FeatureType[] RandomMetalVolcanoFeatures =
        {
            MiniBaseOptions.FeatureType.Copper,
            MiniBaseOptions.FeatureType.Gold,
            MiniBaseOptions.FeatureType.Iron,
            MiniBaseOptions.FeatureType.Cobalt,
            MiniBaseOptions.FeatureType.Aluminum,
            MiniBaseOptions.FeatureType.Tungsten,
        };
    }
}
