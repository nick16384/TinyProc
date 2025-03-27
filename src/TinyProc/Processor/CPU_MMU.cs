using System.Data.SqlTypes;
using TinyProc.Memory;

namespace TinyProc.Processor;

public partial class CPU
{
    // Memory management unit
    // Much simpler than in modern architectures (like x86_64), but essentially controls
    // memory flow in / out of the CPU
    public class MMU
    {
        public class MemoryDataRegister(MMU mmu) : Register(true, RegisterRWAccess.ReadOnly)
        {
            private readonly MMU _mmu = mmu;
            public override uint Value
            {
                get
                {
                    _mmu._RAM.ReadEnable = true;
                    _mmu._RAM.AddressBus = _mmu.MAR.Value;
                    return _mmu._RAM.DataBus;
                }
                set
                {
                    _mmu._RAM.WriteEnable = true;
                    _mmu._RAM.DataBus = value;
                }
            }
        }

        public MMU(RawMemory ram)
        {
            _RAM = ram;
            MAR = new(true, RegisterRWAccess.ReadOnly);
            MDR = new MemoryDataRegister(this);
        }

        public readonly RawMemory _RAM;
        // Memory address register: Sets an address to read from / write to in memory logic.
        public readonly Register MAR;
        // Memory data register:
        // When reading, contains the value at address set in MAR.
        // When writing, contains the value to be written to address in MAR.
        public readonly Register MDR;
    }
}