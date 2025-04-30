namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Resets internal bus 3 so its destination is the void register.
        // Otherwise, previously addressed registers could be overridden by new operations.
        private void ResetBus3()
        {
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_VOID;
        }

        // Load first instruction word
        private void InstructionFetch1()
        {
            Console.WriteLine(
                $"PC at {PC.ValueDirect:X8}; " +
                $"Status: OF[{(_alu.Status_Overflow ? 1 : 0)}] " +
                $"ZR[{(_alu.Status_Zero ? 1 : 0)}] " +
                $"NG[{(_alu.Status_Negative ? 1 : 0)}] " +
                $"CR[{(_alu.Status_Carry ? 1 : 0)}]");
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

        private Instructions.InstructionType _currentInstructionType;
        #pragma warning disable CS8629 // Nullable value type may be null.
        private Instructions.InstructionTypeR? _currentRInstr;
        private Instructions.InstructionTypeR CurrentRInstr => _currentRInstr.Value;
        private Instructions.InstructionTypeI? _currentIInstr;
        private Instructions.InstructionTypeI CurrentIInstr => _currentIInstr.Value;
        private Instructions.InstructionTypeJ? _currentJInstr;
        private Instructions.InstructionTypeJ CurrentJInstr => _currentJInstr.Value;
        #pragma warning restore CS8629 // Nullable value type may be null.
        private Instructions.Condition CurrentInstructionCondition
        {
            get
            {
                return _currentInstructionType switch
                {
                    Instructions.InstructionType.Register => CurrentRInstr.Conditional,
                    Instructions.InstructionType.Immediate => CurrentIInstr.Conditional,
                    Instructions.InstructionType.Jump => CurrentJInstr.Conditional
                };
            }
        }

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
            _currentInstructionType = Instructions.DetermineInstructionType(IRA.ValueDirect);
            Console.Write($"Type: {_currentInstructionType}; ");
            switch (_currentInstructionType)
            {
                case Instructions.InstructionType.Register:
                    _currentIInstr = null;
                    _currentJInstr = null;
                    _currentRInstr = Instructions.ParseInstructionAsRType(IRA.ValueDirect, IRB.ValueDirect);
                    Console.WriteLine(
                        $"OpCode: {(uint)CurrentRInstr.OpCode:X2}->{CurrentRInstr.OpCode}; " +
                        $"Condition: {(uint)CurrentRInstr.Conditional:X2}->{CurrentRInstr.Conditional};");
                    break;
                case Instructions.InstructionType.Immediate:
                    _currentRInstr = null;
                    _currentJInstr = null;
                    _currentIInstr = Instructions.ParseInstructionAsIType(IRA.ValueDirect, IRB.ValueDirect);
                    Console.WriteLine(
                        $"OpCode: {(uint)CurrentIInstr.OpCode:X2}->{CurrentIInstr.OpCode}; " +
                        $"Condition: {(uint)CurrentIInstr.Conditional:X2}->{CurrentIInstr.Conditional};");
                    break;
                case Instructions.InstructionType.Jump:
                    _currentRInstr = null;
                    _currentIInstr = null;
                    _currentJInstr = Instructions.ParseInstructionAsJType(IRA.ValueDirect, IRB.ValueDirect);
                    Console.WriteLine(
                        $"OpCode: {(uint)CurrentJInstr.OpCode:X2}->{CurrentJInstr.OpCode}; " +
                        $"Condition: {(uint)CurrentJInstr.Conditional:X2}->{CurrentJInstr.Conditional};");
                    break;
            }
        }

        // Executes the current instruction respecting conditional values.
        // Equivalent to the execute stage in a real CPU's Fetch-Decode-Execute cycle.
        private void InstructionExecute()
        {
            bool execute = false;
            if (CurrentInstructionCondition == Instructions.Condition.ALWAYS)
                execute = true;
            else if (CurrentInstructionCondition == Instructions.Condition.OF)
                execute = _alu.Status_Overflow;
            else if (CurrentInstructionCondition == Instructions.Condition.NO)
                execute = !_alu.Status_Overflow;
            else if (CurrentInstructionCondition == Instructions.Condition.ZR)
                execute = _alu.Status_Zero;
            else if (CurrentInstructionCondition == Instructions.Condition.NZ)
                execute = !_alu.Status_Zero;
            else if (CurrentInstructionCondition == Instructions.Condition.NG)
                execute = _alu.Status_Negative;
            else if (CurrentInstructionCondition == Instructions.Condition.NN)
                execute = !_alu.Status_Negative;
            else
                throw new NotSupportedException($"Condition {CurrentInstructionCondition} not implemented yet.");
            
            if (!execute)
            {
                Console.WriteLine($"Not executing: Conditional {CurrentInstructionCondition} not satisfied.");
                return;
            }

            // TODO: Handle conditionals here
            ExecuteCurrentInstruction();

            // Disable flag setting by ALU
            _alu.Status_EnableFlags = false;

            Console.WriteLine(
                $"Status: OF[{(_alu.Status_Overflow ? 1 : 0)}] " +
                $"ZR[{(_alu.Status_Zero ? 1 : 0)}] " +
                $"NG[{(_alu.Status_Negative ? 1 : 0)}] " +
                $"CR[{(_alu.Status_Carry ? 1 : 0)}]");
        }

        // Differs from InstructionExecute() in the fact that this method does not check for conditionals.
        // It will always execute the current instruction.
        // Conditional execution is handled by the InstructionExecute() method.
        private void ExecuteCurrentInstruction()
        {
            switch (_currentInstructionType)
            {
                case Instructions.InstructionType.Register:
                    if      (CurrentRInstr.OpCode == Instructions.OpCode.CLZ)   { INSTRUCTION_R_CLZ(); }
                    else if (CurrentRInstr.OpCode == Instructions.OpCode.CLOF)  { INSTRUCTION_R_CLOF(); }
                    else if (CurrentRInstr.OpCode == Instructions.OpCode.CLNG)  { INSTRUCTION_R_CLNG(); }
                    else if (CurrentRInstr.OpCode == Instructions.OpCode.AOPR)  { INSTRUCTION_R_AOPR(); }
                    else if (CurrentRInstr.OpCode == Instructions.OpCode.LOADR) { INSTRUCTION_R_LOADR(); }
                    else if (CurrentRInstr.OpCode == Instructions.OpCode.STORR) { INSTRUCTION_R_STORR(); }
                    break;

                case Instructions.InstructionType.Immediate:
                    if      (CurrentIInstr.OpCode == Instructions.OpCode.AOPI)  { INSTRUCTION_I_AOPI(); }
                    else if (CurrentIInstr.OpCode == Instructions.OpCode.LOAD)  { INSTRUCTION_I_LOAD(); }
                    else if (CurrentIInstr.OpCode == Instructions.OpCode.STORE) { INSTRUCTION_I_STORE(); }
                    break;

                case Instructions.InstructionType.Jump:
                    if      (CurrentJInstr.OpCode == Instructions.OpCode.NOP)   { INSTRUCTION_J_NOP(); }
                    else if (CurrentJInstr.OpCode == Instructions.OpCode.JMP)   { INSTRUCTION_J_JMP(); }
                    else if (CurrentJInstr.OpCode == Instructions.OpCode.B)     { INSTRUCTION_J_B(); }
                    break;
            }
        }
    }
}