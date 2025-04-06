namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Resets internal bus 3 so its destination is the void register.
        // Otherwise, previous operations could interfere with operations happening after them.
        private void ResetBus3()
        {
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_VOID;
        }

        // Load first instruction word
        private void InstructionFetch1()
        {
            Console.WriteLine($"PC at {PC.ValueDirect:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            // PC -> MAR
            _IntBus1Src.BusSourceRegisterAddress = RCODE_PC;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MAR;

            // MDR -> IRA
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferB];
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_MDR;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_IRA;

            // Set void as target to not mess up IRA values in Fetch2
            ResetBus3();
        }
        // Load second instruction word
        private void InstructionFetch2()
        {
            // PC + 1 -> MAR
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_PC;
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_CV_P1;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MAR;

            // MDR -> IRB
            ResetBus3();
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferB];
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_MDR;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_IRB;

            ResetBus3();
            Console.WriteLine($"Loaded 2 instruction words: {IRA.ValueDirect:X8} {IRB.ValueDirect:X8}");
        }

        private InstructionType _currentInstructionType;
        #pragma warning disable CS8629 // Nullable value type may be null.
        private InstructionTypeR? _currentRInstr;
        private InstructionTypeR CurrentRInstr => _currentRInstr.Value;
        private InstructionTypeI? _currentIInstr;
        private InstructionTypeI CurrentIInstr => _currentIInstr.Value;
        private InstructionTypeJ? _currentJInstr;
        private InstructionTypeJ CurrentJInstr => _currentJInstr.Value;
        #pragma warning restore CS8629 // Nullable value type may be null.
        // Essentially prepares the Control Unit for the execute stage
        private void InstructionDecode()
        {
            // PC + 2 -> PC (Increment PC to next instruction)
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_PC;
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_CV_P2;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_PC;
            ResetBus3();

            // Determine instruction type (R/I/J) and parse contents to InstructionType_ struct
            _currentInstructionType = DetermineInstructionType(IRA.ValueDirect);
            Console.Write($"Type: {_currentInstructionType}; ");
            switch (_currentInstructionType)
            {
                case InstructionType.Register:
                    _currentIInstr = null;
                    _currentJInstr = null;
                    _currentRInstr = ParseInstructionAsRType(IRA.ValueDirect, IRB.ValueDirect);
                    Console.WriteLine(
                        $"OpCode: {(uint)CurrentRInstr.OpCode:X2}->{CurrentRInstr.OpCode};" +
                        $"Cond: {(uint)CurrentRInstr.Conditional:X2}->{CurrentRInstr.Conditional};");
                    break;
                case InstructionType.Immediate:
                    _currentRInstr = null;
                    _currentJInstr = null;
                    _currentIInstr = ParseInstructionAsIType(IRA.ValueDirect, IRB.ValueDirect);
                    Console.WriteLine(
                        $"OpCode: {(uint)CurrentIInstr.OpCode:X2}->{CurrentIInstr.OpCode};" +
                        $"Cond: {(uint)CurrentIInstr.Conditional:X2}->{CurrentIInstr.Conditional};");
                    break;
                case InstructionType.Jump:
                    _currentRInstr = null;
                    _currentIInstr = null;
                    _currentJInstr = ParseInstructionAsJType(IRA.ValueDirect, IRB.ValueDirect);
                    Console.WriteLine(
                        $"OpCode: {(uint)CurrentJInstr.OpCode:X2}->{CurrentJInstr.OpCode};" +
                        $"Cond: {(uint)CurrentJInstr.Conditional:X2}->{CurrentJInstr.Conditional};");
                    break;
            }
        }

        // Executes the current instruction respecting conditional values.
        // Equivalent to the execute stage in a real CPU's Fetch-Decode-Execute cycle.
        private void InstructionExecute()
        {
            if (_currentInstructionType == InstructionType.Register && CurrentRInstr.Conditional != Condition.ALWAYS
                || _currentInstructionType == InstructionType.Immediate && CurrentIInstr.Conditional != Condition.ALWAYS
                || _currentInstructionType == InstructionType.Jump && CurrentJInstr.Conditional != Condition.ALWAYS)
            {
                Console.Error.WriteLine("Conditionals not implemented yet. Exiting early.");
                return;
            }

            // TODO: Handle conditionals here
            ExecuteCurrentInstruction();
        }

        // Differs from InstructionExecute() in the fact that this method does not check for conditionals.
        // It will always execute the current instruction.
        // Conditional execution is handled by the InstructionExecute() method.
        private void ExecuteCurrentInstruction()
        {
            switch (_currentInstructionType)
            {
                case InstructionType.Register:
                    if      (CurrentRInstr.OpCode == OpCode.MOVR)  { INSTRUCTION_R_MOVR(); }
                    else if (CurrentRInstr.OpCode == OpCode.CLZ)   { INSTRUCTION_R_CLZ(); }
                    else if (CurrentRInstr.OpCode == OpCode.CLOF)  { INSTRUCTION_R_CLOF(); }
                    else if (CurrentRInstr.OpCode == OpCode.CLNG)  { INSTRUCTION_R_CLNG(); }
                    else if (CurrentRInstr.OpCode == OpCode.ADDR)  { INSTRUCTION_R_ADDR(); }
                    else if (CurrentRInstr.OpCode == OpCode.SUBR)  { INSTRUCTION_R_SUBR(); }
                    else if (CurrentRInstr.OpCode == OpCode.MULR)  { INSTRUCTION_R_MULR(); }
                    else if (CurrentRInstr.OpCode == OpCode.ANDR)  { INSTRUCTION_R_ANDR(); }
                    else if (CurrentRInstr.OpCode == OpCode.ORR)   { INSTRUCTION_R_ORR(); }
                    else if (CurrentRInstr.OpCode == OpCode.XORR)  { INSTRUCTION_R_XORR(); }
                    else if (CurrentRInstr.OpCode == OpCode.LSR)   { INSTRUCTION_R_LSR(); }
                    else if (CurrentRInstr.OpCode == OpCode.RSR)   { INSTRUCTION_R_RSR(); }
                    else if (CurrentRInstr.OpCode == OpCode.SRSR)  { INSTRUCTION_R_SRSR(); }
                    else if (CurrentRInstr.OpCode == OpCode.ROLR)  { INSTRUCTION_R_ROLR(); }
                    else if (CurrentRInstr.OpCode == OpCode.RORR)  { INSTRUCTION_R_RORR(); }
                    else if (CurrentRInstr.OpCode == OpCode.LOADR) { INSTRUCTION_R_LOADR(); }
                    else if (CurrentRInstr.OpCode == OpCode.STORR) { INSTRUCTION_R_STORR(); }
                    break;

                case InstructionType.Immediate:
                    if      (CurrentIInstr.OpCode == OpCode.MOV)   { INSTRUCTION_I_MOV(); }
                    else if (CurrentIInstr.OpCode == OpCode.ADD)   { INSTRUCTION_I_ADD(); }
                    else if (CurrentIInstr.OpCode == OpCode.SUB)   { INSTRUCTION_I_SUB(); }
                    else if (CurrentIInstr.OpCode == OpCode.MUL)   { INSTRUCTION_I_MUL(); }
                    else if (CurrentIInstr.OpCode == OpCode.AND)   { INSTRUCTION_I_AND(); }
                    else if (CurrentIInstr.OpCode == OpCode.OR)    { INSTRUCTION_I_OR(); }
                    else if (CurrentIInstr.OpCode == OpCode.XOR)   { INSTRUCTION_I_XOR(); }
                    else if (CurrentIInstr.OpCode == OpCode.LS)    { INSTRUCTION_I_LS(); }
                    else if (CurrentIInstr.OpCode == OpCode.RS)    { INSTRUCTION_I_RS(); }
                    else if (CurrentIInstr.OpCode == OpCode.SRS)   { INSTRUCTION_I_SRS(); }
                    else if (CurrentIInstr.OpCode == OpCode.ROL)   { INSTRUCTION_I_ROL(); }
                    else if (CurrentIInstr.OpCode == OpCode.ROR)   { INSTRUCTION_I_ROR(); }
                    else if (CurrentIInstr.OpCode == OpCode.LOAD)  { INSTRUCTION_I_LOAD(); }
                    else if (CurrentIInstr.OpCode == OpCode.STORE) { INSTRUCTION_I_STORE(); }
                    break;

                case InstructionType.Jump:
                    if      (CurrentJInstr.OpCode == OpCode.NOP)   { INSTRUCTION_J_NOP(); }
                    else if (CurrentJInstr.OpCode == OpCode.JMP)   { INSTRUCTION_J_JMP(); }
                    else if (CurrentJInstr.OpCode == OpCode.B)     { INSTRUCTION_J_B(); }
                    break;
            }
        }
    }
}