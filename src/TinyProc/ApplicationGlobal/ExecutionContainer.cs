using System.Diagnostics;
using TinyProc.Memory;
using TinyProc.Processor.CPU;

namespace TinyProc.ApplicationGlobal;

public class ExecutionContainer
{
    // The first (and almost always only) instance of an ExecutionContainer, which is
    // exposed externally to be used by e.g. GUIs.
    public readonly ExecutionContainer INSTANCE0;

    private readonly RawMemory _mem1;
    private readonly ConsoleMemory _tmem1;
    private readonly CPU _cpu;

    public ExecutionContainer(uint ramStart, uint ramEnd, uint conStart, uint conEnd, uint[] mainProgram, uint entryPoint)
    {
        uint ramSize = ramEnd - ramStart + 1;
        uint conSize = conEnd - conStart + 1;
        Console.WriteLine("Creating virtual hardware");
        Console.WriteLine("Creating working memory & console memory objects");
        Console.WriteLine($"{ramSize}, {conSize}");
        _mem1 = new RawMemory(ramSize, mainProgram);
        _tmem1 = new ConsoleMemory(conSize);
        
        Console.WriteLine("Creating CPU object, loading main program");
        _cpu = new(new Dictionary<(uint, uint), RawMemory>
            {
                { (ramStart, ramEnd), _mem1 },
                { (conStart, conEnd), _tmem1 }
            }, entryPoint
        );

        Console.WriteLine("Reading loaded program.");
        if (_mem1._words < 4096)
            _mem1.Debug_DumpAll();
        else
            Console.WriteLine("Memory object too large to dump.");
        Console.WriteLine("Done.");

        // Miku = 39 = Sankyuu easter egg ^-^
        //mem1.WriteEnable = true;
        //mem1.AddressBus = 0x00000039u;
        //mem1.DataBus = 0x39393939u;

        if (INSTANCE0 == null)
            INSTANCE0 = this;
    }

    public void LaunchMainLoop()
    {
        for (ulong cycle = 0;; cycle++)
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
            Console.WriteLine($"Cycle took {cycleTimeMicroseconds}us");
        else
            Console.WriteLine($"Cycle {cycle} took {cycleTimeMicroseconds}us");
        return cycleTimer.Elapsed;
    }
}