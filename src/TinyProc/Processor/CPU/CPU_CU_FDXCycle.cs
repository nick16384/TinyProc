namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Load first instruction word
        private void InstructionFetch1()
        {
            Console.WriteLine($"PC at {PC.ValueDirect:X8}");
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
            // PC -> MAR
            _IntBus1Src.BusSourceRegisterAddress = RCODE_PC;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MAR;

            // MDR -> IRA
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferB];
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_MDR;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_IRA;

            // Set void as target to not mess up IRA values in Fetch2
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_VOID;
        }
        // Load second instruction word
        private void InstructionFetch2()
        {
            // PC + 1 -> MAR
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_PC;
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_CV_P1;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_MAR;

            // MDR -> IRB
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_VOID;
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferB];
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_MDR;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_IRB;

            _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_VOID;
            Console.WriteLine($"Loaded 2 instruction words: {IRA.ValueDirect:X8} {IRB.ValueDirect:X8}");
        }

        // Essentially prepares the Control Unit for the execute stage
        private void InstructionDecode()
        {
            // PC + 2 -> PC (Increment PC to next instruction)
            _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
            _IntBus1Src.BusSourceRegisterAddress = RCODE_PC;
            _IntBus2Src.BusSourceRegisterAddress = RCODE_SPECIAL_CV_P2;
            _IntBus3Dst.BusTargetRegisterAddress = RCODE_PC;

            // Determine instruction type (R/I/J)
            uint opCodeBits = Convert.ToUInt32((IRA.ValueDirect & BITMASK_OPCODE) >> 26);
            OpCode opCode;
            try { opCode = (OpCode)opCodeBits; }
            catch (KeyNotFoundException)
            {
                Console.Error.WriteLine($"Invalid OpCode {opCodeBits:X2}.");
                throw;
            }
            switch (opCode)
            {
                case OpCode.NOP: 
            }

            // Decode OpCode bits to OpCode type
            
            
            Console.Write($"OpCode: {opCodeBits:X2}->{_opCode};");

            // Decode conditional bits into conditional type
            uint conditionalBits = Convert.ToUInt32((IRA.ValueDirect & BITMASK_CONDITIONAL) >> 22);
            try { _conditional = (Condition)conditionalBits; }
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
                Console.WriteLine($"Jump to address {IRB.ValueDirect}");
                _alu.OpCode = ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA];
                _IntBus1Src.BusSourceRegisterAddress = RCODE_SPECIAL_IRB;
                _IntBus3Dst.BusTargetRegisterAddress = RCODE_PC;
                _IntBus3Dst.BusTargetRegisterAddress = RCODE_SPECIAL_VOID;
            }
            else if (_opCode == OpCode.B)
            {
                Console.WriteLine($"Branch to address {IRB.ValueDirect}");
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