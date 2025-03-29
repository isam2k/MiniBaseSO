using System;
using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
    [Flags]
    public enum TunnelAccessType : int
    {
        [Option("None", "No Side Tunnels")]
        None = 0,
        [Option("Left Only", "Adds Left Side Tunnel")]
        Left = 0x1,
        [Option("Right Only", "Adds Right Side Tunnel")]
        Right = 0x2,
        [Option("Left and Right", "Adds Left and Right Side Tunnels")]
        BothSides = 0x3
    }
}