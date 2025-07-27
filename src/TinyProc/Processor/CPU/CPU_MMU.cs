using TinyProc.Application;
using TinyProc.Memory;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    // Memory management unit
    // Much simpler than in modern architectures (like x86_64), but essentially controls
    // memory flow in / out of the CPU
    private class MMU : IBusAttachable
    {
        public class MemoryAddressRegister(MMU mmu) : Register(0, true)
        {
            private readonly MMU _mmu = mmu;
            private protected override uint Value
            {
                get => _storedValue;
                set
                {
                    _storedValue = value;
                    // Update MDR --> Trigger bus update
                    //uint? unassigned = _mmu?.MDR?.ValueDirect;
                }
            }
        }
        public class MemoryDataRegister(MMU mmu) : Register(0, true)
        {
            private object readWriteLock = new();
            private readonly MMU _mmu = mmu;
            private protected override uint Value
            {
                get
                {
                    // Synchronization is required, because read / write operations are not atomic, meaning
                    // that any external thread could input wrong address data while the CPU might still want
                    // to read from another address it wrote to the MAR earlier.
                    lock (readWriteLock)
                    {
                        _mmu.MemoryAddressBus.Data = Bus.UIntToBoolArray(_mmu.GetRelativeAddress(_mmu.MAR.ValueDirect, _mmu.RAM));
                        _mmu.RAM.ReadEnable = true;
                        _storedValue = Bus.BoolArrayToUInt(_mmu.MemoryDataBus.Data, 0);
                        _mmu.RAM.ReadEnable = false;
                        return _storedValue;
                    }
                }
                set
                {
                    lock (readWriteLock)
                    {
                        _mmu.MemoryAddressBus.Data = Bus.UIntToBoolArray(_mmu.GetRelativeAddress(_mmu.MAR.ValueDirect, _mmu.RAM));
                        _mmu.MemoryDataBus.Data = Bus.UIntToBoolArray(value);
                        _mmu.RAM.WriteEnable = true;
                        _mmu.RAM.WriteEnable = false;
                        _storedValue = value;
                    }
                }
            }
        }

        private readonly Dictionary<RawMemory, (uint, uint)> _MemorySpaces;
        // Memory address register: Sets an address to read from / write to in memory logic.
        public readonly MemoryAddressRegister MAR;
        // Memory data register:
        // When reading, contains the value at address set in MAR.
        // When writing, contains the value to be written to address in MAR.
        public readonly MemoryDataRegister MDR;

        // Base bus UBIDs, they increase for every memory object created.
        public const uint UBID_BASE_MEMADDRESSBUS = 0x11;
        public const uint UBID_BASE_MEMDATABUS = 0x12;
        private readonly Dictionary<RawMemory, Bus> MemoryAddressBusses = [];
        private Bus MemoryAddressBus { get => MemoryAddressBusses[RAM]; }
        private readonly Dictionary<RawMemory, Bus> MemoryDataBusses = [];
        private Bus MemoryDataBus { get => MemoryDataBusses[RAM]; }

        // Facilitates and encapsulates mechanisms to read from and write to arbitrary memory.
        // Combines attached memory objects' address spaces into one continuous address space.
        public MMU(ROM rom, Dictionary<uint, RawMemory> rams)
        {
            _MemorySpaces = [];
            // TODO: Implement ROMs
            Logging.LogWarn(
                "Warning: ROM not implemented yet in MMU. Access to its space will result in a NullReferenceException.");
            //_MemorySpaces.Add(null, romSpace);
            foreach ((uint ramStart, RawMemory ram) in rams)
            {
                if (rom.Size - 1 > ramStart)
                    throw new Exception($"RAM starts before ROM ends!");
                _MemorySpaces.Add(ram, (ramStart, ram._words - 1));
            }
            MAR = new MemoryAddressRegister(this);
            MDR = new MemoryDataRegister(this);

            int idx = 0;
            foreach ((uint ramStart, RawMemory ram) in rams)
            {
                MemoryAddressBusses.Add(
                    ram,
                    new Bus(Register.SYSTEM_WORD_SIZE, UBID_BASE_MEMADDRESSBUS + (uint)(2 * idx), [this, ram]));
                MemoryDataBusses.Add(
                    ram,
                    new Bus(Register.SYSTEM_WORD_SIZE, UBID_BASE_MEMDATABUS + (uint)(2 * idx), [this, ram]));
                idx++;
            }
        }

        private RawMemory GetRAMAtVirtualAddress(uint addr)
        {
            foreach ((RawMemory ram, (uint, uint) ramSpace) in _MemorySpaces)
            {
                // Not the correct RAM, if the address is above the the max. address of the current RAM
                if (addr >= ramSpace.Item2)
                    continue;
                return ram;
            }
            // If the foreach loop runs through before the return statement was called, then the address is out of range.
            throw new ArgumentOutOfRangeException(
                $"Cannot determine memory from absolute address {addr:x8}. Max. address is {_MemorySpaces.Values.Last().Item2:x8}");
        }

        private RawMemory RAM
        {
            get => GetRAMAtVirtualAddress(MAR.ValueDirect);
        }

        private uint GetRelativeAddress(uint absoluteAddr, RawMemory ram)
        {
            return absoluteAddr - _MemorySpaces[ram].Item1;
        }

        public void AttachToBus(uint ubid, Bus bus) { /* Do nothing. MMU already is the bus master. */ }
    }
}