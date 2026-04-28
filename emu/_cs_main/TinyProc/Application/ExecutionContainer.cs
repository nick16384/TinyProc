using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TinyProc.Memory;
using TinyProc.Processor.CPU;
using static TinyProc.Processor.CPU.CPU;
using static TinyProc.Assembling.Assembler;
using TinyProc.Assembling;

namespace TinyProc.Application;

public class ExecutionContainer
{
    // The first (and only) instance of an ExecutionContainer, which is
    // exposed externally to be used by e.g. GUIs.
    private static ExecutionContainer? _instance0;
    public static ExecutionContainer INSTANCE0 { get => _instance0 ?? throw new Exception("Execution container INSTANCE0 not initialized yet."); }

    private const uint ROM_SIZE = 0x00010000;
    private const uint INITIAL_PROGRAM_BASE_OFFSET = 0x00030000;

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

    public static void Initialize(string firmwareImagePath)
    {
        if (_instance0 != null)
            throw new Exception("Cannot initialize multiple execution containers. Please use ExecutionContainer.INSTANCE0.");
        _instance0 = new ExecutionContainer(firmwareImagePath);
    }

    private ExecutionContainer(string firmwareImagePath)
    {
        Logging.LogDebug("Creating virtual hardware");
        Logging.LogDebug("Creating working memory & console memory objects");
        _mem1 = new RawMemory(RawMemory.FULL_SIZE);

        ROM firmwareRom = LoadFirmwareFromPath(firmwareImagePath);

        // TODO: Funny idea: Copy this image to a physical floppy and boot from it or something

        Logging.LogDebug("Creating CPU object, loading main program");
        _cpu = new(
            new Dictionary<uint, IMemoryDevice>
            {
                { 0x00000000u, firmwareRom },
                { firmwareRom._size, _mem1 }
            }
        );
        Logging.LogDebug("Done.");
    }

    public void ResetCPU() => _cpu.Reset();

    public ROM LoadFirmwareFromPath(string firmwareImagePath)
    {
        Logging.LogDebug($"Reading ROM image at {firmwareImagePath}");
        byte[] romDataBytes = File.ReadAllBytes(firmwareImagePath);
        uint[] romData = ByteArrayToUIntArray(romDataBytes);
        return new ROM(ROM_SIZE, romData);
        // ROM image created
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
        Logging.LogInfo($"Cycle {CurrentCycle} took {_lastCycleTimeMicroseconds}us");
        return cycleTimer.Elapsed;
    }
}
