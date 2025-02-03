using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
    public enum WarpPlacementType
    {
        [Option("Center", "Teleporters spawn in the center.")]
        Center,
        [Option("Corners", "Teleporters spawn in the bottom corners.")]
        Corners
    }
}