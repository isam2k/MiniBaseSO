namespace MiniBase.Model
{
    public struct Octave
    {
        public readonly float Amp;
        public readonly float Freq;

        public Octave(float amplitude, float frequency)
        {
            Amp = amplitude;
            Freq = frequency;
        }
    }
}