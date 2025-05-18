namespace TinyProc.Application;

public sealed class GlobalData
{
    // The version of this program itself
    public const string TINYPROC_PROGRAM_VERSION_STR = "2025.05-dev2";
    // The version / revision of the emulated CPU
    public const string PROCESSOR_REVISION_VERSION_STR = "0.1-indev";

    public static bool IsClockAuto { get; set; } = false;
    public static uint ClockRateHz { get; set; } = 1;

    private static uint _cyclesUntilHalt;
    public static uint CyclesUntilHalt
    {
        get => _cyclesUntilHalt;
        set
        {
            if (value == 0)
                IsClockAuto = false;
            _cyclesUntilHalt = value;
        }
    }
}