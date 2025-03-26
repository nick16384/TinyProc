namespace TinyProc.Processor;

public partial class CPU
{
    // The control unit directs control of individual CPU elements depending on the instruction bits.
    // It essentially decodes a received instruction and controls the components (e.g. ALU and registers)
    // its connected to.
    private partial class ControlUnit
    {
        public static readonly uint PC_PROGRAM_START = 0x39u;
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

        public readonly ControlBus _ControlBus;

        public ControlUnit(CPU cpu)
        {
            _cpu = cpu;
            _ControlBus = new(this, cpu._MMU);
            PC.Value = PC_PROGRAM_START;
        }

        public enum ControlState
        {
            Fetch1,
            Fetch2,
            Decode,
            Execute
        }
        private readonly ControlState _currentControlState;
        private ControlState CurrentControlState
        {
            get => _currentControlState;
            set
            {
                switch (value)
                {
                    case ControlState.Fetch1:  InstructionFetch1();  break;
                    case ControlState.Fetch2:  InstructionFetch2();  break;
                    case ControlState.Decode:  InstructionDecode();  break;
                    case ControlState.Execute: InstructionExecute(); break;
                }
            }
        }

        // Load first instruction word
        private void InstructionFetch1()
        {
            _ControlBus.MAR = PC.Value;
            IRA.Value = _ControlBus.MDR;
        }
        // Load second instruction word
        private void InstructionFetch2()
        {
            _ControlBus.MAR = PC.Value + 0x1u;
            IRB.Value = _ControlBus.MDR;
        }
        private void InstructionDecode()
        {
            PC.Value += 0x2u;
            if (IRA.Value == 0x0u)
                Console.WriteLine("[InstrDec] Temporary NOP");
            // something
            // put stuff in acc
            // etc.
            // essentially prepare for exec
        }
        private void InstructionExecute()
        {
            if (IRA.Value == 0x0u)
                Console.WriteLine("[InstrExec] Temporary NOP");
            // big switch case / if else
        }
    }
}