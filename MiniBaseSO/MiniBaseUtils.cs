using Klei.CustomSettings;
using ProcGen;
using System.Collections.Generic;

namespace MiniBaseSO
{
    class MiniBaseUtils
    {
        // If the starting asteroid is worlds/MiniBase we consider the playthrough to be a minibase one (for Cluster Generaton Manager compatibility)
        public static bool IsMiniBaseCluster()
        {
            Dictionary<string, ClusterLayout> clusterCache = SettingsCache.clusterLayouts.clusterCache;
            var cluster = clusterCache[CustomGameSettings.Instance.GetCurrentQualitySetting(CustomGameSettingConfigs.ClusterLayout).id];
            return cluster.GetStartWorld() == "worlds/MiniBase";
        }
    }
}
