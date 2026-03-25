using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    private static ExecutionContainer _instance0 = Initialize();
    public static ExecutionContainer INSTANCE0 { get => _instance0 ?? Initialize(); }

    private const string RESET_ASM_PROGRAM_PATH = "System Programs/00000000_Reset.hltp32.asm";
    private const string LOADER_ASM_PROGRAM_PATH = "System Programs/00000100_Loader.hltp32.asm";

    private const uint ROM_SIZE = 0x00010000;
    private const uint INITIAL_PROGRAM_BASE_OFFSET = 0x00030000;

    private readonly ROM _rom1;
    private readonly RawMemory _mem1;
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
        _instance0 = new ExecutionContainer();
        return _instance0;
    }

    private ExecutionContainer()
    {
        Logging.LogDebug("Creating virtual hardware");
        Logging.LogDebug("Creating working memory & console memory objects");
        _mem1 = new RawMemory(RawMemory.FULL_SIZE);

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
        _rom1 = new ROM(ROM_SIZE, romData);

        Logging.LogDebug("Creating CPU object, loading main program");
        _cpu = new(
            _rom1,
            new Dictionary<uint, RawMemory>
            {
                { _rom1._size, _mem1 }
            }
        );

        Logging.LogDebug("Reading loaded program.");
        if (_mem1._numWords < 4096)
            _mem1.Debug_DumpAll();
        else
            Logging.LogDebug("Memory object too large to dump.");
        Logging.LogDebug("Done.");

        _instance0 ??= this;
    }

    public void ResetCPU() => _cpu.Reset();

    public void LoadInitialProgram(ExecutableWrapper executable)
    {
        for (uint i = 0; i < executable.Program.Length; i++)
        {
            _mem1.WriteDirect(INITIAL_PROGRAM_BASE_OFFSET - _rom1._size + i, executable.Program[i]);
        }
    }

    public void LoadBytesAtAddress(byte[] byteData, uint address)
    {
        uint[] dataAsUIntArray = new uint[byteData.Length / sizeof(uint)];
        BinaryPrimitives.ReverseEndianness(Unsafe.As<uint[]>(byteData), dataAsUIntArray);
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

    /// <summary>
    /// Dumps the entire address space as a byte array.
    /// Warning: Accessing this property may have a significant performance impact when run after each CPU cycle, since it takes
    /// comparatively long to compute the byte[] resulting from the current RawMemory's internal paging layout.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ReadOnlySpan<byte> GetFullMemoryDump()
    {
        // TODO: Either lazy-evaluate, or write a Read(address) method to prevent a huge byte[] array from being created
        // and slowing everything down.
        // If that won't do, rewrite the hex editor part of the GUI, such that it no longer updates every second, but instead
        // only on user button presses.
        /*
        byte[] virtualMemoryBytes = [];
        virtualMemoryBytes = [.. virtualMemoryBytes, .. ConvertUIntArrayToByteArrayAndReverseEndianness(_rom1.FixedData)];
        virtualMemoryBytes = [.. virtualMemoryBytes, .. ConvertUIntArrayToByteArrayAndReverseEndianness(_mem1.Data[0])];
        return virtualMemoryBytes;

        uint[] uintArrayReversedEndian = new uint[uintArraySource.Length];
        // This method is the fastest way for endian reversal, since it already utilizes
        // bitwise manipulations using advanced AVX / AVX2 SIMD instructions supported by most modern x86 CPUs.
        // There is no need to implement anything faster by hand.
        BinaryPrimitives.ReverseEndianness(uintArraySource, uintArrayReversedEndian);

        return MemoryMarshal.AsBytes<uint>(uintArrayReversedEndian);
        */
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Reads a word directly from the underlying virtual address space of the MMU.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public uint ReadVirtualMemDirect(uint address) => _cpu.DebugPort.ReadVirtualMemDirect(address);
    /// <summary>
    /// Writes a word directly to the underlying virtual address space of the MMU.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    public void WriteVirtualMemDirect(uint address, uint value) => _cpu.DebugPort.WriteVirtualMemDirect(address, value);

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