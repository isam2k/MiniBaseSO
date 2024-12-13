using System.Collections.Generic;
using System.Linq;

namespace MiniBase
{
    public class MiniBaseBiomeProfile
    {
        public MiniBaseBiomeProfile(
            string backgroundSubworld,
            SimHashes defaultMaterial,
            float defaultTemperature,
            BandInfo[] bandProfile,
            List<KeyValuePair<string, float>> startingItems = null,
            Dictionary<string, float> spawnablesOnFloor = null,
            Dictionary<string, float> spawnablesOnCeil = null,
            Dictionary<string, float> spawnablesInGround = null,
            Dictionary<string, float> spawnablesInLiquid = null,
            Dictionary<string, float> spawnablesInAir = null)
        {
            BackgroundSubworld = backgroundSubworld;
            DefaultMaterial = defaultMaterial;
            DefaultTemperature = defaultTemperature;
            BandProfile = bandProfile;
            StartingItems = startingItems ?? new List<KeyValuePair<string, float>>();
            SpawnablesOnFloor = spawnablesOnFloor ?? new Dictionary<string, float>();
            SpawnablesOnCeil = spawnablesOnCeil ?? new Dictionary<string, float>();
            SpawnablesInGround = spawnablesInGround ?? new Dictionary<string, float>();
            SpawnablesInLiquid = spawnablesInLiquid ?? new Dictionary<string, float>();
            SpawnablesInAir = spawnablesInAir ?? new Dictionary<string, float>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Element DefaultElement()
        {
            return ElementLoader.FindElementByHash(DefaultMaterial);
        }

        /// <summary>
        /// Return the corresponding element band info from the float in [0.0, 1.0]
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public BandInfo GetBand(float f)
        {
            for (int i = 0; i < BandProfile.Length; i++)
            {
                if (f < BandProfile[i].cumulativeWeight)
                {
                    return BandProfile[i];
                }
            }
            return BandProfile.Last();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="band"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public Sim.PhysicsData GetPhysicsData(BandInfo band, float modifier = 1f)
        {
            float temperature = (band.temperature < 0 && DefaultTemperature > 0) ? DefaultTemperature : band.temperature;
            return MiniBaseWorldGen.GetPhysicsData(band.GetElement(), modifier * band.density, temperature);
        }
        
        #region Fields
        public string BackgroundSubworld;
        public SimHashes DefaultMaterial;
        public float DefaultTemperature;
        public BandInfo[] BandProfile;
        public List<KeyValuePair<string, float>> StartingItems;
        public Dictionary<string, float> SpawnablesOnFloor;
        public Dictionary<string, float> SpawnablesOnCeil;
        public Dictionary<string, float> SpawnablesInGround;
        public Dictionary<string, float> SpawnablesInLiquid;
        public Dictionary<string, float> SpawnablesInAir;
        #endregion
    }

    public struct BandInfo
    {
        public float cumulativeWeight;
        public SimHashes elementId;
        public float temperature;
        public float density;
        public MiniBaseConfig.DiseaseID disease;

        public BandInfo(float cumulativeWeight, SimHashes elementId, float temperature = -1f, float density = 1f, MiniBaseConfig.DiseaseID disease = MiniBaseConfig.DiseaseID.None)
        {
            this.cumulativeWeight = cumulativeWeight;
            this.elementId = elementId;
            this.temperature = temperature;
            this.density = density;
            this.disease = disease;
        }

        public Element GetElement() { return ElementLoader.FindElementByHash(elementId); }
    }
}
