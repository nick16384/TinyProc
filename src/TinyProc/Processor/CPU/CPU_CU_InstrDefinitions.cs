namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Instruction definitions. Define, what each instruction actually does.

        private void INSTRUCTION_R_MOVR()
        {
            Console.WriteLine(
                "Copy contents of register " +
                $"Src:{CurrentRInstr.SrcRegCode:X8} to " +
                $"Dst:{CurrentRInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.SrcRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentRInstr.DestRegCode;
            ResetBus3();
        }
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
        private void INSTRUCTION_R_ADDR()
        {
            Console.WriteLine(
                "Add registers " +
                $"Src:{CurrentRInstr.SrcRegCode:X8} + " +
                $"Dst:{CurrentRInstr.DestRegCode:X8} -> " +
                $"Dst:{CurrentRInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
            _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.SrcRegCode;
            _IntBus2Src.BusSourceRegisterAddress = CurrentRInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentRInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_R_SUBR()
        {
            Console.WriteLine(
                "Subtract registers " +
                $"Dst:{CurrentRInstr.DestRegCode:X8} - " +
                $"Src:{CurrentRInstr.SrcRegCode:X8} -> " +
                $"Dst:{CurrentRInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AB_SubtractionSigned];
            _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.DestRegCode;
            _IntBus2Src.BusSourceRegisterAddress = CurrentRInstr.SrcRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentRInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_R_MULR()
        {
            throw new NotImplementedException("Instruction MULR not implemented yet.");
        }
        private void INSTRUCTION_R_ANDR()
        {
            Console.WriteLine(
                "Logical AND registers " +
                $"Src:{CurrentRInstr.SrcRegCode:X8} & " +
                $"Dst:{CurrentRInstr.DestRegCode:X8} -> " +
                $"Dst:{CurrentRInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalAND];
            _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.SrcRegCode;
            _IntBus2Src.BusSourceRegisterAddress = CurrentRInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentRInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_R_ORR()
        {
            Console.WriteLine(
                "Logical OR registers " +
                $"Src:{CurrentRInstr.SrcRegCode:X8} | " +
                $"Dst:{CurrentRInstr.DestRegCode:X8} -> " +
                $"Dst:{CurrentRInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalOR];
            _IntBus1Src.BusSourceRegisterAddress = CurrentRInstr.SrcRegCode;
            _IntBus2Src.BusSourceRegisterAddress = CurrentRInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentRInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_R_XORR()
        {
            throw new NotImplementedException("Instruction XORR not implemented yet.");
        }
        private void INSTRUCTION_R_LSR()
        {
            throw new NotImplementedException("Instruction LSR not implemented yet.");
        }
        private void INSTRUCTION_R_RSR()
        {
            throw new NotImplementedException("Instruction RSR not implemented yet.");
        }
        private void INSTRUCTION_R_SRSR()
        {
            throw new NotImplementedException("Instruction SRSR not implemented yet.");
        }
        private void INSTRUCTION_R_ROLR()
        {
            throw new NotImplementedException("Instruction ROLR not implemented yet.");
        }
        private void INSTRUCTION_R_RORR()
        {
            throw new NotImplementedException("Instruction RORR not implemented yet.");
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

        private void INSTRUCTION_I_MOV()
        {
            Console.WriteLine(
                "Copy immediate value " +
                $"#{CurrentIInstr.Immediate:X8} to register " +
                $"Dst:{CurrentIInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentIInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_I_ADD()
        {
            Console.WriteLine(
                "Add immediate value " +
                $"#{CurrentIInstr.Immediate:X8} + " +
                $"Dst:{CurrentIInstr.DestRegCode:X8} -> " +
                $"Dst:{CurrentIInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus2Src.BusSourceRegisterAddress = CurrentIInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentIInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_I_SUB()
        {
            Console.WriteLine(
                "Subtract immediate value " +
                $"Dst:{CurrentIInstr.DestRegCode:X8} - " +
                $"#{CurrentIInstr.Immediate:X8} -> " +
                $"Dst:{CurrentIInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.BA_SubtractionSigned];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus2Src.BusSourceRegisterAddress = CurrentIInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentIInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_I_MUL()
        {
            throw new NotImplementedException("Instruction MUL not implemented yet.");
        }
        private void INSTRUCTION_I_AND()
        {
            Console.WriteLine(
                "Logical AND with immediate value " +
                $"#{CurrentIInstr.Immediate:X8} & " +
                $"Dst:{CurrentIInstr.DestRegCode:X8} -> " +
                $"Dst:{CurrentIInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalAND];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus2Src.BusSourceRegisterAddress = CurrentIInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentIInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_I_OR()
        {
            Console.WriteLine(
                "Logical OR with immediate value " +
                $"#{CurrentIInstr.Immediate:X8} + " +
                $"Dst:{CurrentIInstr.DestRegCode:X8} -> " +
                $"Dst:{CurrentIInstr.DestRegCode:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalOR];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
            _IntBus2Src.BusSourceRegisterAddress = CurrentIInstr.DestRegCode;
            _IntBus3Dst.BusTargetRegisterAddress = CurrentIInstr.DestRegCode;
            ResetBus3();
        }
        private void INSTRUCTION_I_XOR()
        {
            throw new NotImplementedException("Instruction XOR not implemented yet.");
        }
        private void INSTRUCTION_I_LS()
        {
            throw new NotImplementedException("Instruction LS not implemented yet.");
        }
        private void INSTRUCTION_I_RS()
        {
            throw new NotImplementedException("Instruction RS not implemented yet.");
        }
        private void INSTRUCTION_I_SRS()
        {
            throw new NotImplementedException("Instruction SRS not implemented yet.");
        }
        private void INSTRUCTION_I_ROL()
        {
            throw new NotImplementedException("Instruction ROL not implemented yet.");
        }
        private void INSTRUCTION_I_ROR()
        {
            throw new NotImplementedException("Instruction ROR not implemented yet.");
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