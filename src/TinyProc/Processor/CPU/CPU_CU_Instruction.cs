using System.ComponentModel.DataAnnotations;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        private const uint BITMASK_OPCODE = 0b11111100_00000000_00000000_00000000;
        private const uint BITMASK_CONDITIONAL = 0b00000011_11000000_00000000_00000000;

        private static InstructionTypeR ParseInstructionAsRType(uint lowBytes, uint highBytes)
        {
            return new InstructionTypeR();
        }

        // Register type instruction
        public class InstructionTypeR(OpCode opCode, Condition conditional,
            AddressableRegisterCode destinationRegisterCode, AddressableRegisterCode sourceRegisterCode,
            ALU.ALU_OpCode alu_OpCode)
        {
            readonly OpCode OpCode = opCode;
            readonly Condition Conditional = conditional;
            readonly AddressableRegisterCode DestinationRegisterCode = destinationRegisterCode;
            readonly AddressableRegisterCode SourceRegisterCode = sourceRegisterCode;
            readonly ALU.ALU_OpCode ALUOpCode = alu_OpCode;
        }

        // Immediate type instruction
        public class InstructionTypeI
        {
            readonly OpCode OpCode;
            readonly Condition Conditional;
            readonly AddressableRegisterCode DestinationRegisterCode;
            readonly uint Immediate;
        }

        // Jump type instruction
        public class InstructionTypeJ
        {
            readonly OpCode OpCode;
            readonly Condition Conditional;
            readonly uint JumpAddress;
        }
    }
}