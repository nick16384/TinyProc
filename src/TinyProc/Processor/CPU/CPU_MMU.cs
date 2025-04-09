using TinyProc.Memory;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    // Memory management unit
    // Much simpler than in modern architectures (like x86_64), but essentially controls
    // memory flow in / out of the CPU
    private class MMU
    {
        public class MemoryAddressRegister(MMU mmu) : Register(true, RegisterRWAccess.ReadWrite)
        {
            private readonly MMU _mmu = mmu;
            private protected override uint Value
            {
                get => _storedValue;
                set
                {
                    _storedValue = value;
                    // Update MDR --> Trigger bus update
                    uint? unassigned = _mmu?.MDR?.ValueDirect;
                }
            }
        }
        public class MemoryDataRegister(MMU mmu) : Register(true, RegisterRWAccess.ReadOnly)
        {
            private readonly MMU _mmu = mmu;
            private protected override uint Value
            {
                get
                {
                    _mmu.RAM.ReadEnable = true;
                    _mmu.RAM.AddressBus = _mmu.GetRelativeAddress(_mmu.MAR.ValueDirect, _mmu.RAM);
                    _storedValue = _mmu.RAM.DataBus;
                    return _storedValue;
                }
                set
                {
                    _mmu.RAM.WriteEnable = true;
                    _mmu.RAM.DataBus = value;
                    _storedValue = value;
                }
            }
        }

        // Facilitates and encapsulates mechanisms to read from and write to arbitrary memory.
        // Combines attached memory objects' address spaces into one continuous address space.
        public MMU(ROM rom, RawMemory[] rams)
        {
            _RAMs = rams;
            _MemorySpaces = [];
            uint addressCurrent = 0x0u;
            foreach (RawMemory ram in _RAMs)
            {
                _MemorySpaces.Add(ram, (addressCurrent, addressCurrent + ram._words));
                addressCurrent += ram._words;
            }
            MAR = new MemoryAddressRegister(this);
            MDR = new MemoryDataRegister(this);
        }

        private RawMemory GetRAMAtVirtualAddress(uint addr)
        {
            foreach (RawMemory ram in _RAMs)
            {
                uint minAddr = _MemorySpaces[ram].Item1;
                uint maxAddr = _MemorySpaces[ram].Item2;
                if (addr >= minAddr && addr < maxAddr)
                    return ram;
            }
            throw new ArgumentOutOfRangeException(
                $"Cannot determine memory from absolute address {addr:X8}. Max. address is {_MemorySpaces[_RAMs.Last()].Item2:X8}");
        }

        private RawMemory RAM
        {
            get => GetRAMAtVirtualAddress(MAR.ValueDirect);
        }

        private uint GetRelativeAddress(uint absoluteAddr, RawMemory ram)
        {
            return absoluteAddr - _MemorySpaces[ram].Item1;
        }

        public readonly RawMemory[] _RAMs;
        private readonly Dictionary<RawMemory, (uint, uint)> _MemorySpaces;
        // Memory address register: Sets an address to read from / write to in memory logic.
        public readonly MemoryAddressRegister MAR;
        // Memory data register:
        // When reading, contains the value at address set in MAR.
        // When writing, contains the value to be written to address in MAR.
        public readonly MemoryDataRegister MDR;
    }
}