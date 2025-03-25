namespace TinyProc.Memory;

using TinyProc.Processor;

public class RawMemory
{
    public readonly uint _words;
    public uint TotalSizeBits { get { return Register.SYSTEM_WORD_SIZE * _words; } }
    // A 2D bool array simulating RAM structure
    private readonly bool[,] _data;

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
    public uint Read(uint addr)
    {
        CheckValidAddress(addr);
        uint readWord = 0x0;
        for (uint x = 0; x < Register.SYSTEM_WORD_SIZE; x++)
        {
            bool dataBit = _data[x, addr];
            uint dataBitAsuint = dataBit ? MASK_LEFT_BIT_SINGLE : 0x0u;
            readWord |= dataBitAsuint >> (int)x;
        }
        return readWord;
    }
    public void Write(uint addr, uint value)
    {
        CheckValidAddress(addr);
        Console.WriteLine($"Write 0x{value:X} at 0x{addr:X}");
        for (uint x = 0; x < Register.SYSTEM_WORD_SIZE; x++)
        {
            uint valueMasked = value & (MASK_LEFT_BIT_SINGLE >> (int)x);
            valueMasked >>= (int)(Register.SYSTEM_WORD_SIZE - 1 - x);
            bool isBitSet = valueMasked > 0;
            _data[x, addr] = isBitSet;
        }
    }

    public void PrintAll()
    {
        Console.WriteLine("Printing full memory contents");
        // Always switch between printing left and right (2 memory addresses per line)
        bool printLeft = true;
        for (uint y = 0; y < _words; y++)
        {
            if (printLeft)
                Console.Write($"{y:X8}: {Read(y):B32} \t ");
            else
                Console.WriteLine($"{y:X8}: {Read(y):B32}");
            printLeft = !printLeft;
        }
    }

    // Checks if address is below TotalSizeBits. If not, an exception is thrown.
    private void CheckValidAddress(uint addr)
    {
        if (addr > _words)
            throw new ArgumentException($"Error reading memory: Address 0x{addr:X} above 0x{_words:X}.");
    }
}