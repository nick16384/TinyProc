using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TinyProc.Assembling.Sections;
using TinyProc.Memory;
using TinyProc.Processor.CPU;
using static TinyProc.Processor.CPU.CPU;

namespace TinyProc.Application;

public class ExecutionContainer
{
    // The first (and almost always only) instance of an ExecutionContainer, which is
    // exposed externally to be used by e.g. GUIs.
    public static ExecutionContainer? INSTANCE0 { get; set; }

    private const string RESET_ASM_PROGRAM_PATH = "System Programs/00000000_Reset.hltp32.asm";
    private const string LOADER_ASM_PROGRAM_PATH = "System Programs/00000100_Loader.hltp32.asm";

    private const uint INITIAL_PROGRAM_BASE_OFFSET = 0x00030000;

    private readonly ROM _rom1;
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

    public static ExecutionContainer Initialize()
    {
        INSTANCE0 = new ExecutionContainer();
        return INSTANCE0;
    }

    private ExecutionContainer()
    {
        Logging.LogDebug("Creating virtual hardware");
        Logging.LogDebug("Creating working memory & console memory objects");
        Logging.LogWarn("Warning: Using dynamically growing RAM not implemented. Using arbitrary size 0x50000000!");
        _mem1 = new RawMemory(0x50000000);
        _tmem1 = new ConsoleMemory(0x200);

        Logging.LogDebug("Assembling Reset and Loader programs");
        string resetProgramCode = File.ReadAllText(RESET_ASM_PROGRAM_PATH);
        string loaderProgramCode = File.ReadAllText(LOADER_ASM_PROGRAM_PATH);
        (uint, DataSection, TextSection) resetProgram = Assembling.Assembler.AssembleToDirectMachineCode(resetProgramCode);
        (uint, DataSection, TextSection) loaderProgram = Assembling.Assembler.AssembleToDirectMachineCode(loaderProgramCode);
        Logging.LogDebug("Saving Reset and Loader programs in ROM");
        uint resetLoadAddress = resetProgram.Item3.FixedLoadAddress.GetValueOrDefault(0x0);
        uint loaderLoadAddress = loaderProgram.Item3.FixedLoadAddress.GetValueOrDefault(0x0);
        List<uint> resetExecutableProgram = resetProgram.Item3.BinaryRepresentation;
        List<uint> loaderExecutableProgram = loaderProgram.Item3.BinaryRepresentation;
        uint[] romData = new uint[loaderLoadAddress + loaderProgram.Item3.Size];
        Array.Copy(resetExecutableProgram.ToArray(), 0, romData, resetLoadAddress, resetExecutableProgram.Count);
        Array.Copy(loaderExecutableProgram.ToArray(), 0, romData, loaderLoadAddress, loaderExecutableProgram.Count);
        _rom1 = new ROM(romData);

        // FIXME: Make debug dump of loader program (ensure intng has correct address 0x0)
        Console.Error.WriteLine("Debug: Dumping loader exec binary.");
        File.WriteAllBytes("System Programs/00000100_Loader.hltp32.bin",
            ExecutableWrapper.UIntArrayToByteArray(loaderExecutableProgram.ToArray()));
        Environment.Exit(1);

        Logging.LogDebug("Creating CPU object, loading main program");
        _cpu = new(
            _rom1,
            new Dictionary<uint, RawMemory>
            {
                { _rom1._size - 1, _mem1 },
                { _rom1._size - 1 + _mem1._words, _tmem1 }
            }
        );

        Logging.LogDebug("Reading loaded program.");
        if (_mem1._words < 4096)
            _mem1.Debug_DumpAll();
        else
            Logging.LogDebug("Memory object too large to dump.");
        Logging.LogDebug("Done.");

        INSTANCE0 ??= this;
    }

    public void ResetCPU() => _cpu.Reset();

    public void LoadInitialProgram(ExecutableWrapper executable)
    {
        for (uint i = 0; i < executable.ExecutableProgram.Length; i++)
        {
            _mem1.WriteDirect(INITIAL_PROGRAM_BASE_OFFSET - _rom1._size + i, executable.ExecutableProgram[i]);
        }
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
    public ReadOnlySpan<byte> LiveVirtualMemoryBytes
    {
        get
        {
            byte[] virtualMemoryBytes = [];
            virtualMemoryBytes = [.. virtualMemoryBytes, .. ConvertUIntArrayToByteArrayAndReverseEndianness(_rom1.FixedData)];
            virtualMemoryBytes = [.. virtualMemoryBytes, .. ConvertUIntArrayToByteArrayAndReverseEndianness(_mem1.Data[0])];
            return virtualMemoryBytes;
        }
    }

    public const int MAX_BYTES_READ_ALLOWED = 0xFFFFF;
    private static ReadOnlySpan<byte> ConvertUIntArrayToByteArrayAndReverseEndianness(uint[] uintArraySource)
    {
        if ((ulong)uintArraySource.Length * sizeof(uint) > MAX_BYTES_READ_ALLOWED/*int.MaxValue*/)
        {
            Logging.LogWarn(
                $"Accessing a uint[] as byte[] only allows {int.MaxValue:N0} " +
                $"bytes to be read, however, the actual uint[] is larger with {(ulong)uintArraySource.Length * sizeof(uint):N0} " +
                "bytes. Only a partial amount is returned!");
            uintArraySource = uintArraySource[..(MAX_BYTES_READ_ALLOWED / sizeof(uint))];
        }

        uint[] uintArrayReversedEndian = new uint[uintArraySource.Length];
        // This method is the fastest way for endian reversal, since it already utilizes
        // bitwise manipulations using advanced AVX / AVX2 SIMD instructions supported by most modern x86 CPUs.
        // There is no need to implement anything faster by hand.
        BinaryPrimitives.ReverseEndianness(uintArraySource, uintArrayReversedEndian);

        return MemoryMarshal.AsBytes<uint>(uintArrayReversedEndian);
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