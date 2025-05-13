namespace TinyProc.Memory;

using TinyProc.Processor;

public class RawMemory : IBusAttachable
{
    public readonly uint _words;
    public ulong TotalSizeBits { get => (ulong)_words * (ulong)Register.SYSTEM_WORD_SIZE; }
    // The data array can hold a maximum of ~4 billion elements, however an array can
    // only hold up to ~2 billion elements in C#.
    // This necessitates the usage of an array of uint arrays, that can hold up the required amount
    // of uints in total.
    // Externally, this appears as one continuous address space.
    private readonly uint[][] _data;

    // Logic that allows only either write or read line to be set:
    private bool _readEnable;
    public bool ReadEnable
    {
        get => _readEnable;
        set
        {
            _readEnable = value;
            _writeEnable = !value && _writeEnable;
            // Read request via bus
            if (value)
                MemoryDataBus.Data = Bus.UIntToBoolArray(Read(Bus.BoolArrayToUInt(MemoryAddressBus.Data, 0)));
        }
    }
    private bool _writeEnable;
    public bool WriteEnable
    {
        get => _writeEnable;
        set
        {
            _writeEnable = value;
            _readEnable = !value && _readEnable;
            // Write request via bus
            if (value)
                Write(Bus.BoolArrayToUInt(MemoryAddressBus.Data, 0), Bus.BoolArrayToUInt(MemoryDataBus.Data, 0));
        }
    }

    // Initialized as soon as attached to bus
    private Bus MemoryAddressBus;
    private Bus MemoryDataBus;

    public RawMemory(uint words, uint[] initialData)
    {
        if (words <= 0)
            throw new ArgumentException("Word count 0 disallowed");
        if (words > 1_000_000)
            Console.Error.WriteLine(
                "Warning: *Attempting* to initialize very large memory (>1,000,000 words). Expect out-of-memory errors.");
        _words = words;
        if (_words > int.MaxValue)
            _data = [new uint[int.MaxValue], new uint[_words - int.MaxValue]];
        else
            _data = [new uint[_words]];

        if (initialData.Length > words)
            throw new ArgumentException("Cannot initialize memory: Initial data is larger than memory size.");
        for (uint i = 0; i < initialData.Length; i++)
            Write(i, initialData[i]);
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
            Console.Write($"{baseAddr:x8}:");
            // Print address values as hexadecimal
            for (uint subAddr = 0; subAddr < addressesPerLine; subAddr++)
            {
                uint addr = baseAddr + subAddr;
                Console.Write($" {Read(addr):x8}");
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

    // Checks if address is below the amount of words. If not, an exception is thrown.
    private void CheckValidAddress(uint addr)
    {
        if (addr > _words - 1)
            throw new ArgumentOutOfRangeException($"Error reading memory: Address 0x{addr:x8} above 0x{(_words - 1):x8}.");
    }

    // Implicitly treats first call as address bus and second call as data bus,
    // ignoring all subsequent calls.
    private int busAttachCount = 0;
    public void AttachToBus(uint ubid, Bus bus)
    {
        if (busAttachCount == 0)
            MemoryAddressBus = bus;
        else if (busAttachCount == 1)
            MemoryDataBus = bus;
        else
            throw new ArgumentException("Attempted memory bus attachment beyond address and data bus. Aborting.");
        busAttachCount++;
        
        if (MemoryAddressBus != null && MemoryDataBus != null)
            Console.WriteLine("Memory successfully attached to address and data bus.");
    }
}