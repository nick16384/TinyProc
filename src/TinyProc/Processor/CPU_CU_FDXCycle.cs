namespace TinyProc.Processor;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Load first instruction word
        private void InstructionFetch1()
        {
            Console.WriteLine($"PC at {PC.Value:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            // Transfer PC to MAR
            _IntBus1Src.BusSourceRegisterAddress = PC_REGISTER_CODE;
            _IntBus3Dst.BusTargetRegisterAddress = MAR_SPECIAL_REGISTER_CODE;
            // Transfer MDR to IRA
            _IntBus1Src.BusSourceRegisterAddress = MDR_SPECIAL_REGISTER_CODE;
            _IntBus3Dst.BusTargetRegisterAddress = IRA_SPECIAL_REGISTER_CODE;
        }
        // Load second instruction word
        private void InstructionFetch2()
        {
            // Increment program counter by one
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
            _IntBus1Src.BusSourceRegisterAddress = PC_REGISTER_CODE;
            _IntBus2Src.BusSourceRegisterAddress = CONST_PLUSONE_SPECIAL_REGISTER_CODE;
            _IntBus3Dst.BusTargetRegisterAddress = PC_REGISTER_CODE;

            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            // Transfer PC to MAR
            _IntBus1Src.BusSourceRegisterAddress = PC_REGISTER_CODE;
            _IntBus3Dst.BusTargetRegisterAddress = MAR_SPECIAL_REGISTER_CODE;
            // Transfer MDR to IRA
            _IntBus1Src.BusSourceRegisterAddress = MDR_SPECIAL_REGISTER_CODE;
            _IntBus3Dst.BusTargetRegisterAddress = IRB_SPECIAL_REGISTER_CODE;
            Console.WriteLine($"Loaded 2 instruction words: {IRA.Value:X8} {IRB.Value:X8}");
        }

        private OpCode _opCode;
        private Condition _conditional;
        // Essentially prepares the Control Unit for the execute stage
        private void InstructionDecode()
        {
            // Increment PC to next instruction
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
            _IntBus1Src.BusSourceRegisterAddress = PC_REGISTER_CODE;
            _IntBus2Src.BusSourceRegisterAddress = CONST_PLUSONE_SPECIAL_REGISTER_CODE;
            _IntBus3Dst.BusTargetRegisterAddress = PC_REGISTER_CODE;

            // Decode OpCode bits to OpCode type
            byte opCodeBits = Convert.ToByte((IRA.Value & 0b11111100_00000000_00000000_00000000) >> 26);
            try { _opCode = INSTRUCTION_SIXBIT_OPCODE_DICT[opCodeBits]; }
            catch (KeyNotFoundException)
            {
                Console.Error.WriteLine($"Invalid OpCode {opCodeBits:X2}.");
                throw;
            }
            Console.Write($"OpCode: {opCodeBits:X2}->{_opCode};");

            // Decode conditional bits into conditional type
            byte conditionalBits = Convert.ToByte((IRA.Value & 0b00000011_11000000_00000000_00000000) >> 22);
            try { _conditional = INSTRUCTION_FOURBIT_CONDITIONAL_DICT[conditionalBits]; }
            catch (KeyNotFoundException)
            {
                Console.Error.WriteLine($"Invalid conditional {conditionalBits:X2}.");
                throw;
            }
            Console.WriteLine($" Cond: {conditionalBits:X2}->{_conditional}");
        }

        private void InstructionExecute()
        {
            if (_conditional != Condition.ALWAYS)
            {
                Console.Error.WriteLine("Conditionals not implemented yet. Exiting early.");
                return;
            }

            if (_opCode == OpCode.NOP)
            {
                Console.WriteLine("NOP");
            }
            else if (_opCode == OpCode.JMP)
            {
                Console.WriteLine($"Jump to address {IRB.Value}");
                PC.Value = IRB.Value;
            }
            else if (_opCode == OpCode.B)
            {
                Console.WriteLine($"Branch to address {IRB.Value}");
                Console.Error.WriteLine("Conditionals not implemented yet. Exiting early.");
                return;
            }
            else if (_opCode == OpCode.MOV)
            {
                Console.WriteLine("Copy regN to regN");
                Console.Error.WriteLine("Not implemented yet. Exiting early.");
                return;
            }
            else if (_opCode == OpCode.LOAD)
            {
                Console.WriteLine("Load memory address to regN");
                Console.Error.WriteLine("Not implemented yet. Exiting early.");
                return;
            }
        }
    }
}