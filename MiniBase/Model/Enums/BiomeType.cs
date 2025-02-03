using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
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
}