using System.Runtime.CompilerServices;
using TinyProc.Application;
using TinyProc.Processor;

namespace TinyProc.Memory;

public class ROM : IMemoryDevice
{
    public readonly uint _size;
    public ulong TotalSizeBits { get => (ulong)_size * (ulong)Register.SYSTEM_WORD_SIZE; }
    private readonly uint[] _fixedData;
    public uint[] FixedData { get => _fixedData; }
    public uint Size { get => _size; }

    private bool _readEnable;
    public bool ReadEnable
    {
        get => _readEnable;
        set
        {
            _readEnable = value;
            // Read request via bus
            if (value)
                MemoryDataBus!.Data = Bus.UIntToBoolArray(Read(Bus.BoolArrayToUInt(MemoryAddressBus!.Data, 0)));
        }
    }
    /// <summary>
    /// Read directly from ROM without a bus attached;
    /// It is strongly discouraged to use this method unless some external element (e.g. a GUI) needs direct access.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadDirect(uint address) => Read(address);

    // Initialized as soon as attached to bus
    private Bus? MemoryAddressBus;
    private Bus? MemoryDataBus;

    public ROM(uint size, uint[] fixedData)
    {
        if (size > 1_000_000)
            Logging.LogWarn(
                "Warning: *Attempting* to initialize very large ROM (>1,000,000 words). Expect out-of-memory errors.");
        _size = size;
        _fixedData = fixedData;

        Logging.LogDebug(
            $"Init ROM done; WORD SIZE:{Register.SYSTEM_WORD_SIZE}, " +
            $"WORDS:{_size}; Total occupation:{TotalSizeBits} bits; Usage:{100.0 * _fixedData.Length / _size:N2}%");
    }

    // Reads block of 32 bits
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private protected virtual uint Read(uint addr)
    {
        CheckValidAddress(addr);
        if (addr > _fixedData.Length - 1)
            return 0;
        return _fixedData[addr];
    }

    public void Debug_DumpAll()
    {
        Logging.LogDebug("[ROM] Dumping full ROM contents (Big endian)");
        int addressesPerLine = 4;
        for (uint baseAddr = 0; baseAddr < _size; baseAddr += 4)
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
        if (addr > _size - 1)
            throw new ArgumentOutOfRangeException($"Error reading ROM: Address 0x{addr:x8} above 0x{(_size - 1):x8}.");
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
            Logging.LogDebug("ROM successfully attached to address and data bus.");
    }

    uint IMemoryDevice.ReadDirect(uint addr)
        => Read(addr);
}