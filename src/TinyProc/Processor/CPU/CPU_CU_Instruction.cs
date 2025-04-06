using System.ComponentModel.DataAnnotations;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        private const uint BITMASK_OPCODE      = 0b11111100_00000000_00000000_00000000;
        private const uint BITMASK_CONDITIONAL = 0b00000011_11000000_00000000_00000000;

        // R-Type specific
        private const uint BITMASK_R_RDEST     = 0b00000000_00111110_00000000_00000000;
        private const uint BITMASK_R_RSOURCE   = 0b00000000_00000001_11110000_00000000;
        private const uint BITMASK_R_ALUOP     = 0b00000000_00000000_00001111_11000000;

        // I-Type specific
        private const uint BITMASK_I_RDEST     = 0b00000000_00111110_00000000_00000000;
        private const uint BITMASK_I_ALUOP     = 0b00000000_00000001_11111000_00000000;
        
        // J-Type specific
        // Doesn't need bitmasks for low bytes (Only paremeter is address in high byte)

        enum InstructionType
        {
            Register,
            Immediate,
            Jump
        }

        private static uint ExtractWithBitmaskAndShiftRight(uint original, uint bitmask)
        {
            int shiftRightAmount = 0;
            while (((bitmask >> shiftRightAmount) & 0x1) == 0b0)
                shiftRightAmount++;

            return (original & bitmask) >> shiftRightAmount;
        }

        private static ALU.ALU_OpCode GetALUOpCodeFromUInt(uint opCodeBits)
        {
            bool[] aluOpCodeBoolArray = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                aluOpCodeBoolArray[5 - i] = ((opCodeBits >> i) & 0b1) == 0x1;
            }
            return new ALU.ALU_OpCode(
                aluOpCodeBoolArray[0],
                aluOpCodeBoolArray[1],
                aluOpCodeBoolArray[2],
                aluOpCodeBoolArray[3],
                aluOpCodeBoolArray[4],
                aluOpCodeBoolArray[5]);
        }

        private static InstructionType DetermineInstructionType(uint lowBytes)
        {
            OpCode opCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
            if      (opCode == OpCode.NOP)    { return InstructionType.Jump; }
            else if (opCode == OpCode.JMP )   { return InstructionType.Jump; }
            else if (opCode == OpCode.B )     { return InstructionType.Jump; }
            else if (opCode == OpCode.MOV )   { return InstructionType.Register; }
            else if (opCode == OpCode.CLZ )   { return InstructionType.Register; }
            else if (opCode == OpCode.CLOF )  { return InstructionType.Register; }
            else if (opCode == OpCode.CLNG )  { return InstructionType.Register; }
            else if (opCode == OpCode.ADD )   { return InstructionType.Immediate; }
            else if (opCode == OpCode.ADDR )  { return InstructionType.Register; }
            else if (opCode == OpCode.SUB )   { return InstructionType.Immediate; }
            else if (opCode == OpCode.SUBR )  { return InstructionType.Register; }
            else if (opCode == OpCode.MUL )   { return InstructionType.Immediate; }
            else if (opCode == OpCode.MULR )  { return InstructionType.Register; }
            else if (opCode == OpCode.AND )   { return InstructionType.Immediate; }
            else if (opCode == OpCode.ANDR )  { return InstructionType.Register; }
            else if (opCode == OpCode.OR )    { return InstructionType.Immediate; }
            else if (opCode == OpCode.ORR )   { return InstructionType.Register; }
            else if (opCode == OpCode.XOR )   { return InstructionType.Immediate; }
            else if (opCode == OpCode.XORR )  { return InstructionType.Register; }
            else if (opCode == OpCode.LS )    { return InstructionType.Immediate; }
            else if (opCode == OpCode.LSR )   { return InstructionType.Register; }
            else if (opCode == OpCode.RS )    { return InstructionType.Immediate; }
            else if (opCode == OpCode.RSR )   { return InstructionType.Register; }
            else if (opCode == OpCode.SRS )   { return InstructionType.Immediate; }
            else if (opCode == OpCode.SRSR )  { return InstructionType.Register; }
            else if (opCode == OpCode.ROL )   { return InstructionType.Immediate; }
            else if (opCode == OpCode.ROLR )  { return InstructionType.Register; }
            else if (opCode == OpCode.ROR )   { return InstructionType.Immediate; }
            else if (opCode == OpCode.RORR )  { return InstructionType.Register; }
            else if (opCode == OpCode.LOAD )  { return InstructionType.Immediate; }
            else if (opCode == OpCode.LOADR ) { return InstructionType.Register; }
            else if (opCode == OpCode.STORE ) { return InstructionType.Immediate; }
            else if (opCode == OpCode.STORR ) { return InstructionType.Register; }
            throw new NotImplementedException($"Instruction opcode {opCode:X8} not linked to instruction type (R/I/J).");
        }

        // Register type instruction
        public readonly struct InstructionTypeR(OpCode opCode, Condition conditional,
            AddressableRegisterCode destRegCode, AddressableRegisterCode srcRegCode,
            ALU.ALU_OpCode aluOpCode)
        {
            public readonly OpCode OpCode = opCode;
            public readonly Condition Conditional = conditional;
            public readonly AddressableRegisterCode DestRegCode = destRegCode;
            public readonly AddressableRegisterCode SrcRegCode = srcRegCode;
            public readonly ALU.ALU_OpCode ALUOpCode = aluOpCode;
        }
        private static InstructionTypeR ParseInstructionAsRType(uint lowBytes, uint highBytes)
        {
            OpCode opCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
            Condition conditional = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_CONDITIONAL);
            AddressableRegisterCode destRegCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_R_RDEST);
            AddressableRegisterCode srcRegCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_R_RSOURCE);
            ALU.ALU_OpCode aluOpCode = GetALUOpCodeFromUInt(ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_R_ALUOP));
            return new InstructionTypeR(opCode, conditional, destRegCode, srcRegCode, aluOpCode);
        }

        // Immediate type instruction
        public readonly struct InstructionTypeI(OpCode opCode, Condition conditional,
            AddressableRegisterCode destRegCode, ALU.ALU_OpCode aluOpCode, uint immediate)
        {
            public readonly OpCode OpCode = opCode;
            public readonly Condition Conditional = conditional;
            public readonly AddressableRegisterCode DestRegCode = destRegCode;
            public readonly ALU.ALU_OpCode ALUOpCode = aluOpCode;
            public readonly uint Immediate = immediate;
        }
        private static InstructionTypeI ParseInstructionAsIType(uint lowBytes, uint highBytes)
        {
            OpCode opCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
            Condition conditional = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_CONDITIONAL);
            AddressableRegisterCode destRegCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_I_RDEST);
            ALU.ALU_OpCode aluOpCode = GetALUOpCodeFromUInt(ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_I_ALUOP));
            uint immediate = highBytes;
            return new InstructionTypeI(opCode, conditional, destRegCode, aluOpCode, immediate);
        }

        // Jump type instruction
        public readonly struct InstructionTypeJ(OpCode opCode, Condition conditional, uint jumpTargetAddress)
        {
            public readonly OpCode OpCode = opCode;
            public readonly Condition Conditional = conditional;
            public readonly uint JumpTargetAddress = jumpTargetAddress;
        }
        private static InstructionTypeJ ParseInstructionAsJType(uint lowBytes, uint highBytes)
        {
            OpCode opCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
            Condition conditional = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_CONDITIONAL);
            uint jumpTargetAddress = highBytes;
            return new InstructionTypeJ(opCode, conditional, jumpTargetAddress);
        }
    }
}