using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
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
}