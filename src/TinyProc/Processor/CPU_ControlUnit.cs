using System.Diagnostics;

namespace TinyProc.Processor;

public partial class CPU
{
    // The control unit directs control of individual CPU elements depending on the instruction bits.
    // It essentially decodes a received instruction and controls the components (e.g. ALU and registers)
    // its connected to.
    private partial class ControlUnit : IBusAttachable
    {
        public static readonly uint PC_PROGRAM_START = 0x0u;
        // Program counter
        private readonly Register PC = new(true, RegisterRWAccess.ReadOnly);
        // Instruction register 1
        private readonly Register IRA = new(true, RegisterRWAccess.ReadOnly);
        // Instruction register 2
        // Two 32 bit instruction registers necessary, since an entire instruction
        // is double-word-aligned, meaning it occupies 64 bits.
        private readonly Register IRB = new(true, RegisterRWAccess.ReadOnly);

        // The parent CPU object (needed to reference e.g. registers)
        private readonly CPU _cpu;

        private ControlUnit(CPU cpu, MMU mmu, ALU alu)
        {
            _cpu = cpu;
            _IntBus1 = new Bus(32, [this, mmu.MAR, alu]);
            _IntBus2 = new Bus(32, [this, alu]);
            _IntBus3 = new Bus(32, [this, alu]);
            PC.Value = PC_PROGRAM_START;
        }

        public enum ControlState
        {
            Fetch1,
            Fetch2,
            Decode,
            Execute
        }
        private ControlState _currentControlState;
        private ControlState CurrentControlState
        {
            get => _currentControlState;
            set
            {
                _currentControlState = value;
                switch (value)
                {
                    case ControlState.Fetch1:  InstructionFetch1();  break;
                    case ControlState.Fetch2:  InstructionFetch2();  break;
                    case ControlState.Decode:  InstructionDecode();  break;
                    case ControlState.Execute: InstructionExecute(); break;
                }
            }
        }
    }
}