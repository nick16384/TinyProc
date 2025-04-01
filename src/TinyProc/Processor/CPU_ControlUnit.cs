using System.Runtime.CompilerServices;

namespace TinyProc.Processor;

public partial class CPU
{
    // The control unit directs control of individual CPU elements depending on the instruction bits.
    // It essentially decodes a received instruction and controls the components (e.g. ALU and registers)
    // its connected to, to execute the instruction.
    private partial class ControlUnit
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
        private readonly ALU _alu;
        private readonly MMU _mmu;

        internal ControlUnit(CPU cpu, ALU alu, MMU mmu)
        {
            _cpu = cpu;
            _alu = alu;
            _mmu = mmu;

            // Internal bus 1 - 3 initialization
            B1_REGISTERS = new Dictionary<uint, Register>
            {
                {PC_REGISTER_CODE, PC},
                {GP1_REGISTER_CODE, _cpu.GP1},
                {GP2_REGISTER_CODE, _cpu.GP2},
                {GP3_REGISTER_CODE, _cpu.GP3},
                {GP4_REGISTER_CODE, _cpu.GP4},
                {GP5_REGISTER_CODE, _cpu.GP5},
                {GP6_REGISTER_CODE, _cpu.GP6},
                {GP7_REGISTER_CODE, _cpu.GP7},
                {GP8_REGISTER_CODE, _cpu.GP8},
                {MAR_SPECIAL_REGISTER_CODE, _mmu.MAR},
                {IRA_SPECIAL_REGISTER_CODE, IRA},
                {IRB_SPECIAL_REGISTER_CODE, IRB}
                /*{SR_REGISTER_CODE, _alu.SR}*/
            };
            B2_REGISTERS = new Dictionary<uint, Register>
            {
                {GP1_REGISTER_CODE, _cpu.GP1},
                {GP2_REGISTER_CODE, _cpu.GP2},
                {GP3_REGISTER_CODE, _cpu.GP3},
                {GP4_REGISTER_CODE, _cpu.GP4},
                {GP5_REGISTER_CODE, _cpu.GP5},
                {GP6_REGISTER_CODE, _cpu.GP6},
                {GP7_REGISTER_CODE, _cpu.GP7},
                {GP8_REGISTER_CODE, _cpu.GP8},
                {MDR_SPECIAL_REGISTER_CODE, _mmu.MAR}
            };
            B3_REGISTERS = new Dictionary<uint, Register>
            {
                {PC_REGISTER_CODE, PC},
                {GP1_REGISTER_CODE, _cpu.GP1},
                {GP2_REGISTER_CODE, _cpu.GP2},
                {GP3_REGISTER_CODE, _cpu.GP3},
                {GP4_REGISTER_CODE, _cpu.GP4},
                {GP5_REGISTER_CODE, _cpu.GP5},
                {GP6_REGISTER_CODE, _cpu.GP6},
                {GP7_REGISTER_CODE, _cpu.GP7},
                {GP8_REGISTER_CODE, _cpu.GP8},
                {MAR_SPECIAL_REGISTER_CODE, _mmu.MAR},
                {MDR_SPECIAL_REGISTER_CODE, _mmu.MDR},
                {IRA_SPECIAL_REGISTER_CODE, IRA},
                {IRB_SPECIAL_REGISTER_CODE, IRB}
            };

            _IntBus1Src = new MultiSrcSingleDstRegisterSelector(
                Register.SYSTEM_WORD_SIZE, INTBUS_B1_UBID, B1_REGISTERS, PC_REGISTER_CODE, alu.A);
            _IntBus2Src = new MultiSrcSingleDstRegisterSelector(
                Register.SYSTEM_WORD_SIZE, INTBUS_B2_UBID, B2_REGISTERS, MDR_SPECIAL_REGISTER_CODE, alu.B);
            _IntBus3Dst = new SingleSrcMultiDstRegisterSelector(
                Register.SYSTEM_WORD_SIZE, INTBUS_B3_UBID, alu.R, B3_REGISTERS, PC_REGISTER_CODE);

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