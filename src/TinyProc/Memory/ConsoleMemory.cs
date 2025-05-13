namespace TinyProc.Memory;

public class ConsoleMemory(uint words) : RawMemory(words, [])
{
    public bool EnableConsoleOutput = true;

    private protected override void Write(uint addr, uint value)
    {
        base.Write(addr, value);
        if (EnableConsoleOutput)
            PrintToConsole();
    }

    private void PrintToConsole()
    {
        Console.WriteLine("CON memory received update; Current contents:");
        for (uint address = 0; address < _words; address++)
        {
            uint data = Read(address);
            char c1 = (char)((data & 0xFF000000) >> 24);
            char c2 = (char)((data & 0x00FF0000) >> 16);
            char c3 = (char)((data & 0x0000FF00) >> 8);
            char c4 = (char)((data & 0x000000FF) >> 0);
            if (c1 >= 0x20 && c1 <= 0x7E) { Console.Write($"{c1}"); } else { Console.Write("."); }
            if (c2 >= 0x20 && c2 <= 0x7E) { Console.Write($"{c2}"); } else { Console.Write("."); }
            if (c3 >= 0x20 && c3 <= 0x7E) { Console.Write($"{c3}"); } else { Console.Write("."); }
            if (c4 >= 0x20 && c4 <= 0x7E) { Console.Write($"{c4}"); } else { Console.Write("."); }
        }
        Console.WriteLine();
    }
}