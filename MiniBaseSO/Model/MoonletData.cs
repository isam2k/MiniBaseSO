using MiniBase.Profiles;
using ProcGenGame;
using static MiniBase.MiniBaseConfig;

namespace MiniBase.Model
{
    internal class MoonletData
    {
        #region Properties
        /// <summary>Total size of the map in tiles.</summary>
        public Vector2I WorldSize { get; }

        /// <summary>The type of mini moonlet of this instance.</summary>
        public Moonlet Type { get; }

        /// <summary>Main moonlet biome.</summary>
        public MiniBaseBiomeProfile Biome { get; }

        /// <summary>Bottom moonlet biome.</summary>
        public MiniBaseBiomeProfile CoreBiome { get; }

        /// <summary>Moonlet has a core biome or just the main biome.</summary>
        public bool HasCore { get; }
        #endregion

        public MoonletData(WorldGen worldGen)
        {
            var options = MiniBaseOptions.Instance;
            switch (worldGen.Settings.world.filePath)
            {
                case "worlds/MiniBase":
                case "expansion1::worlds/MiniBase":
                    Type = Moonlet.Start;
                    Biome = options.GetBiome();
                    CoreBiome = options.GetCoreBiome();
                    HasCore = options.HasCore();
                    _extraTopMargin = 0;
                    break;
                case "expansion1::worlds/BabyOilyMoonlet":
                    Type = Moonlet.Second;
                    Biome = MiniBaseBiomeProfiles.OilMoonletProfile;
                    CoreBiome = MiniBaseCoreBiomeProfiles.MagmaCoreProfile;
                    HasCore = true;
                    _extraTopMargin = ColonizableExtraMargin;
                    break;
                case "expansion1::worlds/BabyMarshyMoonlet":
                    Type = Moonlet.Tree;
                    Biome = MiniBaseBiomeProfiles.TreeMoonletProfile;
                    HasCore = false;
                    _extraTopMargin = ColonizableExtraMargin;
                    break;
                case "expansion1::worlds/BabyNiobiumMoonlet":
                    Type = Moonlet.Niobium;
                    Biome = MiniBaseBiomeProfiles.NiobiumMoonletProfile;
                    HasCore = false;
                    _extraTopMargin = ColonizableExtraMargin;
                    break;
            }
            WorldSize = worldGen.WorldSize;
            _size = new Vector2I(WorldSize.x - 2 * BorderSize,
                WorldSize.y - 2 * BorderSize - TopMargin - _extraTopMargin);
        }
        
        #region Methods
        public int SideMargin() { return (WorldSize.x - _size.x - 2 * BorderSize) / 2; }
        /// <summary>
        /// The leftmost tile position that is considered inside the liveable area or its borders.
        /// </summary>
        /// <param name="withBorders"></param>
        /// <returns></returns>
        public int Left(bool withBorders = false) { return SideMargin() + (withBorders ? 0 : BorderSize); }
        /// <summary>
        /// The rightmost tile position that is considered inside the liveable area or its borders.
        /// </summary>
        /// <param name="withBorders"></param>
        /// <returns></returns>
        public int Right(bool withBorders = false) { return Left(withBorders) + _size.x + (withBorders ? BorderSize * 2 : 0); }
        public int Top(bool withBorders = false) { return WorldSize.y - TopMargin - _extraTopMargin - (withBorders ? 0 : BorderSize) + 1; }
        public int Bottom(bool withBorders = false) { return Top(withBorders) - _size.y - (withBorders ? BorderSize * 2 : 0); }
        public int Width(bool withBorders = false) { return Right(withBorders) - Left(withBorders); }
        public int Height(bool withBorders = false) { return Top(withBorders) - Bottom(withBorders); }
        public Vector2I TopLeft(bool withBorders = false) { return new Vector2I(Left(withBorders), Top(withBorders)); }
        public Vector2I TopRight(bool withBorders = false) { return new Vector2I(Right(withBorders), Top(withBorders)); }
        public Vector2I BottomLeft(bool withBorders = false) { return new Vector2I(Left(withBorders), Bottom(withBorders)); }
        public Vector2I BottomRight(bool withBorders = false) { return new Vector2I(Right(withBorders), Bottom(withBorders)); }
        public bool InLiveableArea(Vector2I pos) { return pos.x >= Left() && pos.x < Right() && pos.y >= Bottom() && pos.y < Top(); }
        #endregion
        
        #region Fields
        private readonly Vector2I _size;
        private readonly int _extraTopMargin;
        #endregion
        
        #region Enum
        public enum Moonlet
        {
            Start,
            Second,
            Tree,
            Niobium
        }
        #endregion
    }
}