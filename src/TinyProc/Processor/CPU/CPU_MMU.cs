using TinyProc.Memory;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    // Memory management unit
    // Much simpler than in modern architectures (like x86_64), but essentially controls
    // memory flow in / out of the CPU
    private class MMU
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
                    uint? unassigned = _mmu?.MDR?.ValueDirect;
                }
            }
        }
        public class MemoryDataRegister(MMU mmu) : Register(0, true)
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
        public MMU(ROM rom, (uint, uint) romSpace, Dictionary<(uint, uint), RawMemory> rams)
        {
            _MemorySpaces = [];
            // TODO: Implement ROMs
            Console.Error.WriteLine(
                "Warning: ROM not implemented yet in MMU. Access to its space will result in a NullReferenceException.");
            //_MemorySpaces.Add(null, romSpace);
            foreach (((uint, uint) ramSpace, RawMemory ram) in rams)
            {
                _MemorySpaces.Add(ram, ramSpace);
            }
            MAR = new MemoryAddressRegister(this);
            MDR = new MemoryDataRegister(this);
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
                $"Cannot determine memory from absolute address {addr:X8}. Max. address is {_MemorySpaces.Values.Last().Item2:X8}");
        }

        private RawMemory RAM
        {
            get => GetRAMAtVirtualAddress(MAR.ValueDirect);
        }

        private uint GetRelativeAddress(uint absoluteAddr, RawMemory ram)
        {
            return absoluteAddr - _MemorySpaces[ram].Item1;
        }

        private readonly Dictionary<RawMemory, (uint, uint)> _MemorySpaces;
        // Memory address register: Sets an address to read from / write to in memory logic.
        public readonly MemoryAddressRegister MAR;
        // Memory data register:
        // When reading, contains the value at address set in MAR.
        // When writing, contains the value to be written to address in MAR.
        public readonly MemoryDataRegister MDR;
    }
}