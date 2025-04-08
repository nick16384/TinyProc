namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Instruction definitions. Define, what each instruction actually does.

        private void INSTRUCTION_R_CLZ()
        {
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
            Console.WriteLine(
                "Arithmetic register operation: " +
                $"Src:{CurrentRInstr.SrcRegCode:X8} [{CurrentRInstr.ALUOpCode}] " +
                $"Dst:{CurrentRInstr.DestRegCode:X8} --> Dst:{CurrentRInstr.DestRegCode:X8}");
                _alu.OpCode = CurrentRInstr.ALUOpCode;
                _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.SrcRegCode;
                _IntBus2Src.BusSourceRegisterAddress = CurrentRInstr.DestRegCode;
                _IntBus3Dst.BusTargetRegisterAddress = CurrentRInstr.DestRegCode;
                ResetBus3();
        }
        
        private void INSTRUCTION_R_LOADR()
        {
            Console.WriteLine(
                "Load from memory to register " +
                $"Dst:{CurrentRInstr.DestRegCode:X8} at address contained in register " +
                $"Src:{CurrentRInstr.SrcRegCode:X8}");
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
                $"Dst:{CurrentRInstr.DestRegCode:X8} at address contained in register " +
                $"Src:{CurrentRInstr.SrcRegCode:X8}");
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
            Console.WriteLine(
                "Arithmetic immediate operation: " +
                $"#{CurrentIInstr.Immediate:X8} [{CurrentIInstr.ALUOpCode}] " +
                $"Dst:{CurrentIInstr.DestRegCode:X8} --> Dst:{CurrentIInstr.DestRegCode:X8}");
            _alu.OpCode = CurrentIInstr.ALUOpCode;
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus2Src.BusSourceRegisterAddress = CurrentIInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentIInstr.DestRegCode;
            ResetBus3();
        }

        private void INSTRUCTION_I_LOAD()
        {
            Console.WriteLine(
                "Load from memory to register " +
                $"Dst:{CurrentIInstr.DestRegCode:X8} at immediate address " +
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
                $"Dst:{CurrentIInstr.DestRegCode:X8} at immediate address " +
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
            Console.WriteLine($"Jump to address {IRB.ValueDirect}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_PC;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_VOID;
        }
        private void INSTRUCTION_J_B()
        {
            Console.Error.WriteLine("Branch instruction: Conditionals not supported yet. Not branching.");
        }
    }
}