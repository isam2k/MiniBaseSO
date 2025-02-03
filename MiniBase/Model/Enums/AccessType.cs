using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
    public enum AccessType
    {
        [Option("None", "Fully contained and protected")]
        None,
        [Option("Classic", "Limited space access ports")]
        Classic,
        [Option("Full", "Space access across the top border")]
        Full,
    }
}