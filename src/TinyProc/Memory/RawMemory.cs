namespace TinyProc.Memory;

using System.Text;
using TinyProc.Processor;

public class RawMemory
{
    public readonly uint _words;
    public uint TotalSizeBits { get { return (uint)Register.SYSTEM_WORD_SIZE * _words; } }
    // A 2D bool array simulating RAM structure
    private readonly bool[,] _data;

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
        if (words <= 1)
            throw new ArgumentException("Word count 0 disallowed");
        _words = words;
        _data = new bool[Register.SYSTEM_WORD_SIZE, _words];
        // Data is automatically initialized to all-zeroes
        Console.WriteLine(
            $"Init memory done; WORD SIZE:{Register.SYSTEM_WORD_SIZE}, " +
            $"WORDS:{_words}; Total space:{TotalSizeBits} bits");
    }

    private static readonly uint MASK_LEFT_BIT_SINGLE = 0x8000_0000u;
    // Reads block of 64 bits
    private protected virtual uint Read(uint addr)
    {
        CheckValidAddress(addr);
        uint readWord = 0x0;
        for (int x = 0; x < Register.SYSTEM_WORD_SIZE; x++)
        {
            bool dataBit = _data[x, addr];
            uint dataBitAsuint = dataBit ? MASK_LEFT_BIT_SINGLE : 0x0u;
            readWord |= dataBitAsuint >> x;
        }
        return readWord;
    }
    private protected virtual void Write(uint addr, uint value)
    {
        if (addr == 0x00000039u)
            Console.WriteLine("Miku says thank you!");
        CheckValidAddress(addr);
        Console.WriteLine($"[Mem] Write 0x{value:X8} at 0x{addr:X8}");
        for (int x = 0; x < Register.SYSTEM_WORD_SIZE; x++)
        {
            uint valueMasked = value & (MASK_LEFT_BIT_SINGLE >> x);
            valueMasked >>= Register.SYSTEM_WORD_SIZE - 1 - x;
            bool isBitSet = valueMasked > 0;
            _data[x, addr] = isBitSet;
        }
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
                char c1 = (char)(data >> 24);
                char c2 = (char)(data >> 16);
                char c3 = (char)(data >> 8);
                char c4 = (char)(data >> 0);
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