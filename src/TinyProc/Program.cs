using TinyProc.Memory;

class Program
{
    public static readonly string TINYPROC_VERSION_STR = "0.0";
    static void Main(string[] args)
    {
        Console.WriteLine($"TinyProc ver. {TINYPROC_VERSION_STR}");

        Console.WriteLine("Creating memory object");
        RawMemory mem1 = new(16);
        mem1.Write(0x0, 0xFFFF_FFFF);
        mem1.Write(0x1, 0xFFFF_FFFF);
        mem1.Write(0x2, 0xFFFF);
        mem1.PrintAll();
    }
}