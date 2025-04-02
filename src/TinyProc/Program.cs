using TinyProc.Memory;
using TinyProc.Processor;

class Program
{
    public static readonly string TINYPROC_VERSION_STR = "0.2025.02";
    static void Main(string[] args)
    {
        Console.WriteLine($"\nTinyProc ver. {TINYPROC_VERSION_STR}");

        Console.WriteLine("Creating memory object");
        RawMemory mem1 = new(256);
        CPU cpu = new(mem1)
        {
            ClockLevel = false
        };

        // Miku = Sankyuu easter egg :)
        mem1.WriteEnable = true;
        mem1.AddressBus = 0x00000039u;
        mem1.DataBus = 0x39393939u;

        Console.WriteLine("Loading program into memory.");
        LoadDataIntoMemory(MAIN_PROGRAM, mem1, 0x00000000u);
        mem1.Debug_DumpAll();
        Console.WriteLine("Done.");

        Console.WriteLine("\nDecide, whether to use auto or manual clock.");
        Console.WriteLine("For auto, press \"a\" key; For manual, press any other key.");
        char? input = (char)Console.Read();
        bool isAutoClock = input == 'a' || input == 'A';
        Console.WriteLine($"Auto clock enabled: {isAutoClock}");

        Console.CancelKeyPress += delegate
        {
            Console.WriteLine("\nLeaving cycle loop and exiting...");
        };

        long cycles = 0;
        // Main clock loop
        while (true)
        {
            if (!cpu.ClockLevel)
            {
                if (!isAutoClock)
                    // Wait until the user presses enter for the next time
                    Console.ReadLine();
                cycles++;
                Console.WriteLine(
                    "\n====================================================================\n" +
                    $"CPU cycle {cycles}"
                );
            }
            // CPU clock level oscillates between low (false) and high (true)
            cpu.ClockLevel = !cpu.ClockLevel;

            Thread.Sleep(100);
        }
    }

    private static readonly (uint, uint)[] MAIN_PROGRAM_INSTRUCTION_TUPLES =
    [
        (0x77777777u, 0x00000000u),
        (0x00000000u, 0x00000000u),
        (0x0, 0x0),
        (0x00000063u, 0x00000064u)
    ];
    private static readonly uint[] MAIN_PROGRAM = [.. MAIN_PROGRAM_INSTRUCTION_TUPLES.SelectMany(t => new uint[] { t.Item1, t.Item2 })];

    private static void LoadDataIntoMemory(uint[] data, RawMemory mem, uint startAddress)
    {
        Console.WriteLine($"Loading {data.Length} words into memory (size {mem._words} words) starting at address {startAddress:X8}");
        if (startAddress + data.Length > mem._words)
            throw new ArgumentOutOfRangeException("Start address + Data size > Memory size");
        mem.WriteEnable = true;
        for (uint i = 0; i < data.Length; i++)
        {
            mem.AddressBus = startAddress + i;
            mem.DataBus = data[i];
        }
        Console.WriteLine("Memory write successful.");
    }
}