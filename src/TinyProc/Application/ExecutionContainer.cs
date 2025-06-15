using System.Buffers.Binary;
using System.Diagnostics;
using TinyProc.Memory;
using TinyProc.Processor.CPU;
using static TinyProc.Processor.CPU.CPU;

namespace TinyProc.Application;

public class ExecutionContainer
{
    // The first (and almost always only) instance of an ExecutionContainer, which is
    // exposed externally to be used by e.g. GUIs.
    public static ExecutionContainer INSTANCE0;

    private readonly RawMemory _mem1;
    private readonly ConsoleMemory _tmem1;
    private readonly CPU _cpu;
    public CPUDebugPort CPUDebugPort { get => _cpu.DebugPort; }

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

    public uint ReadRAMDirect(uint address) => _mem1.ReadDirect(address);
    
    public void WriteRAMDirect(uint address, int uintOffset, byte value)
        => _mem1.WriteDirect(address, _mem1.ReadDirect(address) & ((uint)value << (uintOffset * 8)));
    // Returns the bytes that currently reside within the working memory.
    // Note that using a byte[] instead of a uint[][] is not guaranteed to return all RAM data (because of C# limitations).
    // TODO: Maybe use some sort of List<byte> instead of byte[] to overcome this limit
    // Warning: Accessing this property may have a significant performance impact when run after each CPU cycle, since it takes
    // comparatively long to compute the byte[] resulting from the current RawMemory's uint[][].
    public byte[] LiveRAMBytes { get => UIntArrayToByteArray(_mem1.Data[0]); }

    private static List<long> timesEndian = [];
    
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
        // Copies the entire memory chunk of the uint array to the byte array
        Buffer.BlockCopy(uintArray, 0, byteArray, 0, byteArraySize);

        Stopwatch sw = Stopwatch.StartNew();

        /*for (int i = 0; i < byteArraySize / sizeof(uint); i++)
        {
            if ((byteArray[i * 4 + 0] ^ byteArray[i * 4 + 1] ^ byteArray[i * 4 + 1] ^ byteArray[i * 4 + 2]) == 0)
                continue;
            Array.Reverse(byteArray, i * 4, 4);
        }*/
        
        // TODO: Make this pointer magic work
        /*unsafe
        {
            fixed (byte* byteArrayPtr = byteArray)
            {
                uint[] uintArray2 = new uint[uintArray.Length];
                Buffer.BlockCopy(uintArray, 0, uintArray2, 0, uintArray.Length * sizeof(uint));
                BinaryPrimitives.ReverseEndianness(uintArray, uintArray2);
                fixed (uint* uintArrayPtr = uintArray2)
                {
                    return (byte[])(byte*)uintArrayPtr;
                }
            }
        }*/

        Parallel.For(0, byteArraySize / sizeof(uint), i =>
        {
            // If all bytes are equal, no reverse is needed
            // Reduces runtime by around 30%
            if ((byteArray[i * 4 + 0] ^ byteArray[i * 4 + 1] ^ byteArray[i * 4 + 1] ^ byteArray[i * 4 + 2]) == 0)
                return;
            Array.Reverse(byteArray, i * 4, 4);

            // Unused method for unsafe endian reversing
            // Sometimes faster, sometimes slower than safe endian reverse
            // Some timing tests below; ST = Single-threaded (no Parallel.For); AOT = Ahead-of-time compiled
            // Avg safe:          17.5ms
            // Avg safe AOT:      07.6ms
            // Avg safe ST:       46.8ms
            // Avg safe ST AOT:   18.5ms
            // Avg unsafe:        16.6ms
            // Avg unsafe AOT:    07.6ms
            // Avg unsafe ST:     52.9ms
            // Avg unsafe ST AOT: 14.6ms
            /*unsafe
            {
                fixed (byte* byteArrayPtr = &byteArray[i * 4])
                {
                    uint* uintLittleEndianPtr = (uint*)byteArrayPtr;
                    uint uintLittleEndian = *uintLittleEndianPtr;
                    if (uintLittleEndian == 0 || uintLittleEndian == 0xFFFFFFFF) return;
                    uint uintBigEndian =
                        ((uintLittleEndian & 0xFF000000) >> 24) |
                        ((uintLittleEndian & 0x00FF0000) >> 08) |
                        ((uintLittleEndian & 0x0000FF00) << 08) |
                        ((uintLittleEndian & 0x000000FF) << 24);
                    *uintLittleEndianPtr = uintBigEndian;
                }
            }*/
        });
        timesEndian.Add(sw.ElapsedMilliseconds);
        Console.WriteLine($"Endian {sw.ElapsedMilliseconds}ms Avg {timesEndian.Average()}ms");

        return byteArray;
    }

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