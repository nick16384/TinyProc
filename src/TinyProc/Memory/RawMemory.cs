namespace TinyProc.Memory;

using System.Text;
using TinyProc.Processor;

public class RawMemory
{
    public readonly uint _words;
    public uint TotalSizeBits { get { return Register.SYSTEM_WORD_SIZE * _words; } }
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
    private uint Read(uint addr)
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
    private void Write(uint addr, uint value)
    {
        if (addr == 0x00000039u)
            Console.WriteLine("Miku says thank you!");
        CheckValidAddress(addr);
        Console.WriteLine($"Write 0x{value:X} at 0x{addr:X}");
        for (int x = 0; x < Register.SYSTEM_WORD_SIZE; x++)
        {
            uint valueMasked = value & (MASK_LEFT_BIT_SINGLE >> x);
            valueMasked >>= (int)(Register.SYSTEM_WORD_SIZE - 1 - x);
            bool isBitSet = valueMasked > 0;
            _data[x, addr] = isBitSet;
        }
    }

    // TODO: Refactor this method to look more pleasing.
    public void Debug_DumpAll()
    {
        Console.WriteLine("Dumping full memory contents");
        // Print 4 addresses per line
        bool printingAscii = false;
        int printColumn = 1;
        for (uint y = 0; y < _words; y++)
        {
            if (printColumn == 1 && !printingAscii)
                Console.Write($"{y:X8}:");
            if (printColumn < 4)
            {
                if (!printingAscii)
                    Console.Write($" {Read(y):X8}");
                else
                {
                    string str = Encoding.UTF8.GetString([(byte)Read(y)]);
                    if (string.IsNullOrWhiteSpace(str) || str == ((char)0).ToString())
                        Console.Write(".");
                    else
                        Console.Write(str);
                }
                printColumn++;
            }
            else
            {
                if (!printingAscii)
                    Console.Write($" {Read(y):X8} \t ");
                else
                {
                    string str = Encoding.UTF8.GetString([(byte)Read(y)]);
                    if (string.IsNullOrWhiteSpace(str) || str == ((char)0).ToString())
                        Console.WriteLine(".");
                    else
                        Console.WriteLine(str);
                }
                printColumn = 1;
                if (!printingAscii)
                {
                    printingAscii = true;
                    y -= 4;
                }
                else
                    printingAscii = false;
            }
        }
    }

    // Checks if address is below TotalSizeBits. If not, an exception is thrown.
    private void CheckValidAddress(uint addr)
    {
        if (addr > _words - 1)
            throw new ArgumentException($"Error reading memory: Address 0x{addr:X} above 0x{(_words - 1):X}.");
    }
}