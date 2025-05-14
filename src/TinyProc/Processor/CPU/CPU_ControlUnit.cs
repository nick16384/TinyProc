namespace TinyProc.Processor.CPU;

public partial class CPU
{
    // The control unit directs control of individual CPU elements depending on the instruction bits.
    // It essentially decodes a received instruction and controls the components (e.g. ALU and registers)
    // its connected to, to execute the instruction.
    private partial class ControlUnit
    {
        // Program counter
        private readonly Register PC = new(true, RegisterRWAccess.ReadOnly);
        // Instruction register 1
        private readonly Register IRA = new(true, RegisterRWAccess.ReadOnly);
        // Instruction register 2
        // Two 32 bit instruction registers necessary, since an entire instruction
        // is double-word-aligned, meaning it occupies 64 bits.
        private readonly Register IRB = new(true, RegisterRWAccess.ReadOnly);
        
        private readonly Dictionary<uint, Register> CU_ADDRESSABLE_REGISTERS;

        // The parent CPU object (needed to reference e.g. registers)
        private readonly CPU _cpu;
        private readonly ALU _alu;
        private readonly MMU _mmu;

        internal ControlUnit(CPU cpu, uint entryPoint, ALU alu, MMU mmu)
        {
            PC.ValueDirect = entryPoint;
            _cpu = cpu;
            _alu = alu;
            _mmu = mmu;

            CU_ADDRESSABLE_REGISTERS = new Dictionary<uint, Register>
            {
                {RCODE_PC, PC},
                {RCODE_GP1, _cpu.GP1},
                {RCODE_GP2, _cpu.GP2},
                {RCODE_GP3, _cpu.GP3},
                {RCODE_GP4, _cpu.GP4},
                {RCODE_GP5, _cpu.GP5},
                {RCODE_GP6, _cpu.GP6},
                {RCODE_GP7, _cpu.GP7},
                {RCODE_GP8, _cpu.GP8},
                {RCODE_SR, _alu.SR},
                {RCODE_SPECIAL_MAR, _mmu.MAR},
                {RCODE_SPECIAL_MDR, _mmu.MDR},
                {RCODE_SPECIAL_IRA, IRA},
                {RCODE_SPECIAL_IRB, IRB},
                {RCODE_SPECIAL_CV_0, CV_0_SPECIAL_REG},
                {RCODE_SPECIAL_CV_P1, CV_P1_SPECIAL_REG},
                {RCODE_SPECIAL_CV_N1, CV_N1_SPECIAL_REG},
                {RCODE_SPECIAL_CV_P2, CV_P2_SPECIAL_REG},
            };

            // Internal bus 1 - 3 initialization
            B1_REGISTERS = new Dictionary<uint, Register>
            {
                {RCODE_PC, PC},
                {RCODE_GP1, _cpu.GP1},
                {RCODE_GP2, _cpu.GP2},
                {RCODE_GP3, _cpu.GP3},
                {RCODE_GP4, _cpu.GP4},
                {RCODE_GP5, _cpu.GP5},
                {RCODE_GP6, _cpu.GP6},
                {RCODE_GP7, _cpu.GP7},
                {RCODE_GP8, _cpu.GP8},
                //{RCODE_SR, _alu.SR},
                {RCODE_SPECIAL_MAR, _mmu.MAR},
                {RCODE_SPECIAL_IRA, IRA},
                {RCODE_SPECIAL_IRB, IRB},
                {RCODE_SPECIAL_CV_0, CV_0_SPECIAL_REG}
            };
            B2_REGISTERS = new Dictionary<uint, Register>
            {
                {RCODE_GP1, _cpu.GP1},
                {RCODE_GP2, _cpu.GP2},
                {RCODE_GP3, _cpu.GP3},
                {RCODE_GP4, _cpu.GP4},
                {RCODE_GP5, _cpu.GP5},
                {RCODE_GP6, _cpu.GP6},
                {RCODE_GP7, _cpu.GP7},
                {RCODE_GP8, _cpu.GP8},
                {RCODE_SPECIAL_MDR, _mmu.MDR},
                {RCODE_SPECIAL_CV_P1, CV_P1_SPECIAL_REG},
                {RCODE_SPECIAL_CV_N1, CV_N1_SPECIAL_REG},
                {RCODE_SPECIAL_CV_P2, CV_P2_SPECIAL_REG},
                {RCODE_SPECIAL_CV_0, CV_0_SPECIAL_REG}
            };
            B3_REGISTERS = new Dictionary<uint, Register>
            {
                {RCODE_PC, PC},
                {RCODE_GP1, _cpu.GP1},
                {RCODE_GP2, _cpu.GP2},
                {RCODE_GP3, _cpu.GP3},
                {RCODE_GP4, _cpu.GP4},
                {RCODE_GP5, _cpu.GP5},
                {RCODE_GP6, _cpu.GP6},
                {RCODE_GP7, _cpu.GP7},
                {RCODE_GP8, _cpu.GP8},
                {RCODE_SPECIAL_MAR, _mmu.MAR},
                {RCODE_SPECIAL_MDR, _mmu.MDR},
                {RCODE_SPECIAL_IRA, IRA},
                {RCODE_SPECIAL_IRB, IRB},
                {RCODE_SPECIAL_CV_0, CV_0_SPECIAL_REG}
            };

            _IntBus1Src = new MultiSrcSingleDstRegisterSelector(
                Register.SYSTEM_WORD_SIZE, INTBUS_B1_UBID, B1_REGISTERS, RCODE_GP1, alu.A);
            _IntBus2Src = new MultiSrcSingleDstRegisterSelector(
                Register.SYSTEM_WORD_SIZE, INTBUS_B2_UBID, B2_REGISTERS, RCODE_GP1, alu.B);
            _IntBus3Dst = new SingleSrcMultiDstRegisterSelector(
                Register.SYSTEM_WORD_SIZE, INTBUS_B3_UBID, alu.R, B3_REGISTERS, RCODE_GP1);
        }

        private enum ControlState
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
                throw new NotImplementedException("Control state cannot be set: Not yet implemented");
            }
        }

        // TODO: Add some kind of control bus, which is selected by the CPU
        // depending on the clock cycle and determines the current phase.
        public void Temp_InstructionFetch1() { InstructionFetch1(); }
        public void Temp_InstructionFetch2() { InstructionFetch2(); }
        public void Temp_InstructionDecode() { InstructionDecode(); }
        public void Temp_InstructionExecute() { InstructionExecute(); }
    }
}