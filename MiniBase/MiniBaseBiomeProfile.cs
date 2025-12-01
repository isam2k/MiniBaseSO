using System.Collections.Generic;
using System.Linq;
using MiniBase.Model;
using MiniBase.Model.Enums;

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
            for (var i = 0; i < BandProfile.Length; i++)
            {
                if (f < BandProfile[i].CumulativeWeight)
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
            var temperature = (band.Temperature < 0 && DefaultTemperature > 0) ? DefaultTemperature : band.Temperature;
            return MiniBaseWorldGen.GetPhysicsData(band.GetElement(), modifier * band.Density, temperature);
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
}
