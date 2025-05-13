using static TinyProc.Processor.Instructions;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Resets internal bus 3 so its destination is the void register.
        // Otherwise, previously addressed registers could be overridden by new operations.
        private void ResetBus3()
        {
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_VOID;
        }

        // Load first instruction word
        private void InstructionFetch1()
        {
            Console.WriteLine("Entering FETCH stage...");
            Console.WriteLine(
                $"PC at {PC.ValueDirect:x8}; " +
                $"Status: OF[{(_alu.Status_Overflow ? 1 : 0)}] " +
                $"ZR[{(_alu.Status_Zero ? 1 : 0)}] " +
                $"NG[{(_alu.Status_Negative ? 1 : 0)}] " +
                $"CR[{(_alu.Status_Carry ? 1 : 0)}]");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            // PC -> MAR
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_PC;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;

            // MDR -> IRA
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRA;

            // Set void as target to not mess up IRA values in Fetch2
            ResetBus3();
        }
        // Load second instruction word
        private void InstructionFetch2()
        {
            // PC + 1 -> MAR
            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_PC;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_CONST_POS1;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;

            // MDR -> IRB
            ResetBus3();
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;

            ResetBus3();
            Console.WriteLine($"Loaded 2 instruction words: {IRA.ValueDirect:x8} {IRB.ValueDirect:x8}");
        }

        private IInstruction _currentInstruction;

        // Essentially prepares the Control Unit for the execute stage
        private void InstructionDecode()
        {
            // PC + 2 -> PC (Increment PC to next instruction)
            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_PC;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_CONST_POS2;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_PC;
            ResetBus3();

            Console.WriteLine("Entering DECODE stage...");

            InstructionType instructionType = DetermineInstructionType(IRA.ValueDirect);
            if (instructionType == InstructionType.Register)
                _currentInstruction = (RegRegInstruction)(IRA.ValueDirect, IRB.ValueDirect);
            else if (instructionType == InstructionType.Immediate)
                _currentInstruction = (RegImmInstruction)(IRA.ValueDirect, IRB.ValueDirect);
            else if (instructionType == InstructionType.Jump)
                _currentInstruction = (JumpInstruction)(IRA.ValueDirect, IRB.ValueDirect);

            Console.WriteLine(
                $"Type: {_currentInstruction.GetInstructionType()}; " +
                $"Opcode: {(uint)_currentInstruction.GetOpcode():X2}->{_currentInstruction.GetOpcode()}; " +
                $"Condition: {(uint)_currentInstruction.GetConditional():X2}->{_currentInstruction.GetConditional()};");
        }

        // Executes the current instruction respecting conditional values.
        // Equivalent to the execute stage in a real CPU's Fetch-Decode-Execute cycle.
        private void InstructionExecute()
        {
            Console.WriteLine("Entering EXECUTE stage...");
            bool execute = false;
            if (_currentInstruction.GetConditional() == Condition.ALWAYS)
                execute = true;
            else if (_currentInstruction.GetConditional() == Condition.OF)
                execute = _alu.Status_Overflow;
            else if (_currentInstruction.GetConditional() == Condition.NO)
                execute = !_alu.Status_Overflow;
            else if (_currentInstruction.GetConditional() == Condition.ZR)
                execute = _alu.Status_Zero;
            else if (_currentInstruction.GetConditional() == Condition.NZ)
                execute = !_alu.Status_Zero;
            else if (_currentInstruction.GetConditional() == Condition.NG)
                execute = _alu.Status_Negative;
            else if (_currentInstruction.GetConditional() == Condition.NN)
                execute = !_alu.Status_Negative;
            else
                throw new NotSupportedException($"Condition {_currentInstruction.GetConditional()} not implemented yet.");
            
            if (!execute)
            {
                Console.WriteLine($"Not executing: Conditional {_currentInstruction.GetConditional()} not satisfied.");
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
            switch (_currentInstruction.GetInstructionType())
            {
                case InstructionType.Register:
                    if      (_currentInstruction.GetOpcode() == Opcode.CLZ)   { INSTRUCTION_R_CLZ(); }
                    else if (_currentInstruction.GetOpcode() == Opcode.CLOF)  { INSTRUCTION_R_CLOF(); }
                    else if (_currentInstruction.GetOpcode() == Opcode.CLNG)  { INSTRUCTION_R_CLNG(); }
                    else if (_currentInstruction.GetOpcode() == Opcode.AOPR)  { INSTRUCTION_R_AOPR(); }
                    else if (_currentInstruction.GetOpcode() == Opcode.LOADR) { INSTRUCTION_R_LOADR(); }
                    else if (_currentInstruction.GetOpcode() == Opcode.STORR) { INSTRUCTION_R_STORR(); }
                    break;

                case InstructionType.Immediate:
                    if      (_currentInstruction.GetOpcode() == Opcode.AOPI)  { INSTRUCTION_I_AOPI(); }
                    else if (_currentInstruction.GetOpcode() == Opcode.LOAD)  { INSTRUCTION_I_LOAD(); }
                    else if (_currentInstruction.GetOpcode() == Opcode.STORE) { INSTRUCTION_I_STORE(); }
                    break;

                case InstructionType.Jump:
                    if      (_currentInstruction.GetOpcode() == Opcode.NOP)   { INSTRUCTION_J_NOP(); }
                    else if (_currentInstruction.GetOpcode() == Opcode.JMP)   { INSTRUCTION_J_JMP(); }
                    else if (_currentInstruction.GetOpcode() == Opcode.B)     { INSTRUCTION_J_B(); }
                    break;
            }
        }
    }
}