using MiniBase.Model.Enums;

namespace MiniBase.Model
{
    public readonly struct BandInfo
    {
        public readonly float CumulativeWeight;
        public readonly SimHashes ElementId;
        public readonly float Temperature;
        public readonly float Density;
        public readonly DiseaseID Disease;

        public BandInfo(float cumulativeWeight, SimHashes elementId, float temperature = -1f, float density = 1f, DiseaseID disease = DiseaseID.None)
        {
            CumulativeWeight = cumulativeWeight;
            ElementId = elementId;
            Temperature = temperature;
            Density = density;
            Disease = disease;
        }

        public Element GetElement() { return ElementLoader.FindElementByHash(ElementId); }
    }
}