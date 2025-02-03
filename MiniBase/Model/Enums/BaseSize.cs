using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
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
}