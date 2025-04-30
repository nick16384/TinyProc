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
                $"Dst:{CurrentRInstr.DestRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentRInstr.DestRegCode].ValueDirect:X8}] " +
                $"[{CurrentRInstr.ALUOpCode}] " +
                $"Src:{CurrentRInstr.SrcRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentRInstr.SrcRegCode].ValueDirect:X8}]");
            _alu.OpCode = CurrentRInstr.ALUOpCode;
            _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.DestRegCode;
            _IntBus2Src.BusSourceRegisterAddress = CurrentRInstr.SrcRegCode;
            _alu.Status_EnableFlags = true;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentRInstr.DestRegCode;
            _alu.Status_EnableFlags = false;
            ResetBus3();
            Console.WriteLine(
                $" --> Dst:{CurrentRInstr.DestRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentRInstr.DestRegCode].ValueDirect:X8}]");
        }
        
        private void INSTRUCTION_R_LOADR()
        {
            Console.WriteLine(
                "Load from memory to register " +
                $"Dst:{CurrentRInstr.DestRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentRInstr.DestRegCode].ValueDirect:X8}] " +
                "at address contained in register " +
                $"Src:{CurrentRInstr.SrcRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentRInstr.SrcRegCode].ValueDirect:X8}]");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.SrcRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MAR;

            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferB];
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_MDR;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentRInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_R_STORR()
        {
            Console.WriteLine(
                "Store to memory from register " +
                $"Dst:{CurrentRInstr.DestRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentRInstr.DestRegCode].ValueDirect:X8}] " +
                "at address contained in register " +
                $"Src:{CurrentRInstr.SrcRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentRInstr.SrcRegCode].ValueDirect:X8}]");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.SrcRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MAR;
            ResetBus3();

            _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MDR;
            ResetBus3();
        }

        private void INSTRUCTION_I_AOPI()
        {
            Console.Write(
                "Arithmetic immediate operation: " +
                $"#{CurrentIInstr.Immediate:X8} [{CurrentIInstr.ALUOpCode}] " +
                $"Dst:{CurrentIInstr.DestRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentIInstr.DestRegCode].ValueDirect:X8}]");
            _alu.OpCode = CurrentIInstr.ALUOpCode;
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus2Src.BusSourceRegisterAddress = CurrentIInstr.DestRegCode;
            _alu.Status_EnableFlags = true;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentIInstr.DestRegCode;
            _alu.Status_EnableFlags = false;
            ResetBus3();
            Console.WriteLine(
                $" --> Dst:{CurrentIInstr.DestRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentIInstr.DestRegCode].ValueDirect:X8}]");
        }

        private void INSTRUCTION_I_LOAD()
        {
            Console.WriteLine(
                "Load from memory to register " +
                $"Dst:{CurrentIInstr.DestRegCode:X8} " +
                "at immediate address " +
                $"#{CurrentIInstr.Immediate:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MAR;

            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferB];
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_MDR;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentIInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_I_STORE()
        {
            Console.WriteLine(
                "Store to memory from register " +
                $"Dst:{CurrentIInstr.DestRegCode:X8}[{CU_ADDRESSABLE_REGISTERS[CurrentIInstr.DestRegCode].ValueDirect:X8}] " +
                "at immediate address " +
                $"#{CurrentIInstr.Immediate:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MAR;
            ResetBus3();

            _IntBus1Src.BusSourceRegisterAddress = CurrentIInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MDR;
            ResetBus3();
        }

        private void INSTRUCTION_J_NOP()
        {
            Console.WriteLine("No operation.");
            // Do literally nothing. Same as jumping to next address, which is automatically done.
        }
        private void INSTRUCTION_J_JMP()
        {
            Console.WriteLine($"Jump to address {IRB.ValueDirect:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_PC;
            ResetBus3();
        }
        private void INSTRUCTION_J_B()
        {
            // Conditionals have already been handled at this point
            INSTRUCTION_J_JMP();
        }
    }
}