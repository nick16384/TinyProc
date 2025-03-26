using TinyProc.Memory;
using TinyProc.Processor;

class Program
{
    public static readonly string TINYPROC_VERSION_STR = "0.0";
    static void Main(string[] args)
    {
        Console.WriteLine($"\nTinyProc ver. {TINYPROC_VERSION_STR}");

        Console.WriteLine("Creating memory object");
        RawMemory mem1 = new(256);

        mem1.WriteLine = true;
        mem1.AddressBus = 0x3A;
        mem1.DataBus = 0x1;
        mem1.AddressBus = 0x3B;
        mem1.DataBus = 0x1;
        mem1.WriteLine = false;

        CPU cpu = new(mem1);
        
        bool clockLevel = false;
        for (int clkEdges = 1;; clkEdges++)
        {
            if (!clockLevel)
                Console.WriteLine("\n====================================================================\n"
                    + $"Cycle {1 + clkEdges / 2}");
            clockLevel = !clockLevel;
            cpu.ClockLevel = clockLevel;
            //mem1.Debug_PrintAll();

            System.Threading.Thread.Sleep(1000);

            if (clkEdges > 5)
                break;
        }
        Console.WriteLine("\nLeaving cycle loop and exiting...");
    }
}