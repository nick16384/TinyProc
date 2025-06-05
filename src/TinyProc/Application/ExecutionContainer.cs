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
    // TODO: Maybe use some sort of List<byte> instead of byte[] to overcome this limit
    public byte[] LiveRAMBytes
    {
        get
        {
            int byteArraySize;
            if ((ulong)_mem1._words * sizeof(uint) > int.MaxValue)
            {
                byteArraySize = int.MaxValue;
                Logging.LogWarn(
                    $"Accessing RAM bytes only allows {int.MaxValue} " +
                    $"bytes to be read, however, the actual RAM is larger with {(ulong)_mem1._words * sizeof(uint)} " +
                    "bytes. Only a partial amount is returned!");
            }
            else
                byteArraySize = (int)_mem1._words * sizeof(uint);

            byte[] ramBytes = new byte[byteArraySize];
            // Using manual copy from uint[] to byte[], since Buffer.BlockCopy produces little-endian results.
            for (int i = 0; i < byteArraySize / sizeof(uint); i++)
            {
                ramBytes[i * 4 + 0] = (byte)((_mem1.Data[0][i] & 0xFF000000) >> 24);
                ramBytes[i * 4 + 1] = (byte)((_mem1.Data[0][i] & 0x00FF0000) >> 16);
                ramBytes[i * 4 + 2] = (byte)((_mem1.Data[0][i] & 0x0000FF00) >> 8);
                ramBytes[i * 4 + 3] = (byte)((_mem1.Data[0][i] & 0x000000FF) >> 0);
            }
            return ramBytes;
        }
    }

    public void LaunchMainLoop()
    {
        for (ulong cycle = 0; ; cycle++)
        {
            TimeSpan cycleTime = StepSingleCycle(cycle);
            Thread.Sleep((int)(1000.0 / GlobalData.ClockRateHz) - cycleTime.Milliseconds);
        }
    }

    public TimeSpan StepSingleCycle(ulong? cycle = null)
    {
        Stopwatch cycleTimer = Stopwatch.StartNew();
        _cpu.NextClock();
        cycleTimer.Stop();
        long cycleTimeMicroseconds = cycleTimer.ElapsedMilliseconds * 1000 + cycleTimer.Elapsed.Microseconds;
        if (cycle == null)
            Logging.LogDebug($"Cycle took {cycleTimeMicroseconds}us");
        else
            Logging.LogDebug($"Cycle {cycle} took {cycleTimeMicroseconds}us");
        return cycleTimer.Elapsed;
    }
}