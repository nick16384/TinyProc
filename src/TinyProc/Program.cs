using TinyProc.Memory;

class Program
{
    public static readonly string TINYPROC_VERSION_STR = "0.0";
    static void Main(string[] args)
    {
        Console.WriteLine($"TinyProc ver. {TINYPROC_VERSION_STR}");

        Console.WriteLine("Creating memory object");
        RawMemory mem1 = new(16);
        mem1.WriteLine = true;
        
        mem1.AddressBusData = 0x0;
        mem1.DataBusData = 0xFFFF_FFFF;

        mem1.AddressBusData = 0x1;
        mem1.DataBusData = 0xFFFF_FFFF;

        mem1.AddressBusData = 0x2;
        mem1.DataBusData = 0x0000_FFFF;

        mem1.ReadLine = true;
        mem1.Dbg_PrintAll();
    }
}