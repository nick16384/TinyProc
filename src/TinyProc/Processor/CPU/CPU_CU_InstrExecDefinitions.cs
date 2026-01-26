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
            Console.Error.WriteLine($"{CU_ADDRESSABLE_REGISTERS[InternalRegisterCode.RCODE_GP7].ValueDirect:x8}");
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

            // Extract interrupt vector from instruction operand, get address from vector, jump to vector address

            // Load SHIT offset into GP1
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_CONST_SHIT_OFFSET_ADDRESS;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_GP1;
            ResetBus3();

            
            Console.Error.WriteLine("Moin moin");

            // ALU: Vector + SHIT offset ==> IRB ()
            _alu.CurrentOpcode = ALU.ALUOpcode.Addition;
            // TODO: Maybe change fixed offset (special CV register) to value in memory (changeable later)
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_GP1;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            ResetBus3();

            // Load address IRB into IRB
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            ResetBus3();

            // Jump to IRB
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
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