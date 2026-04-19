using System.Runtime.CompilerServices;
using TinyProc.Application;
using static TinyProc.Processor.Instructions;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        /// <summary>
        /// Resets internal bus 3 so its destination is the void register.
        /// Otherwise, previously addressed registers could be overridden by new operations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetBus3()
        {
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_VOID;
        }

        /// <summary>
        /// Helper used to create a string consisting of the name and value of any CU-addressable register.
        /// </summary>
        /// <param name="regCode"></param>
        /// <returns></returns>
        private string RegisterNameAndValueFormatted(InternalRegisterCode regCode)
            => $"{regCode}[{CU_ADDRESSABLE_REGISTERS[regCode].ValueDirect:x8}]";

        // Useful methods for CPU data flow
        internal void CopyFromRegisterToRegister(InternalRegisterCode source, InternalRegisterCode destination, bool enableFlags = true)
        {
            lock (_alu)
            {
                _alu.Status_EnableFlags = enableFlags;
                if (B1_REGISTERS.ContainsKey(source) && B3_REGISTERS.ContainsKey(destination))
                {
                    _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
                    _IntBus1.BusSourceRegisterCode = source;
                    _IntBus3.BusTargetRegisterCode = destination;
                }
                else if (B2_REGISTERS.ContainsKey(source) && B3_REGISTERS.ContainsKey(destination))
                {
                    _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
                    _IntBus2.BusSourceRegisterCode = source;
                    _IntBus3.BusTargetRegisterCode = destination;
                }
                else
                {
                    if (!B1_REGISTERS.ContainsKey(source) && !B2_REGISTERS.ContainsKey(source))
                        throw new Exception($"Inter-register copy error: Internal source register code {source:x} is invalid.");
                    else if (!B3_REGISTERS.ContainsKey(destination))
                        throw new Exception($"Inter-register copy error: Internal destination register code {destination:x} is invalid.");
                }
                _alu.Status_EnableFlags = false;
                ResetBus3();
            }
        }
        internal void PushOntoStack(InternalRegisterCode sourceRegister)
        {
            // Increment the stack pointer
            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SP;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_CONST_POS1;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SP;
            ResetBus3();
            // Push the data onto the stack
            CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SP, InternalRegisterCode.RCODE_SPECIAL_MAR);
            // FIXME: Enable memory write
            CopyFromRegisterToRegister(sourceRegister, InternalRegisterCode.RCODE_SPECIAL_MDR);
            // FIXME: Disable memory write
        }
        internal void PopFromStack(InternalRegisterCode destinationRegister)
        {
            // Pop element from the stack
            CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SP, InternalRegisterCode.RCODE_SPECIAL_MAR);
            // FIXME: Enable memory read
            CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SPECIAL_MDR, destinationRegister);
            // FIXME: Disable memory read
            // Decrement the stack pointer
            _alu.CurrentOpcode = ALU.ALUOpcode.AB_SubtractionSigned;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SP;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_CONST_POS1;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SP;
            ResetBus3();
        }

        // Load first instruction word
        private void InstructionFetch1()
        {
            Logging.LogDebug(
                $"PC at {PC.ValueDirect:x8}; " +
                $"Status: OF[{(_alu.Status_Overflow ? 1 : 0)}] " +
                $"ZR[{(_alu.Status_Zero ? 1 : 0)}] " +
                $"NG[{(_alu.Status_Negative ? 1 : 0)}] " +
                $"CR[{(_alu.Status_Carry ? 1 : 0)}] " +
                $"INTD[{(_alu.Status_Interrupted ? 1 : 0)}] " +
                $"EINT[{(_alu.Status_InterruptsEnabled ? 1 : 0)}]");

            // PC -> MAR
            CopyFromRegisterToRegister(InternalRegisterCode.RCODE_PC, InternalRegisterCode.RCODE_SPECIAL_MAR, enableFlags: false);

            // MDR -> IRA
            CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SPECIAL_MDR, InternalRegisterCode.RCODE_SPECIAL_IRA, enableFlags: false);
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
            CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SPECIAL_MDR, InternalRegisterCode.RCODE_SPECIAL_IRB, enableFlags: false);

            ResetBus3();
            Logging.LogDebug($"Loaded 2 instruction words: {IRA.ValueDirect:x8} {IRB.ValueDirect:x8}");
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

            InstructionType instructionType = DetermineInstructionType(IRA.ValueDirect);
            if (instructionType == InstructionType.Register)
                _currentInstruction = (RegRegInstruction)(IRA.ValueDirect, IRB.ValueDirect);
            else if (instructionType == InstructionType.Immediate)
                _currentInstruction = (RegImmInstruction)(IRA.ValueDirect, IRB.ValueDirect);
            else if (instructionType == InstructionType.Jump)
                _currentInstruction = (JumpInstruction)(IRA.ValueDirect, IRB.ValueDirect);
            else
                _cpu.TriggerHardwareFault(Fault.UNKNOWN_INSTRUCTION);

            Logging.LogDebug(
                $"Type: {_currentInstruction.InstructionType}; " +
                $"Opcode: {(uint)_currentInstruction.Opcode:X2}->{_currentInstruction.Opcode}; " +
                $"Condition: {(uint)_currentInstruction.Conditional:X2}->{_currentInstruction.Conditional};");
        }

        // Executes the current instruction respecting conditional values.
        // Equivalent to the execute stage in a real CPU's Fetch-Decode-Execute cycle.
        private void InstructionExecute()
        {
            bool execute;
            if (_currentInstruction.Conditional == Condition.ALWAYS)
                execute = true;
            else if (_currentInstruction.Conditional == Condition.OF)
                execute = _alu.Status_Overflow;
            else if (_currentInstruction.Conditional == Condition.NO)
                execute = !_alu.Status_Overflow;
            else if (_currentInstruction.Conditional == Condition.ZR)
                execute = _alu.Status_Zero;
            else if (_currentInstruction.Conditional == Condition.NZ)
                execute = !_alu.Status_Zero;
            else if (_currentInstruction.Conditional == Condition.NG)
                execute = _alu.Status_Negative;
            else if (_currentInstruction.Conditional == Condition.NN)
                execute = !_alu.Status_Negative;
            else
                throw new NotSupportedException($"Condition {_currentInstruction.Conditional} not implemented yet.");
            
            if (!execute)
            {
                Logging.LogDebug($"Not executing: Conditional {_currentInstruction.Conditional} not satisfied.");
                return;
            }

            if (_currentInstruction.AddressingMode == AddressingMode.Absolute)
                Logging.LogDebug("Addressing mode (AM): Absolute");
            else
                Logging.LogDebug("Addressing mode (AM): PC-relative");

            Logging.LogDebug($"Extension bit set: {(_currentInstruction.Extension ? "Yes" : "No")}");

            ExecuteCurrentInstruction();

            // Disable flag setting by ALU
            _alu.Status_EnableFlags = false;

            Logging.LogDebug(
                $"Status: OF[{(_alu.Status_Overflow ? 1 : 0)}] " +
                $"ZR[{(_alu.Status_Zero ? 1 : 0)}] " +
                $"NG[{(_alu.Status_Negative ? 1 : 0)}] " +
                $"CR[{(_alu.Status_Carry ? 1 : 0)}] " +
                $"INTD[{(_alu.Status_Interrupted ? 1 : 0)}] " +
                $"EINT[{(_alu.Status_InterruptsEnabled ? 1 : 0)}]");
        }

        // Differs from InstructionExecute() in the fact that this method does not check for conditionals.
        // It will always execute the current instruction.
        // Conditional execution is handled by the InstructionExecute() method.
        private void ExecuteCurrentInstruction()
        {
            switch (_currentInstruction.InstructionType)
            {
                case InstructionType.Register:
                    if      (_currentInstruction.Opcode == Opcode.TST)   { INSTRUCTION_R_TST(); }
                    else if (_currentInstruction.Opcode == Opcode.CLC)   { INSTRUCTION_R_CLC(); }
                    else if (_currentInstruction.Opcode == Opcode.CLZ)   { INSTRUCTION_R_CLZ(); }
                    else if (_currentInstruction.Opcode == Opcode.CLOF)  { INSTRUCTION_R_CLOF(); }
                    else if (_currentInstruction.Opcode == Opcode.CLNG)  { INSTRUCTION_R_CLNG(); }
                    else if (_currentInstruction.Opcode == Opcode.CLA)   { INSTRUCTION_R_CLA(); }
                    else if (_currentInstruction.Opcode == Opcode.AOPR)  { INSTRUCTION_R_AOPR(); }
                    else if (_currentInstruction.Opcode == Opcode.PUSH)  { INSTRUCTION_R_PUSH(); }
                    else if (_currentInstruction.Opcode == Opcode.POP)   { INSTRUCTION_R_POP(); }
                    else if (_currentInstruction.Opcode == Opcode.CMPR)  { INSTRUCTION_R_CMPR(); }
                    else if (_currentInstruction.Opcode == Opcode.LDR &&
                        _currentInstruction.AddressingMode == AddressingMode.Absolute)   { INSTRUCTION_R_LDR_A(); }
                    else if (_currentInstruction.Opcode == Opcode.LDR &&
                        _currentInstruction.AddressingMode == AddressingMode.PCRelative) { INSTRUCTION_R_LDR_R(); }
                    else if (_currentInstruction.Opcode == Opcode.STR &&
                        _currentInstruction.AddressingMode == AddressingMode.Absolute)   { INSTRUCTION_R_STR_A(); }
                    else if (_currentInstruction.Opcode == Opcode.STR &&
                        _currentInstruction.AddressingMode == AddressingMode.PCRelative) { INSTRUCTION_R_STR_R(); }
                    else if (_currentInstruction.Opcode == Opcode.JMPR &&
                        _currentInstruction.AddressingMode == AddressingMode.Absolute)   { INSTRUCTION_R_JMPR_A(); }
                    else if (_currentInstruction.Opcode == Opcode.JMPR &&
                        _currentInstruction.AddressingMode == AddressingMode.PCRelative) { INSTRUCTION_R_JMPR_R(); }
                    else if (_currentInstruction.Opcode == Opcode.CALLR &&
                        _currentInstruction.AddressingMode == AddressingMode.Absolute)   { INSTRUCTION_R_CALLR_A(); }
                    else if (_currentInstruction.Opcode == Opcode.CALLR &&
                        _currentInstruction.AddressingMode == AddressingMode.PCRelative) { INSTRUCTION_R_CALLR_R(); }
                    else
                        // Note: Usually, opcode decoding happens in the decode stage, so the unknown instruction fault would be generated there and not
                        // during the execute stage.
                        _cpu.TriggerHardwareFault(Fault.UNKNOWN_INSTRUCTION);
                    return;

                case InstructionType.Immediate:
                    if      (_currentInstruction.Opcode == Opcode.AOPI)  { INSTRUCTION_I_AOPI(); }
                    else if (_currentInstruction.Opcode == Opcode.CMP)   { INSTRUCTION_I_CMP(); }
                    else if (_currentInstruction.Opcode == Opcode.LD &&
                        _currentInstruction.AddressingMode == AddressingMode.Absolute)   { INSTRUCTION_I_LD_A(); }
                    else if (_currentInstruction.Opcode == Opcode.LD &&
                        _currentInstruction.AddressingMode == AddressingMode.PCRelative) { INSTRUCTION_I_LD_R(); }
                    else if (_currentInstruction.Opcode == Opcode.ST &&
                        _currentInstruction.AddressingMode == AddressingMode.Absolute)   { INSTRUCTION_I_ST_A(); }
                    else if (_currentInstruction.Opcode == Opcode.ST &&
                        _currentInstruction.AddressingMode == AddressingMode.PCRelative) { INSTRUCTION_I_ST_R(); }
                    else
                        _cpu.TriggerHardwareFault(Fault.UNKNOWN_INSTRUCTION);
                    return;

                case InstructionType.Jump:
                    if      (_currentInstruction.Opcode == Opcode.NOP)   { INSTRUCTION_J_NOP(); }
                    else if (_currentInstruction.Opcode == Opcode.JMP &&
                        _currentInstruction.AddressingMode == AddressingMode.Absolute)   { INSTRUCTION_J_JMP_A(); }
                    else if (_currentInstruction.Opcode == Opcode.JMP &&
                        _currentInstruction.AddressingMode == AddressingMode.PCRelative) { INSTRUCTION_J_JMP_R(); }
                    else if (_currentInstruction.Opcode == Opcode.CALL &&
                        _currentInstruction.AddressingMode == AddressingMode.Absolute)   { INSTRUCTION_J_CALL_A(); }
                    else if (_currentInstruction.Opcode == Opcode.CALL &&
                        _currentInstruction.AddressingMode == AddressingMode.PCRelative) { INSTRUCTION_J_CALL_R(); }
                    else if (_currentInstruction.Opcode == Opcode.RET)   { INSTRUCTION_J_RET(); }
                    else if (_currentInstruction.Opcode == Opcode.INT)   { INSTRUCTION_J_INT(); }
                    else if (_currentInstruction.Opcode == Opcode.IRET)  { INSTRUCTION_J_IRET(); }
                    else
                        _cpu.TriggerHardwareFault(Fault.UNKNOWN_INSTRUCTION);
                    return;
            }
            _cpu.TriggerHardwareFault(Fault.UNKNOWN_INSTRUCTION);
        }
    }
}