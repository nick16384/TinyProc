using TinyProc.Memory;
using TinyProc.Processor;

class Program
{
    public static readonly string TINYPROC_VERSION_STR = "0.0";
    static void Main(string[] args)
    {
        Console.WriteLine($"\nTinyProc ver. {TINYPROC_VERSION_STR}");

        Console.WriteLine("Creating memory object");
        RawMemory mem1 = new(32);
        CPU cpu = new(mem1);
        
        for (int cycle = 1;; cycle++)
        {
            Console.WriteLine("====================================================================\n"
                + $"Cycle {cycle}");
            cpu.NextClock();
            mem1.Dbg_PrintAll();
            Console.WriteLine();
            System.Threading.Thread.Sleep(1000);

            if (cycle > 0)
                break;
        }
        Console.WriteLine("Leaving cycle loop and exiting...");
    }
}