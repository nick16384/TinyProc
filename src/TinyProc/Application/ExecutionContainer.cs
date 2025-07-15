using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TinyProc.Memory;
using TinyProc.Processor.CPU;
using static TinyProc.Processor.CPU.CPU;

namespace TinyProc.Application;

public class ExecutionContainer
{
    // The first (and almost always only) instance of an ExecutionContainer, which is
    // exposed externally to be used by e.g. GUIs.
    public static ExecutionContainer? INSTANCE0 { get; set; }

    private readonly RawMemory _mem1;
    private readonly ConsoleMemory _tmem1;
    private readonly CPU _cpu;
    public CPUDebugPort CPUDebugPort { get => _cpu.DebugPort; }
    // An invalid CPU state is raised, when the CPU is unable to execute the next instruction,
    // since an attempt would cause an immediate exception to be thrown.
    // This could be caused by e.g. the PC pointing to unmapped memory.
    // If this flag is set, the CPU will be locked and is not able to execute any further instructions
    // inside this ExecutionContainer.
    private bool _isCPUInInvalidState = false;
    public bool IsCPUInInvalidState { get => _isCPUInInvalidState; }

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

        INSTANCE0 ??= this;
    }

    public void LoadBytesAtAddress(byte[] byteData, uint address)
    {
        uint[] dataAsUIntArray = ConvertByteArrayToUIntArrayAndReverseEndianness(byteData).ToArray();
        LoadDataAtAddress(dataAsUIntArray, address);
    }

    public void LoadDataAtAddress(uint[] data, uint address)
    {
        for (uint i = 0; i < data.Length; i++)
        {
            _mem1.WriteDirect(address + i, data[i]);
        }
    }

    public uint VirtualMemorySizeWords { get => (uint)(_mem1.TotalSizeBits / 32); }

    public uint[][] LiveMemoryDump {
        get
        {
            uint[][] memoryDump = new uint[_mem1.Data.Length][];
            for (int i = 0; i < memoryDump.Length; i++)
                memoryDump[i] = new uint[_mem1.Data[i].Length];
            _mem1.Data.CopyTo(memoryDump.AsMemory());
            return memoryDump;
        }}

    // TODO: Replace direct read / write methods with an array and underlying yield methods.
    public uint ReadRAMDirect(uint address) => _mem1.ReadDirect(address);
    
    public void WriteRAMDirect(uint address, int uintOffset, byte value)
        => _mem1.WriteDirect(address, _mem1.ReadDirect(address) & ((uint)value << (uintOffset * 8)));
    // Returns the bytes that currently reside within the working memory.
    // Note that using a byte[] instead of a uint[][] is not guaranteed to return all RAM data (because of C# limitations).
    // TODO: Maybe use some sort of List<byte> instead of byte[] to overcome this limit
    // Warning: Accessing this property may have a significant performance impact when run after each CPU cycle, since it takes
    // comparatively long to compute the byte[] resulting from the current RawMemory's uint[][].
    public ReadOnlySpan<byte> LiveRAMBytes { get => ConvertUIntArrayToByteArrayAndReverseEndianness(_mem1.Data[0]); }
    
    private static ReadOnlySpan<byte> ConvertUIntArrayToByteArrayAndReverseEndianness(uint[] uintArray)
    {
        if ((ulong)uintArray.Length * sizeof(uint) > int.MaxValue)
        {
            Logging.LogWarn(
                $"Accessing a uint[] as byte[] only allows {int.MaxValue} " +
                $"bytes to be read, however, the actual uint[] is larger with {(ulong)uintArray.Length * sizeof(uint)} " +
                "bytes. Only a partial amount is returned!");
        }

        uint[] uintArrayBigEndian = new uint[uintArray.Length];
        // This method is the fastest way for endian reversal, since it already utilizes
        // bitwise manipulations using advanced AVX / AVX2 SIMD instructions supported by most modern x86 CPUs.
        // There is no need to implement anything faster by hand.
        BinaryPrimitives.ReverseEndianness(uintArray, uintArrayBigEndian);

        return MemoryMarshal.AsBytes<uint>(uintArrayBigEndian);
    }
    private static ReadOnlySpan<uint> ConvertByteArrayToUIntArrayAndReverseEndianness(byte[] byteArray)
    {
        if (byteArray.Length % sizeof(uint) != 0)
            throw new ArgumentException("Cannot convert byte[] to uint[]: Size not divisible by 4.");
        ReadOnlySpan<uint> uintArray = MemoryMarshal.Cast<byte, uint>(byteArray);
        Span<uint> uintArrayReversedEndian = new uint[byteArray.Length / 4];
        BinaryPrimitives.ReverseEndianness(uintArray, uintArrayReversedEndian);
        return uintArrayReversedEndian;
    }

    public TimeSpan StepSingleCycle()
    {
        if (IsCPUInInvalidState)
            return TimeSpan.Zero;
        _currentCycle++;
        Stopwatch cycleTimer = Stopwatch.StartNew();
        try { _cpu.NextClock(); }
        catch (Exception) { _isCPUInInvalidState = true; throw; }
        cycleTimer.Stop();
        _lastCycleTimeMicroseconds = cycleTimer.ElapsedMilliseconds * 1000 + cycleTimer.Elapsed.Microseconds;
        Logging.LogDebug($"Cycle {CurrentCycle} took {_lastCycleTimeMicroseconds}us");
        return cycleTimer.Elapsed;
    }
}