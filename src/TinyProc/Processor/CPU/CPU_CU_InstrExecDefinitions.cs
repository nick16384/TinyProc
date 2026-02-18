using TinyProc.Application;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Instruction definitions. Define, what each instruction actually does.

        private void INSTRUCTION_R_TST()
        {
            Logging.LogDebug("Set all flags");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_CONST_NEG1;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SR;
            ResetBus3();
        }
        // TODO: These functions would be implemented differently on hardware. Find out how exactly.
        private void INSTRUCTION_R_CLC()
        {
            Logging.LogDebug("Clear carry flag");
            _alu.Status_Carry = false;
        }
        private void INSTRUCTION_R_CLZ()
        {
            Logging.LogDebug("Clear zero flag");
            _alu.Status_Zero = false;
        }
        private void INSTRUCTION_R_CLOF()
        {
            Logging.LogDebug("Clear overflow flag");
            _alu.Status_Overflow = false;
        }
        private void INSTRUCTION_R_CLNG()
        {
            Logging.LogDebug("Clear negative flag");
            _alu.Status_Negative = false;
        }
        private void INSTRUCTION_R_CLA()
        {
            Logging.LogDebug("Clear all flags");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_CONST_ZERO;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SR;
            ResetBus3();
            _alu.Status_EnableFlags = true;
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

        private void INSTRUCTION_R_LDR_A()
        {
            Logging.LogDebug(
                "Load from memory to register " +
                $"Dst:{_currentInstruction.R_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_DestRegCode].ValueDirect:x8}] " +
                "at absolute address contained in register " +
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
        private void INSTRUCTION_R_LDR_R()
        {
            Logging.LogDebug(
                "Load from memory to register " +
                $"Dst:{_currentInstruction.R_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_DestRegCode].ValueDirect:x8}] " +
                "at relative offset contained in register " +
                $"Src:{_currentInstruction.R_SrcRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}] " +
                $"+ PC[{PC.ValueDirect:x8}] ==> {PC.ValueDirect + CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}");

            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_PC;
            _IntBus2.BusSourceRegisterCode = _currentInstruction.R_SrcRegCode;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.R_DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_R_STR_A()
        {
            Logging.LogDebug(
                "Store to memory from register " +
                $"Dst:{_currentInstruction.R_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_DestRegCode].ValueDirect:x8}] " +
                "at absolute address contained in register " +
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
        private void INSTRUCTION_R_STR_R()
        {
            Logging.LogDebug(
                "Store to memory from register " +
                $"Dst:{_currentInstruction.R_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_DestRegCode].ValueDirect:x8}] " +
                "at relative offset contained in register " +
                $"Src:{_currentInstruction.R_SrcRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}] " +
                $"+ PC[{PC.ValueDirect:x8}] ==> {PC.ValueDirect + CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}");

            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_PC;
            _IntBus2.BusSourceRegisterCode = _currentInstruction.R_SrcRegCode;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
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
                $"Dst:{_currentInstruction.I_DestRegCode}[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_DestRegCode].ValueDirect:x8}]" +
                $" <{_currentInstruction.I_ALUOpcode}> " +
                $"#{_currentInstruction.I_ImmediateValue:x8}");
            _alu.CurrentOpcode = _currentInstruction.I_ALUOpcode;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.I_DestRegCode;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _alu.Status_EnableFlags = true;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.I_DestRegCode;
            _alu.Status_EnableFlags = false;
            ResetBus3();
            Logging.PrintDebug(
                $" --> Dst:{_currentInstruction.I_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_DestRegCode].ValueDirect:x8}]\n");
        }

        private void INSTRUCTION_I_LD_A()
        {
            Logging.LogDebug(
                "Load from memory to register " +
                $"Dst:{_currentInstruction.I_DestRegCode} " +
                "at immediate absolute address " +
                $"#{_currentInstruction.I_ImmediateValue:x8}");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.I_DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_I_LD_R()
        {
            Logging.LogDebug(
                "Load from memory to register " +
                $"Dst:{_currentInstruction.I_DestRegCode} " +
                "at immediate relative offset " +
                $"#{_currentInstruction.I_ImmediateValue:x8} + PC[{PC.ValueDirect:x8}] " +
                $"==> {PC.ValueDirect + _currentInstruction.I_ImmediateValue:x8}");

            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_PC;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            ResetBus3();

            // Actual load happens here:
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.I_DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_I_ST_A()
        {
            Logging.LogDebug(
                "Store to memory from register " +
                $"Dst:{_currentInstruction.I_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_DestRegCode].ValueDirect:x8}] " +
                "at immediate absolute address " +
                $"#{_currentInstruction.I_ImmediateValue:x8}");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.I_DestRegCode;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            ResetBus3();
        }
        private void INSTRUCTION_I_ST_R()
        {
            Logging.LogDebug(
                "Store to memory from register " +
                $"Dst:{_currentInstruction.I_DestRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_DestRegCode].ValueDirect:x8}] " +
                "at immediate relative offset " +
                $"#{_currentInstruction.I_ImmediateValue:x8} + PC[{PC.ValueDirect:x8}] " +
                $"==> {PC.ValueDirect + _currentInstruction.I_ImmediateValue:x8}");

            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_PC;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            ResetBus3();

            // Actual store happens here:
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.I_DestRegCode;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            ResetBus3();
        }

        private void INSTRUCTION_J_NOP()
        {
            Logging.LogDebug("No operation.");
            // Do literally nothing. Same as jumping to next address, which is automatically done.
        }
        private void INSTRUCTION_J_JMP_A()
        {
            Logging.LogDebug($"Absolute jump to address {IRB.ValueDirect:x8}");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_PC;
            ResetBus3();
        }
        private void INSTRUCTION_J_JMP_R()
        {
            Logging.LogDebug($"Relative jump to offset {IRB.ValueDirect:x8} from PC[{PC.ValueDirect:x8}] ==> {PC.ValueDirect + IRB.ValueDirect:x8}");

            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_PC;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_PC;
            ResetBus3();
        }
        private void INSTRUCTION_J_CALL_A()
        {
            Logging.LogDebug($"Absolute call subroutine at {_currentInstruction.J_JumpTargetAddress:x8}");
            PushOntoStack(InternalRegisterCode.RCODE_SR);
            PushOntoStack(InternalRegisterCode.RCODE_PC);
            CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SPECIAL_IRB, InternalRegisterCode.RCODE_PC);
        }
        private void INSTRUCTION_J_CALL_R()
        {
            Logging.LogDebug(
                $"Relative call subroutine at offset {_currentInstruction.J_JumpTargetAddress:x8} " +
                $"from PC[{PC.ValueDirect:x8}] ==> {PC.ValueDirect + _currentInstruction.J_JumpTargetAddress}");
            PushOntoStack(InternalRegisterCode.RCODE_SR);
            PushOntoStack(InternalRegisterCode.RCODE_PC);

            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_PC;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_PC;
            ResetBus3();
        }
        private void INSTRUCTION_R_CALLR_A()
        {
            Logging.LogDebug(
                $"Absolute call subroutine at address in register Src:{_currentInstruction.R_SrcRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}]");
            PushOntoStack(InternalRegisterCode.RCODE_SR);
            PushOntoStack(InternalRegisterCode.RCODE_PC);

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_SrcRegCode;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_PC;
            ResetBus3();
        }
        private void INSTRUCTION_R_CALLR_R()
        {
            Logging.LogDebug(
                $"Relative call subroutine at offset in register Src:{_currentInstruction.R_SrcRegCode}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}] " +
                $"from PC[{PC.ValueDirect:x8}] ==> {PC.ValueDirect + CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_SrcRegCode].ValueDirect:x8}");
            
            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_SrcRegCode;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_PC;
            ResetBus3();
        }
        private void INSTRUCTION_J_RET()
        {
            Logging.LogDebug("Return from subroutine");
            PopFromStack(InternalRegisterCode.RCODE_PC);
            PopFromStack(InternalRegisterCode.RCODE_SR);
        }
        private void INSTRUCTION_J_INT()
        {
            Logging.LogDebug(
                $"Trigger software interrupt with vector {_currentInstruction.J_JumpTargetAddress:x} " +
                $"({GetFaultName((Fault)_currentInstruction.J_JumpTargetAddress)})");
            PushOntoStack(InternalRegisterCode.RCODE_SR);
            // Push working state to stack
            PushOntoStack(InternalRegisterCode.RCODE_GP1);
            PushOntoStack(InternalRegisterCode.RCODE_GP2);
            PushOntoStack(InternalRegisterCode.RCODE_GP3);
            PushOntoStack(InternalRegisterCode.RCODE_GP4);
            PushOntoStack(InternalRegisterCode.RCODE_GP5);
            PushOntoStack(InternalRegisterCode.RCODE_GP6);
            PushOntoStack(InternalRegisterCode.RCODE_GP7);
            PushOntoStack(InternalRegisterCode.RCODE_GP8);
            _alu.Status_InterruptsEnabled = false;
            _alu.Status_Interrupted = true;
            PushOntoStack(InternalRegisterCode.RCODE_PC);

            // Interrupt vector is IRB
            // SHIT offset is stored in SPECIAL_CONST_SHIT_OFFSET_ADDRESS(CSOA) register
            // 1. CSOA + IRB ==> Address for vector in SHIT (SHIT vector offset, SVO)
            // 2. Value at SVO ==> ISR address
            // 3. Jump to ISR addres

            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_CONST_SHIT_OFFSET_ADDRESS;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            // Jump to address contained in [CSOA + IRB] (where IRB is the interrupt vector)
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_PC;
            ResetBus3();
        }
        private void INSTRUCTION_J_IRET()
        {
            Logging.LogDebug("Return from interrupt");
            PopFromStack(InternalRegisterCode.RCODE_PC);
            PopFromStack(InternalRegisterCode.RCODE_GP8);
            PopFromStack(InternalRegisterCode.RCODE_GP7);
            PopFromStack(InternalRegisterCode.RCODE_GP6);
            PopFromStack(InternalRegisterCode.RCODE_GP5);
            PopFromStack(InternalRegisterCode.RCODE_GP4);
            PopFromStack(InternalRegisterCode.RCODE_GP3);
            PopFromStack(InternalRegisterCode.RCODE_GP2);
            PopFromStack(InternalRegisterCode.RCODE_GP1);
            PopFromStack(InternalRegisterCode.RCODE_SR);
            _alu.Status_Interrupted = false;
            _alu.Status_InterruptsEnabled = true;
        }
    }
}