using TinyProc.Application;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Instruction definitions. Define, what each instruction actually does.

        private void INSTRUCTION_R_CLZ()
        {
            // TODO: Implement CLZ, CLOF and CLNG, since these flags are implemented already
            throw new NotImplementedException("Clear zero flag: Flags not implemented yet.");
        }
        private void INSTRUCTION_R_CLOF()
        {
            throw new NotImplementedException("Clear overflow flag: Flags not implemented yet.");
        }
        private void INSTRUCTION_R_CLNG()
        {
            throw new NotImplementedException("Clear negative flag: Flags not implemented yet.");
        }

        private void INSTRUCTION_R_AOPR()
        {
            Logging.LogDebugWithoutNewline(
                "Arithmetic register operation: " +
                $"Dst:{_currentInstruction.R_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_DestRegCode].ValueDirect:x8}] " +
                $"<{_currentInstruction.R_ALUOpcode}> " +
                $"Src:{_currentInstruction.R_SrcRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}]");
            _alu.CurrentOpcode = _currentInstruction.R_ALUOpcode;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_DestRegCode;
            _IntBus2.BusSourceRegisterCode = _currentInstruction.R_SrcRegCode;
            _alu.Status_EnableFlags = true;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.R_DestRegCode;
            _alu.Status_EnableFlags = false;
            ResetBus3();
            Logging.PrintDebug(
                $" --> Dst:{_currentInstruction.R_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_DestRegCode].ValueDirect:x8}]\n");
        }

        private void INSTRUCTION_R_LOADR()
        {
            Logging.LogDebug(
                "Load from memory to register " +
                $"Dst:{_currentInstruction.R_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_DestRegCode].ValueDirect:x8}] " +
                "at address contained in register " +
                $"Src:{_currentInstruction.R_SrcRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}]");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_SrcRegCode;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.R_DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_R_STORR()
        {
            Logging.LogDebug(
                "Store to memory from register " +
                $"Dst:{_currentInstruction.R_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_DestRegCode].ValueDirect:x8}] " +
                "at address contained in register " +
                $"Src:{_currentInstruction.R_SrcRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}]");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_SrcRegCode;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_DestRegCode;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            ResetBus3();
        }
        private void INSTRUCTION_R_PUSH()
        {
            Logging.LogDebug($"Push register {_currentInstruction.R_DestRegCode} to stack\n" +
                $"SP: {_mmu.SP.ValueDirect:x8}");
            PushOntoStack(_currentInstruction.R_SrcRegCode);
        }
        private void INSTRUCTION_R_POP()
        {
            Logging.LogDebug($"Pop from stack to register {_currentInstruction.R_DestRegCode}\n"
                + $"SP: {_mmu.SP.ValueDirect:x8}");
            PopFromStack(_currentInstruction.R_DestRegCode);
        }

        private void INSTRUCTION_I_AOPI()
        {
            Logging.LogDebugWithoutNewline(
                "Arithmetic immediate operation: " +
                $"#{_currentInstruction.I_ImmediateValue:x8} <{_currentInstruction.I_ALUOpcode}> " +
                $"Dst:{_currentInstruction.I_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_DestRegCode].ValueDirect:x8}]");
            _alu.CurrentOpcode = _currentInstruction.I_ALUOpcode;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus2.BusSourceRegisterCode = _currentInstruction.I_DestRegCode;
            _alu.Status_EnableFlags = true;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.I_DestRegCode;
            _alu.Status_EnableFlags = false;
            ResetBus3();
            Logging.PrintDebug(
                $" --> Dst:{_currentInstruction.I_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_DestRegCode].ValueDirect:x8}]\n");
        }

        private void INSTRUCTION_I_LOAD()
        {
            Logging.LogDebug(
                "Load from memory to register " +
                $"Dst:{_currentInstruction.I_DestRegCode} " +
                "at immediate address " +
                $"#{_currentInstruction.I_ImmediateValue:x8}");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.I_DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_I_STORE()
        {
            Logging.LogDebug(
                "Store to memory from register " +
                $"Dst:{_currentInstruction.I_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_DestRegCode].ValueDirect:x8}] " +
                "at immediate address " +
                $"#{_currentInstruction.I_ImmediateValue:x8}");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _IntBus1.BusSourceRegisterCode = _currentInstruction.I_DestRegCode;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            ResetBus3();
        }

        private void INSTRUCTION_J_NOP()
        {
            Logging.LogDebug("No operation.");
            // Do literally nothing. Same as jumping to next address, which is automatically done.
        }
        private void INSTRUCTION_J_JMP()
        {
            Logging.LogDebug($"Jump to address {IRB.ValueDirect:x8}");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_PC;
            ResetBus3();
        }
        private void INSTRUCTION_J_B()
        {
            // Conditionals have already been handled at this point
            INSTRUCTION_J_JMP();
        }
        private void INSTRUCTION_J_CALL()
        {
            Logging.LogDebug($"Call subroutine at {_currentInstruction.J_JumpTargetAddress:x8}");
            PushOntoStack(InternalRegisterCode.RCODE_SR);
            PushOntoStack(InternalRegisterCode.RCODE_PC);
            CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SPECIAL_IRB, InternalRegisterCode.RCODE_PC);
        }
        private void INSTRUCTION_J_RET()
        {
            Logging.LogDebug("Return from subroutine");
            PopFromStack(InternalRegisterCode.RCODE_PC);
            PopFromStack(InternalRegisterCode.RCODE_SR);
        }
        private void INSTRUCTION_J_INT()
        {
            Logging.LogDebug($"Trigger software interrupt with vector {_currentInstruction.J_JumpTargetAddress:x8}");
            throw new NotImplementedException();
        }
        private void INSTRUCTION_J_IRET()
        {
            Logging.LogDebug("Return from interrupt");
            PopFromStack(InternalRegisterCode.RCODE_PC);
            PopFromStack(InternalRegisterCode.RCODE_SR);
            _alu.Status_Interrupted = false;
            _alu.Status_InterruptsEnabled = true;
        }
    }
}