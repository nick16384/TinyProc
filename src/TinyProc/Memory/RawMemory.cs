using System.Net.Http.Headers;

namespace TinyProc.Memory;

public class RawMemory
{
    public readonly ulong _words;
    public ulong TotalSizeBits { get { return Register.SYSTEM_WORD_SIZE * _words; } }
    private readonly bool[,] _data;

    public RawMemory(ulong words)
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

    private static readonly ulong MASK_LEFT_BIT_SINGLE = 0x8000_0000_0000_0000u;
    // Reads block of 64 bits
    public ulong Read(ulong addr)
    {
        CheckValidAddress(addr);
        ulong readWord = 0x0;
        for (uint x = 0; x < Register.SYSTEM_WORD_SIZE; x++)
        {
            bool dataBit = _data[x, addr];
            ulong dataBitAsULong = dataBit ? MASK_LEFT_BIT_SINGLE : 0x0u;
            readWord |= dataBitAsULong >> (int)x;
        }
        return readWord;
    }
    public void Write(ulong addr, ulong value)
    {
        CheckValidAddress(addr);
        Console.WriteLine($"Write 0x{value:X} at 0x{addr:X}");
        for (uint x = 0; x < Register.SYSTEM_WORD_SIZE; x++)
        {
            ulong valueMasked = value & (MASK_LEFT_BIT_SINGLE >> (int)x);
            valueMasked >>= (int)(Register.SYSTEM_WORD_SIZE - 1 - x);
            bool isBitSet = valueMasked > 0;
            _data[x, addr] = isBitSet;
        }
    }

    public void PrintAll()
    {
        Console.WriteLine("Printing full memory contents");
        for (ulong y = 0; y < _words; y++)
        {
            Console.WriteLine($"{y:D8}: {Read(y):B64} ");
        }
    }

    // Checks if address is below TotalSizeBits. If not, an exception is thrown.
    private void CheckValidAddress(ulong addr)
    {
        if (addr > _words)
            throw new ArgumentException($"Error reading memory: Address 0x{addr:X} above 0x{_words:X}.");
    }
}