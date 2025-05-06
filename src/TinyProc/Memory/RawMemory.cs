namespace TinyProc.Memory;

using System.Collections;
using TinyProc.Processor;

public class RawMemory
{
    public readonly uint _words;
    public ulong TotalSizeBits { get => (ulong)_words * (ulong)Register.SYSTEM_WORD_SIZE; }
    // The data array can hold a maximum of ~4 billion elements, however an array can
    // only hold up to ~2 billion elements in C#.
    // This necessitates the usage of an array of uint arrays, that can hold up the required amount
    // of uints in total.
    // Externally, this appears as one continuous address space.
    private readonly uint[][] _data;

    // TODO: Attach to true bus object

    // Logic that allows only either write or read line to be set:
    private bool _readEnable;
    public bool ReadEnable
    {
        get => _readEnable;
        set { _readEnable = value; _writeEnable = !value && _writeEnable; }
    }
    private bool _writeEnable;
    public bool WriteEnable
    {
        get => _writeEnable;
        set { _writeEnable = value; _readEnable = !value && _readEnable; }
    }

    public uint AddressBus { get; set; } = 0x0u;
    public uint DataBus
    {
        get => Read(AddressBus);
        set
        {
            if (WriteEnable)
                Write(AddressBus, value);
        }
    }

    public RawMemory(uint words)
    {
        if (words <= 0)
            throw new ArgumentException("Word count 0 disallowed");
        if (words > 1_000_000)
            Console.Error.WriteLine(
                "Warning: *Attempting* to initialize very large memory (>1,000,000 words). Expect out-of-memory errors.");
        _words = words;
        if (_words > int.MaxValue)
            _data = [new uint[int.MaxValue], [_words - int.MaxValue]];
        else
            _data = [new uint[_words]];
        // Data is automatically initialized to all-zeroes
        Console.WriteLine(
            $"Init memory done; WORD SIZE:{Register.SYSTEM_WORD_SIZE}, " +
            $"WORDS:{_words}; Total space:{TotalSizeBits} bits");
    }

    // Reads block of 32 bits
    private protected virtual uint Read(uint addr)
    {
        CheckValidAddress(addr);
        if (addr > int.MaxValue)
            return _data[1][addr - int.MaxValue + 1];
        else
            return _data[0][addr];
    }
    private protected virtual void Write(uint addr, uint value)
    {
        if (addr == 0x39u || addr == 39)
            Console.WriteLine("Miku says thank you!");
        CheckValidAddress(addr);
        if (addr > int.MaxValue)
            _data[1][addr - int.MaxValue + 1] = value;
        else
            _data[0][addr] = value;
    }

    public void Debug_DumpAll()
    {
        Console.WriteLine("[Mem] Dumping full memory contents (Big endian)");
        int addressesPerLine = 4;
        for (uint baseAddr = 0; baseAddr < _words; baseAddr += 4)
        {
            Console.Write($"{baseAddr:X8}:");
            // Print address values as hexadecimal
            for (uint subAddr = 0; subAddr < addressesPerLine; subAddr++)
            {
                uint addr = baseAddr + subAddr;
                Console.Write($" {Read(addr):X8}");
            }
            Console.Write("   ");
            // Print address values decoded as ASCII
            for (uint subAddr = 0; subAddr < addressesPerLine; subAddr++)
            {
                uint addr = baseAddr + subAddr;
                uint data = Read(addr);
                char c1 = (char)((data & 0xFF000000) >> 24);
                char c2 = (char)((data & 0x00FF0000) >> 16);
                char c3 = (char)((data & 0x0000FF00) >> 8);
                char c4 = (char)((data & 0x000000FF) >> 0);
                if (c1 >= 0x20 && c1 <= 0x7E) { Console.Write($" {c1} "); } else { Console.Write(" . "); }
                if (c2 >= 0x20 && c2 <= 0x7E) { Console.Write($" {c2} "); } else { Console.Write(" . "); }
                if (c3 >= 0x20 && c3 <= 0x7E) { Console.Write($" {c3} "); } else { Console.Write(" . "); }
                if (c4 >= 0x20 && c4 <= 0x7E) { Console.Write($" {c4} "); } else { Console.Write(" . "); }
            }
            Console.WriteLine();
        }
    }

    // Checks if address is below TotalSizeBits. If not, an exception is thrown.
    private void CheckValidAddress(uint addr)
    {
        if (addr > _words - 1)
            throw new ArgumentOutOfRangeException($"Error reading memory: Address 0x{addr:X} above 0x{(_words - 1):X}.");
    }
}