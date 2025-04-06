using TinyProc.Assembler;
using TinyProc.Memory;
using TinyProc.Processor.CPU;

class Program
{
    public static readonly string TINYPROC_VERSION_STR = "0.2025.04";
    static void Main(string[] args)
    {
        Console.WriteLine($"\nTinyProc ver. {TINYPROC_VERSION_STR}");

        Console.WriteLine("Creating memory object");
        RawMemory mem1 = new(256);
        CPU cpu = new(mem1)
        {
            ClockLevel = false
        };

        // Miku = 39 = Sankyuu easter egg :)
        mem1.WriteEnable = true;
        mem1.AddressBus = 0x00000039u;
        mem1.DataBus = 0x39393939u;

        // Read assembly code from file
        string assemblyCode = File.ReadAllText("../../Test Programs/Counter.lltp-x25-32.asm");
        uint[] MAIN_PROGRAM = Assembler.AssembleToMachineCode(assemblyCode);

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
                Console.WriteLine($"\n\nCPU cycle {cycles}");
            }
            // CPU clock level oscillates between low (false) and high (true)
            cpu.ClockLevel = !cpu.ClockLevel;

            Thread.Sleep(100);
        }
    }

    /*private static readonly (uint, uint)[] MAIN_PROGRAM_INSTRUCTION_TUPLES =
    [
        // TODO: Next step: Assembler
        (0b110000_0000_00001_00000000000000000u, 0x00000078u), // LOAD GP1, 00000078
        (0b010000_0000_00001_00000000000000000u, 0x00000001u), // ADD GP1, 00000001
        (0b110010_0000_00001_00000000000000000u, 0x00000078u), // STORE GP1, 00000078
        (0b000001_0000_0000000000000000000000u, 0x00000000u) // JMP 00000000
    ];*/

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