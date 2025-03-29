using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
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
}