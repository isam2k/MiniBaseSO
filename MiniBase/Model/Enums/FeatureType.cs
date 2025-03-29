using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
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
            ColdCo2,
            [Option("CO2 Vent", "500C carbon dioxide vent")]
            HotCo2,
            [Option("Infectious PO2", "60C infectious polluted oxygen vent")]
            InfectedPo2,
            [Option("Hot PO2 Vent", "500C polluted oxygen vent")]
            HotPo2,
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
}