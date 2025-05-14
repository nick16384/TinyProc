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
            Console.Write(
                "Arithmetic register operation: " +
                $"Dst:{_currentInstruction.R_GetDestRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_GetDestRegCode()].ValueDirect:x8}] " +
                $"<{_currentInstruction.R_GetALUOpcode}> " +
                $"Src:{_currentInstruction.R_GetSrcRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_GetSrcRegCode()].ValueDirect:x8}]");
            _alu.CurrentOpcode = _currentInstruction.R_GetALUOpcode();
            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_GetDestRegCode();
            _IntBus2.BusSourceRegisterCode = _currentInstruction.R_GetSrcRegCode();
            _alu.Status_EnableFlags = true;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.R_GetDestRegCode();
            _alu.Status_EnableFlags = false;
            ResetBus3();
            Console.WriteLine(
                $" --> Dst:{_currentInstruction.R_GetDestRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_GetDestRegCode()].ValueDirect:x8}]");
        }
        
        private void INSTRUCTION_R_LOADR()
        {
            Console.WriteLine(
                "Load from memory to register " +
                $"Dst:{_currentInstruction.R_GetDestRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_GetDestRegCode()].ValueDirect:x8}] " +
                "at address contained in register " +
                $"Src:{_currentInstruction.R_GetSrcRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_GetSrcRegCode()].ValueDirect:x8}]");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_GetSrcRegCode();
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.R_GetDestRegCode();
            ResetBus3();
        }
        private void INSTRUCTION_R_STORR()
        {
            Console.WriteLine(
                "Store to memory from register " +
                $"Dst:{_currentInstruction.R_GetDestRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_GetDestRegCode()].ValueDirect:x8}] " +
                "at address contained in register " +
                $"Src:{_currentInstruction.R_GetSrcRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.R_GetSrcRegCode()].ValueDirect:x8}]");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_GetSrcRegCode();
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _IntBus1.BusSourceRegisterCode = _currentInstruction.R_GetDestRegCode();
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            ResetBus3();
        }

        private void INSTRUCTION_I_AOPI()
        {
            Console.Write(
                "Arithmetic immediate operation: " +
                $"#{_currentInstruction.I_GetImmediateValue():x8} <{_currentInstruction.I_GetALUOpcode()}> " +
                $"Dst:{_currentInstruction.I_GetDestRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_GetDestRegCode()].ValueDirect:x8}]");
            _alu.CurrentOpcode = _currentInstruction.I_GetALUOpcode();
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus2.BusSourceRegisterCode = _currentInstruction.I_GetDestRegCode();
            _alu.Status_EnableFlags = true;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.I_GetDestRegCode();
            _alu.Status_EnableFlags = false;
            ResetBus3();
            Console.WriteLine(
                $" --> Dst:{_currentInstruction.I_GetDestRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_GetDestRegCode()].ValueDirect:x8}]");
        }

        private void INSTRUCTION_I_LOAD()
        {
            Console.WriteLine(
                "Load from memory to register " +
                $"Dst:{_currentInstruction.I_GetDestRegCode()} " +
                "at immediate address " +
                $"#{_currentInstruction.I_GetImmediateValue():x8}");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;

            _alu.CurrentOpcode = ALU.ALUOpcode.TransferB;
            _IntBus2.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            _IntBus3.BusTargetRegisterCode = _currentInstruction.I_GetDestRegCode();
            ResetBus3();
        }
        private void INSTRUCTION_I_STORE()
        {
            Console.WriteLine(
                "Store to memory from register " +
                $"Dst:{_currentInstruction.I_GetDestRegCode()}" +
                $"[{CU_ADDRESSABLE_REGISTERS[_currentInstruction.I_GetDestRegCode()].ValueDirect:x8}] " +
                "at immediate address " +
                $"#{_currentInstruction.I_GetImmediateValue():x8}");
            _alu.CurrentOpcode = ALU.ALUOpcode.TransferA;
            _IntBus1.BusSourceRegisterCode = InternalRegisterCode.RCODE_SPECIAL_IRB;
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MAR;
            ResetBus3();

            _IntBus1.BusSourceRegisterCode = _currentInstruction.I_GetDestRegCode();
            _IntBus3.BusTargetRegisterCode = InternalRegisterCode.RCODE_SPECIAL_MDR;
            ResetBus3();
        }

        private void INSTRUCTION_J_NOP()
        {
            Console.WriteLine("No operation.");
            // Do literally nothing. Same as jumping to next address, which is automatically done.
        }
        private void INSTRUCTION_J_JMP()
        {
            Console.WriteLine($"Jump to address {IRB.ValueDirect:x8}");
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
    }
}