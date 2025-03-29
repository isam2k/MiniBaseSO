using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
    public enum ResourceModifier
    {
        [Option("Poor", "50% fewer resources")]
        Poor,
        [Option("Normal", "Standard amount of resources")]
        Normal,
        [Option("Rich", "50% more resources")]
        Rich,
    }
}