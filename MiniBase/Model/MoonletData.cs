using Klei.CustomSettings;
using MiniBase.Model.Enums;
using MiniBase.Model.Profiles;
using ProcGen;
using ProcGenGame;

namespace MiniBase.Model
{
    public class MoonletData
    {
        #region Properties
        /// <summary>Total size of the map in tiles.</summary>
        public Vector2I WorldSize { get; }

        /// <summary>Size of the map where a borders and biomes will spawn.</summary>
        public Vector2I BaseSize { get; }

        /// <summary>The type of mini moonlet of this instance.</summary>
        internal Moonlet Type { get; }

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
                case VanillaStartMap:
                case DlcStartMap:
                    Type = Moonlet.Start;
                    Biome = options.GetBiome();
                    CoreBiome = options.GetCoreBiome();
                    HasCore = options.HasCore();
                    _extraTopMargin = 0;
                    break;
                case DlcSecondMap:
                    Type = Moonlet.Second;
                    Biome = MiniBaseBiomeProfiles.OilMoonletProfile;
                    CoreBiome = MiniBaseCoreBiomeProfiles.MagmaCoreProfile;
                    HasCore = true;
                    _extraTopMargin = 0;
                    break;
                case DlcMarshyMap:
                    Type = Moonlet.Tree;
                    Biome = MiniBaseBiomeProfiles.TreeMoonletProfile;
                    HasCore = false;
                    _extraTopMargin = MiniBaseOptions.ColonizableExtraMargin;
                    break;
                case DlcNiobiumMap:
                    Type = Moonlet.Niobium;
                    Biome = MiniBaseBiomeProfiles.NiobiumMoonletProfile;
                    HasCore = false;
                    _extraTopMargin = MiniBaseOptions.ColonizableExtraMargin;
                    break;
                case DlcFrozenForestMap:
                    Type = Moonlet.FrozenForest;
                    break;
                case DlcBadlandsMap:
                    Type = Moonlet.Badlands;
                    break;
                case DlcFlippedMap:
                    Type = Moonlet.Flipped;
                    break;
                case DlcMetallicSwampyMap:
                    Type = Moonlet.MetallicSwampy;
                    break;
                case DlcRadioactiveOceanMap:
                    Type = Moonlet.RadioactiveOcean;
                    break;
            }
            WorldSize = worldGen.WorldSize;
            BaseSize = MiniBaseOptions.Instance.GetBaseSize(Type);
        }
        
        #region Methods
        
        /// <summary>
        /// Width of the vacuum area to the side of the base, outside of the borders. These areas are
        /// accessible via the side tunnels if enabled.
        /// </summary>
        /// <returns></returns>
        public int SideMargin() { return (WorldSize.x - BaseSize.x - 2 * MiniBaseOptions.BorderSize) / 2; }
        /// <summary>
        /// The leftmost tile position that is considered inside the liveable area or its borders.
        /// Tiles in the liveable area are part of the main biome.
        /// </summary>
        /// <param name="withBorders"></param>
        /// <returns></returns>
        public int Left(bool withBorders = false) { return SideMargin() + (withBorders ? 0 : MiniBaseOptions.BorderSize); }
        /// <summary>
        /// The rightmost tile position that is considered inside the liveable area or its borders.
        /// </summary>
        /// <param name="withBorders"></param>
        /// <returns></returns>
        public int Right(bool withBorders = false) { return Left(withBorders) + BaseSize.x + (withBorders ? MiniBaseOptions.BorderSize * 2 : 0); }
        public int Top(bool withBorders = false) { return WorldSize.y - MiniBaseOptions.TopMargin - _extraTopMargin - (withBorders ? 0 : MiniBaseOptions.BorderSize) + 1; }
        public int Bottom(bool withBorders = false) { return Top(withBorders) - BaseSize.y - (withBorders ? MiniBaseOptions.BorderSize * 2 : 0); }
        public int Width(bool withBorders = false) { return Right(withBorders) - Left(withBorders); }
        public int Height(bool withBorders = false) { return Top(withBorders) - Bottom(withBorders); }
        public Vector2I TopLeft(bool withBorders = false) { return new Vector2I(Left(withBorders), Top(withBorders)); }
        public Vector2I TopRight(bool withBorders = false) { return new Vector2I(Right(withBorders), Top(withBorders)); }
        public Vector2I BottomLeft(bool withBorders = false) { return new Vector2I(Left(withBorders), Bottom(withBorders)); }
        public Vector2I BottomRight(bool withBorders = false) { return new Vector2I(Right(withBorders), Bottom(withBorders)); }
        public bool InLiveableArea(Vector2I pos) { return pos.x >= Left() && pos.x < Right() && pos.y >= Bottom() && pos.y < Top(); }
        
        #endregion
        
        #region Static Functions
        /// <summary>
        /// Determines if a <see cref="WorldGen"/> instance represents a minibase map.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static bool IsMiniBaseWorld(WorldGen instance) =>
            instance.Settings.world.filePath == VanillaStartMap ||
            instance.Settings.world.filePath == DlcStartMap ||
            instance.Settings.world.filePath == DlcSecondMap ||
            instance.Settings.world.filePath == DlcMarshyMap ||
            instance.Settings.world.filePath == DlcNiobiumMap;
        /// <summary>
        /// Determines if a <see cref="WorldGen"/> instance represents a natural minibase map.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static bool IsMiniBaseNatural(WorldGen instance) =>
            instance.Settings.world.filePath == DlcFrozenForestMap ||
            instance.Settings.world.filePath == DlcBadlandsMap ||
            instance.Settings.world.filePath == DlcFlippedMap ||
            instance.Settings.world.filePath == DlcMetallicSwampyMap ||
            instance.Settings.world.filePath == DlcRadioactiveOceanMap;
        /// <summary>
        /// If the starting asteroid is worlds/MiniBase we consider the playthrough
        /// to be a minibase one (for Cluster Generaton Manager compatibility).
        /// </summary>
        /// <returns></returns>
        public static bool IsMiniBaseCluster()
        {
            var clusterCache = SettingsCache.clusterLayouts.clusterCache;
            var world = clusterCache[CustomGameSettings.Instance.GetCurrentQualitySetting(CustomGameSettingConfigs.ClusterLayout).id]
                .GetStartWorld();
            return world == DlcStartMap ||
                   world == DlcFrozenForestMap ||
                   world == VanillaStartMap;
        }
        #endregion
        
        #region Fields
        
        /// <summary>Additional margin at the top for landing rockets</summary>
        private readonly int _extraTopMargin;

        #endregion
        
        #region Constants
        internal const string MiniBaseCluster = "expansion1::clusters/MiniBase";
        internal const string VanillaStartMap = "worlds/MiniBase";
        internal const string DlcStartMap = "expansion1::worlds/MiniBase";
        internal const string DlcSecondMap = "expansion1::worlds/BabyOilyMoonlet";
        internal const string DlcMarshyMap = "expansion1::worlds/BabyMarshyMoonlet";
        internal const string DlcNiobiumMap = "expansion1::worlds/BabyNiobiumMoonlet";
        internal const string DlcFrozenForestMap = "expansion1::worlds/BabyFrozedForestStart";
        internal const string DlcBadlandsMap = "expansion1::worlds/BabyBadlands";
        internal const string DlcFlippedMap = "expansion1::worlds/BabyFlipped";
        internal const string DlcMetallicSwampyMap = "expansion1::worlds/BabyMetallicSwampy";
        internal const string DlcRadioactiveOceanMap = "expansion1::worlds/BabyRadioactiveOcean";
        #endregion
    }
}