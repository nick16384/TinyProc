namespace TinyProc.Memory;

using System.Runtime.CompilerServices;
using TinyProc.Application;
using TinyProc.Processor;

public class RawMemory : IReadWriteMemoryDevice
{
    public const uint FULL_SIZE = 0xFFFFFFFFu;
    // The memory is internally managed via a paged address space. This is not exposed to
    // the outside, but necessary to allocate pages dynamically, since allocating a 32-bit flat array
    // would consume around 4GiB of host memory and would therefore be unfeasable.
    private const int PAGE_SIZE_BITS = 12;
    private const uint PAGE_SIZE = 1u << PAGE_SIZE_BITS; // 4096
    private const uint PAGE_OFFSET_MASK = PAGE_SIZE - 1u; // 
    private const uint PAGE_NUMBER_MASK = ~PAGE_OFFSET_MASK;

    public readonly uint _numWords;
    public ulong TotalSizeBits { get => (ulong)_numWords * (ulong)Register.SYSTEM_WORD_SIZE; }
    private readonly uint _numPages;
    private readonly uint[]?[] _data;

    /// <summary>
    /// Write directly to the memory without a bus attached;
    /// It is strongly discouraged to use this method unless some external element (e.g. a GUI) needs direct write access.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDirect(uint address, uint value) => Write(address, value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadDirect(uint address) => Read(address);

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
                MemoryDataBus!.Data = Bus.UIntToBoolArray(Read(Bus.BoolArrayToUInt(MemoryAddressBus!.Data, 0)));
        }
    }
    private bool _writeEnable;
    // It is important to note, that a write operation to the bus only effectively stores its data,
    // if the WriteEnable (WE) line is set after the data has been written to the bus.
    public bool WriteEnable
    {
        get => _writeEnable;
        set
        {
            _writeEnable = value;
            _readEnable = !value && _readEnable;
            // Write request via bus
            if (value)
                Write(Bus.BoolArrayToUInt(MemoryAddressBus!.Data, 0), Bus.BoolArrayToUInt(MemoryDataBus!.Data, 0));
        }
    }

    // Initialized as soon as attached to bus
    private Bus? MemoryAddressBus;
    private Bus? MemoryDataBus;

    public RawMemory(uint numWords = FULL_SIZE) : this(numWords, []) {}

    /// <summary>
    /// Creates a virtual memory device.
    /// </summary>
    /// <param name="numWords">The number of words that should be addressable via the bus.</param>
    /// <param name="initialData">Data inside the memory device starting from the lowest address.</param>
    /// <exception cref="ArgumentException"></exception>
    public RawMemory(uint numWords, IEnumerable<uint> initialData)
    {
        if (numWords <= 0)
            throw new ArgumentException("Word count 0 disallowed");
        _numWords = numWords;
        Logging.LogWarn("Warning: Word count is currently ignored for memory creation. Assuming FULL_SIZE.");

        // Precondition checks
        if (initialData.Count() > _numWords)
            throw new ArgumentException("Cannot initialize memory: Initial data is larger than memory size.");
        
        // Always add another page to ensure enough memory is addressable
        _numPages = (numWords / PAGE_SIZE) + 1;
        Logging.LogDebug($"Memory: {_numPages} pages ({PAGE_SIZE} words / page)");

        _data = new uint[]?[_numPages];
        for (uint addr = 0u; addr < initialData.Count(); addr++)
            Write(addr, initialData.ElementAt((int)addr));

        Logging.LogDebug(
            $"Init memory done; WORD SIZE:{Register.SYSTEM_WORD_SIZE}, " +
            $"WORDS:{_numWords}; Total space:{TotalSizeBits} bits");
    }

    // Reads block of 32 bits
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private protected virtual uint Read(uint addr)
    {
        CheckValidAddress(addr);
        uint pageNumber = (addr & PAGE_NUMBER_MASK) >> PAGE_SIZE_BITS;
        uint pageOffset = addr & PAGE_OFFSET_MASK;
        if (_data[pageNumber] == null)
            return 0u;
        else
            return _data[pageNumber]![pageOffset];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private protected virtual void Write(uint addr, uint value)
    {
        CheckValidAddress(addr);
        uint pageNumber = (addr & PAGE_NUMBER_MASK) >> PAGE_SIZE_BITS;
        uint pageOffset = addr & PAGE_OFFSET_MASK;
        if (_data[pageNumber] == null)
            _data[pageNumber] = new uint[PAGE_SIZE];
        
        _data[pageNumber]![pageOffset] = value;
    }

    /// <summary>
    /// Returns the memory content as byte array.
    /// FIXME: How to avoid running into the 32-bit array addressing limit in C#?
    /// </summary>
    public Span<byte> ReadAllAsBytes()
    {
        throw new NotImplementedException();
    }

    public void Debug_DumpAll()
    {
        Logging.LogDebug("[Mem] Dumping full memory contents (Big endian)");
        int addressesPerLine = 4;
        for (uint baseAddr = 0; baseAddr < _numWords; baseAddr += 4)
        {
            Logging.PrintDebug($"{baseAddr:x8}:");
            // Print address values as hexadecimal
            for (uint subAddr = 0; subAddr < addressesPerLine; subAddr++)
            {
                uint addr = baseAddr + subAddr;
                Logging.PrintDebug($" {Read(addr):x8}");
            }
            Logging.PrintDebug("   ");
            // Print address values decoded as ASCII
            for (uint subAddr = 0; subAddr < addressesPerLine; subAddr++)
            {
                uint addr = baseAddr + subAddr;
                uint data = Read(addr);
                char c1 = (char)((data & 0xFF000000) >> 24);
                char c2 = (char)((data & 0x00FF0000) >> 16);
                char c3 = (char)((data & 0x0000FF00) >> 8);
                char c4 = (char)((data & 0x000000FF) >> 0);
                if (c1 >= 0x20 && c1 <= 0x7E) { Logging.PrintDebug($" {c1} "); } else { Logging.PrintDebug(" . "); }
                if (c2 >= 0x20 && c2 <= 0x7E) { Logging.PrintDebug($" {c2} "); } else { Logging.PrintDebug(" . "); }
                if (c3 >= 0x20 && c3 <= 0x7E) { Logging.PrintDebug($" {c3} "); } else { Logging.PrintDebug(" . "); }
                if (c4 >= 0x20 && c4 <= 0x7E) { Logging.PrintDebug($" {c4} "); } else { Logging.PrintDebug(" . "); }
            }
            Logging.NewlineDebug();
        }
    }

    // Checks if address is below the amount of words. If not, an exception is thrown.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckValidAddress(uint addr)
    {
        if (addr > _numWords - 1)
            throw new ArgumentOutOfRangeException($"Error reading memory: Address 0x{addr:x8} above 0x{(_numWords - 1):x8}.");
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
            Logging.LogDebug("Memory successfully attached to address and data bus.");
    }
}