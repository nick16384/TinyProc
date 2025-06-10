using System.Diagnostics;
using TinyProc.Memory;
using TinyProc.Processor.CPU;

namespace TinyProc.Application;

public class ExecutionContainer
{
    // The first (and almost always only) instance of an ExecutionContainer, which is
    // exposed externally to be used by e.g. GUIs.
    public static ExecutionContainer INSTANCE0;

    private readonly RawMemory _mem1;
    private readonly ConsoleMemory _tmem1;
    private readonly CPU _cpu;

    private ulong _currentCycle = 0;
    public ulong CurrentCycle { get => _currentCycle; }
    private long _lastCycleTimeMicroseconds = 0;
    public long LastCycleTimeMicroseconds { get => _lastCycleTimeMicroseconds; }

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

    public static ExecutionContainer Initialize(ExecutableWrapper mainProgramWrapper)
    {
        INSTANCE0 = new ExecutionContainer(mainProgramWrapper);
        return INSTANCE0;
    }

    private ExecutionContainer(ExecutableWrapper mainProgramWrapper)
    {
        uint ramSize = mainProgramWrapper.RAMRegionEnd - mainProgramWrapper.RAMRegionStart + 1;
        uint conSize = mainProgramWrapper.CONRegionEnd - mainProgramWrapper.CONRegionStart + 1;
        Logging.LogDebug("Creating virtual hardware");
        Logging.LogDebug("Creating working memory & console memory objects");
        Logging.LogDebug($"{ramSize}, {conSize}");
        _mem1 = new RawMemory(ramSize, mainProgramWrapper.ExecutableProgram);
        _tmem1 = new ConsoleMemory(conSize);

        Logging.LogDebug("Creating CPU object, loading main program");
        _cpu = new(new Dictionary<(uint, uint), RawMemory>
            {
                { (mainProgramWrapper.RAMRegionStart, mainProgramWrapper.RAMRegionEnd), _mem1 },
                { (mainProgramWrapper.CONRegionStart, mainProgramWrapper.CONRegionEnd), _tmem1 }
            }, mainProgramWrapper.EntryPoint
        );

        Logging.LogDebug("Reading loaded program.");
        if (_mem1._words < 4096)
            _mem1.Debug_DumpAll();
        else
            Logging.LogDebug("Memory object too large to dump.");
        Logging.LogDebug("Done.");

        // Miku = 39 = Sankyuu easter egg ^-^
        //mem1.WriteEnable = true;
        //mem1.AddressBus = 0x00000039u;
        //mem1.DataBus = 0x39393939u;

        if (INSTANCE0 == null)
            INSTANCE0 = this;
    }
    
    public void WriteRAMDirect(uint address, int uintOffset, byte value)
        => _mem1.WriteDirect(address, _mem1.ReadDirect(address) & ((uint)value << (uintOffset * 8)));
    // Returns the bytes that currently reside within the working memory.
    // Note that using a byte[] instead of a uint[][] is not guaranteed to return all RAM data (because of C# limitations).
    // Warning: Accessing this property may have a significant performance impact when run after each CPU cycle, since it takes
    // comparatively long to compute the byte[] resulting from the current RawMemory's uint[][].
    // TODO: Maybe use some sort of List<byte> instead of byte[] to overcome this limit
    public byte[] LiveRAMBytes { get => UIntArrayToByteArray(_mem1.Data[0]); }
    
    private static byte[] UIntArrayToByteArray(uint[] uintArray)
    {
        int byteArraySize;
        if ((ulong)uintArray.Length * sizeof(uint) > int.MaxValue)
        {
            byteArraySize = int.MaxValue;
            Logging.LogWarn(
                $"Accessing a uint[] as byte[] only allows {int.MaxValue} " +
                $"bytes to be read, however, the actual uint[] is larger with {(ulong)uintArray.Length * sizeof(uint)} " +
                "bytes. Only a partial amount is returned!");
        }
        else
            byteArraySize = uintArray.Length * sizeof(uint);

        byte[] byteArray = new byte[byteArraySize];
        Buffer.BlockCopy(uintArray, 0, byteArray, 0, byteArraySize);
        Parallel.For(0, byteArraySize / sizeof(uint), i =>
        {
            Array.Reverse(byteArray, i * 4, 4);
        });
        return byteArray;
    }

    public uint Debug_CPU_GP1Value { get => _cpu.Debug_GP1Value; }
    public uint Debug_CPU_GP2Value { get => _cpu.Debug_GP2Value; }
    public uint Debug_CPU_GP3Value { get => _cpu.Debug_GP3Value; }
    public uint Debug_CPU_GP4Value { get => _cpu.Debug_GP4Value; }

    public uint Debug_CPU_GP5Value { get => _cpu.Debug_GP5Value; }
    public uint Debug_CPU_GP6Value { get => _cpu.Debug_GP6Value; }
    public uint Debug_CPU_GP7Value { get => _cpu.Debug_GP7Value; }
    public uint Debug_CPU_GP8Value { get => _cpu.Debug_GP8Value; }

    public uint Debug_CPU_PCValue { get => _cpu.Debug_PCValue; }
    public uint Debug_CPU_SRValue { get => _cpu.Debug_SRValue; }

    public void LaunchCycleLoop()
    {
        for (ulong cycle = 0; ; cycle++)
        {
            TimeSpan cycleTime = StepSingleCycle();
            Thread.Sleep((int)(1000.0 / ClockRateHz) - cycleTime.Milliseconds);
        }
    }

    public TimeSpan StepSingleCycle()
    {
        _currentCycle++;
        Stopwatch cycleTimer = Stopwatch.StartNew();
        _cpu.NextClock();
        cycleTimer.Stop();
        _lastCycleTimeMicroseconds = cycleTimer.ElapsedMilliseconds * 1000 + cycleTimer.Elapsed.Microseconds;
        Logging.LogDebug($"Cycle {CurrentCycle} took {_lastCycleTimeMicroseconds}us");
        return cycleTimer.Elapsed;
    }
}