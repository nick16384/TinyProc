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
                    _mmu._RAM.ReadEnable = true;
                    _mmu._RAM.AddressBus = _mmu.MAR.ValueDirect;
                    _storedValue = _mmu._RAM.DataBus;
                    return _storedValue;
                }
                set
                {
                    _mmu._RAM.WriteEnable = true;
                    _mmu._RAM.DataBus = value;
                    _storedValue = value;
                }
            }
        }

        public MMU(RawMemory ram)
        {
            _RAM = ram;
            MAR = new MemoryAddressRegister(this);
            MDR = new MemoryDataRegister(this);
        }

        public readonly RawMemory _RAM;
        // Memory address register: Sets an address to read from / write to in memory logic.
        public readonly MemoryAddressRegister MAR;
        // Memory data register:
        // When reading, contains the value at address set in MAR.
        // When writing, contains the value to be written to address in MAR.
        public readonly MemoryDataRegister MDR;
    }
}