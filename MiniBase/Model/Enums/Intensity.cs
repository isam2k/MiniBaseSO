using PeterHan.PLib.Options;

namespace MiniBase.Model.Enums
{
    public enum Intensity
    {
        [Option("Very very low", "62.5 rads")]
        VeryVeryLow,
        [Option("Very low", "125 rads")]
        VeryLow,
        [Option("Low", "187.5 rads")]
        Low,
        [Option("Med low", "218.75 rads")]
        MedLow,
        [Option("Med (Default)", "250 rads")]
        Med,
        [Option("Med high", "312.5 rads")]
        MedHigh,
        [Option("High", "375 rads")]
        High,
        [Option("Very high", "500 rads")]
        VeryHigh,
        [Option("Very very high", "750 rads")]
        VeryVeryHigh,
        [Option("None", "0 rads")]
        None,
    }
}