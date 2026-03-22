using static TinyProc.Processor.CPU.CPU;

namespace TinyProc.Processor;

public sealed partial class Instructions
{
    // This file covers the structure of processor executable instructions, without defining
    // the instruction set of the LLTP/x25-32 architecture.

    private const uint BITMASK_OPCODE      = 0b11111100_00000000_00000000_00000000;
    private const uint BITMASK_CONDITIONAL = 0b00000011_11000000_00000000_00000000;
    private const uint BITMASK_ADRMODE     = 0b00000000_00000000_00000000_00000010;
    private const uint BITMASK_EXTENSION   = 0b00000000_00000000_00000000_00000001;

    // R-Type specific
    private const uint BITMASK_R_RDEST     = 0b00000000_00111110_00000000_00000000;
    private const uint BITMASK_R_RSOURCE   = 0b00000000_00000001_11110000_00000000;
    private const uint BITMASK_R_ALUOP     = 0b00000000_00000000_00001111_11000000;

    // I-Type specific
    private const uint BITMASK_I_RDEST     = 0b00000000_00111110_00000000_00000000;
    private const uint BITMASK_I_ALUOP     = 0b00000000_00000001_11111000_00000000;
        
    // J-Type specific
    // None

    public enum InstructionType
    {
        Register = 0,
        Immediate = 1,
        Jump = 2
    }
    public enum AddressingMode
    {
        Absolute = 0,
        PCRelative = 1
    }

    private static uint ExtractWithBitmaskAndShiftRight(uint original, uint bitmask)
    {
        int shiftRightAmount = 0;
        while (((bitmask >> shiftRightAmount) & 0x1) == 0b0)
            shiftRightAmount++;

        return (original & bitmask) >> shiftRightAmount;
    }

    public static InstructionType DetermineInstructionType(uint lowWord)
    {
        Opcode opcode = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_OPCODE);
        if      (opcode == Opcode.NOP)   { return InstructionType.Jump; }
        else if (opcode == Opcode.JMP)   { return InstructionType.Jump; }
        else if (opcode == Opcode.JMPR)  { return InstructionType.Register; }
        else if (opcode == Opcode.CALL)  { return InstructionType.Jump; }
        else if (opcode == Opcode.CALLR) { return InstructionType.Register; }
        else if (opcode == Opcode.RET)   { return InstructionType.Jump; }
        else if (opcode == Opcode.INT)   { return InstructionType.Jump; }
        else if (opcode == Opcode.IRET)  { return InstructionType.Jump; }
        else if (opcode == Opcode.AOPI)  { return InstructionType.Immediate; }
        else if (opcode == Opcode.AOPR)  { return InstructionType.Register; }
        else if (opcode == Opcode.TST)   { return InstructionType.Register; }
        else if (opcode == Opcode.CLC)   { return InstructionType.Register; }
        else if (opcode == Opcode.CLZ)   { return InstructionType.Register; }
        else if (opcode == Opcode.CLOF)  { return InstructionType.Register; }
        else if (opcode == Opcode.CLNG)  { return InstructionType.Register; }
        else if (opcode == Opcode.CLA)   { return InstructionType.Register; }
        else if (opcode == Opcode.LD)    { return InstructionType.Immediate; }
        else if (opcode == Opcode.LDR)   { return InstructionType.Register; }
        else if (opcode == Opcode.STR)   { return InstructionType.Register; }
        else if (opcode == Opcode.ST)    { return InstructionType.Immediate; }
        else if (opcode == Opcode.PUSH)  { return InstructionType.Register; }
        else if (opcode == Opcode.POP)   { return InstructionType.Register; }
        
        throw new NotImplementedException($"Instruction opcode {opcode:x8} not linked to instruction type (R/I/J).");
    }
    public static InstructionType DetermineInstructionType(Opcode opcode)
    {
        return DetermineInstructionType(opcode << 26);
    }

    // TODO: Move these 2 methods into the ALU class and replace them with explicit operators.
    private static ALU.ALUOpcode GetALUOpcodeFromUInt(uint opcodeBits)
    {
        bool[] aluOpcodeBoolArray = new bool[6];
        for (int i = 0; i < 6; i++)
        {
            aluOpcodeBoolArray[5 - i] = ((opcodeBits >> i) & 0b1) == 0x1;
        }
        return new ALU.ALUOpcode((
            aluOpcodeBoolArray[0],
            aluOpcodeBoolArray[1],
            aluOpcodeBoolArray[2],
            aluOpcodeBoolArray[3],
            aluOpcodeBoolArray[4],
            aluOpcodeBoolArray[5]));
    }
    private static uint GetUIntFromALUOpcode(ALU.ALUOpcode aluOpcode)
    {
        uint aluOpcodeUInt = 0x0u;
        aluOpcodeUInt |= Convert.ToUInt32(aluOpcode.zx) << 5;
        aluOpcodeUInt |= Convert.ToUInt32(aluOpcode.nx) << 4;
        aluOpcodeUInt |= Convert.ToUInt32(aluOpcode.zy) << 3;
        aluOpcodeUInt |= Convert.ToUInt32(aluOpcode.ny) << 2;
        aluOpcodeUInt |= Convert.ToUInt32(aluOpcode.f)  << 1;
        aluOpcodeUInt |= Convert.ToUInt32(aluOpcode.no) << 0;
        return aluOpcodeUInt;
    }

    public interface IInstruction
    {
        public Opcode Opcode { get; }
        public Condition Conditional { get; }
        public AddressingMode AddressingMode { get; }
        public InstructionType InstructionType { get; }
        public bool Extension { get; }
        public (uint, uint) BinaryRepresentation
        {
            get
            {
                return InstructionType switch
                {
                    InstructionType.Register => ((uint, uint))(RegRegInstruction)this,
                    InstructionType.Immediate => ((uint, uint))(RegImmInstruction)this,
                    InstructionType.Jump => ((uint, uint))(JumpInstruction)this,
                    _ => throw new Exception("Cannot return instruction byte representation, since its type could not be determined.")
                };
            }
        }
        
        // Specify access interface to different instruction types via one interface.
        // This is done via access methods, whose default behavior is to throw an exception and only the appropriate
        // instruction types override these functions to return their respective values.

        // Register-Type only
        public AddressableRegisterCode R_AddressableDestRegCode
        { get => throw new Exception("This instruction is not a Reg-Reg-Type instruction; It has no R-Type destination register code."); }
        internal InternalRegisterCode R_DestRegCode { get => (InternalRegisterCode)(uint)R_AddressableDestRegCode; }
        public AddressableRegisterCode R_AddressableSrcRegCode
        { get => throw new Exception("This instruction is not a Reg-Reg-Type instruction; It has no R-Type source register code."); }
        internal InternalRegisterCode R_SrcRegCode { get => (InternalRegisterCode)(uint)R_AddressableSrcRegCode; }
        public ALU.ALUOpcode R_ALUOpcode
        { get => throw new Exception("This instruction is not a Reg-Reg-Type instruction; It has no R-Type ALU Opcode."); }

        // Immediate-Type only
        public AddressableRegisterCode I_AddressableDestRegCode
        { get => throw new Exception("This instruction is not a Reg-Imm-Type instruction; It has no I-Type destination register code."); }
        internal InternalRegisterCode I_DestRegCode { get => (InternalRegisterCode)(uint)I_AddressableDestRegCode; }
        public ALU.ALUOpcode I_ALUOpcode
        { get => throw new Exception("This instruction is not a Reg-Imm-Type instruction; It has no I-Type ALU Opcode."); }
        public uint I_ImmediateValue
        { get => throw new Exception("This instruction is not a Reg-Imm-Type instruction; It has no I-Type immediate value."); }

        // Jump-Type only
        public uint J_JumpTargetAddress
        { get => throw new Exception("This instruction is not a Jump-Type instruction; It has no J-Type target jump address."); }
    }

    // Register type instruction
    public readonly struct RegRegInstruction(Opcode opcode, Condition conditional,
        AddressableRegisterCode destRegCode, AddressableRegisterCode srcRegCode,
        ALU.ALUOpcode aluOpcode, AddressingMode adrMode) : IInstruction
    {
        public readonly Opcode Opcode { get; } = opcode;
        public readonly Condition Conditional { get; } = conditional;
        public readonly AddressingMode AddressingMode { get; } = adrMode;
        public readonly bool Extension { get; } = true;

        public readonly InstructionType InstructionType { get; } = InstructionType.Register;
        public readonly AddressableRegisterCode R_AddressableDestRegCode { get; } = destRegCode;
        public readonly AddressableRegisterCode R_AddressableSrcRegCode { get; } = srcRegCode;
        public readonly ALU.ALUOpcode R_ALUOpcode { get; } = aluOpcode;
        public readonly AddressingMode R_AddressingMode { get; } = adrMode;

        // Explicit cast from binary instruction tuple to RegRegInstruction
        public static explicit operator RegRegInstruction((uint, uint) instructionTuple)
        {
            uint lowWord = instructionTuple.Item1;
            uint highWord = instructionTuple.Item2;
            Opcode opcode = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_OPCODE);
            Condition conditional = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_CONDITIONAL);
            AddressableRegisterCode destRegCode = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_R_RDEST);
            AddressableRegisterCode srcRegCode = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_R_RSOURCE);
            ALU.ALUOpcode aluOpcode = GetALUOpcodeFromUInt(ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_R_ALUOP));
            AddressingMode adrMode = (AddressingMode)ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_ADRMODE);
            return new RegRegInstruction(opcode, conditional, destRegCode, srcRegCode, aluOpcode, adrMode);
        }
        // Explicit cast from RegRegInstruction to binary instruction tuple
        public static explicit operator (uint, uint)(RegRegInstruction instruction)
        {
            uint lowWord = 0x0u;
            uint highWord = 0x0u;
            lowWord |= instruction.Opcode <<      (32 - 6);
            lowWord |= instruction.Conditional << (32 - 6 - 4);
            lowWord |= instruction.R_AddressableDestRegCode << (32 - 6 - 4 - 5);
            lowWord |= instruction.R_AddressableSrcRegCode <<  (32 - 6 - 4 - 5 - 5);
            lowWord |= GetUIntFromALUOpcode(instruction.R_ALUOpcode) << (32 - 6 - 4 - 5 - 5 - 6);
            lowWord |= (uint)instruction.R_AddressingMode << (32 - 6 - 4 - 5 - 5 - 6 - 4 - 1);
            lowWord |= 0x1u << (32 - 6 - 4 - 5 - 5 - 6 - 4 - 1 - 1);
            return (lowWord, highWord);
        }
    }

    // Immediate type instruction
    public readonly struct RegImmInstruction(Opcode opcode, Condition conditional,
        AddressableRegisterCode destRegCode, ALU.ALUOpcode aluOpcode, AddressingMode adrMode, uint immediate) : IInstruction
    {
        public readonly Opcode Opcode { get; } = opcode;
        public readonly Condition Conditional { get; } = conditional;
        public readonly AddressingMode AddressingMode { get; } = adrMode;
        public readonly bool Extension { get; } = true;

        public readonly InstructionType InstructionType { get; } = InstructionType.Immediate;
        public readonly AddressableRegisterCode I_AddressableDestRegCode { get; } = destRegCode;
        public readonly ALU.ALUOpcode I_ALUOpcode { get; } = aluOpcode;
        public readonly uint I_ImmediateValue { get; } = immediate;

        // Explicit cast from binary instruction tuple to RegImmInstruction
        public static explicit operator RegImmInstruction((uint, uint) instructionTuple)
        {
            uint lowWord = instructionTuple.Item1;
            uint highWord = instructionTuple.Item2;
            Opcode opcode = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_OPCODE);
            Condition conditional = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_CONDITIONAL);
            AddressableRegisterCode destRegCode = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_I_RDEST);
            ALU.ALUOpcode aluOpcode = GetALUOpcodeFromUInt(ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_I_ALUOP));
            AddressingMode adrMode = (AddressingMode)ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_ADRMODE);
            uint immediateValue = highWord;
            return new RegImmInstruction(opcode, conditional, destRegCode, aluOpcode, adrMode, immediateValue);
        }
        // Explicit cast from RegImmInstruction to binary instruction tuple
        public static explicit operator (uint, uint)(RegImmInstruction instruction)
        {
            uint lowWord = 0x0u;
            uint highWord = 0x0u;
            lowWord |= instruction.Opcode <<      (32 - 6);
            lowWord |= instruction.Conditional << (32 - 6 - 4);
            lowWord |= instruction.I_AddressableDestRegCode << (32 - 6 - 4 - 5);
            lowWord |= GetUIntFromALUOpcode(instruction.I_ALUOpcode) << (32 - 6 - 4 - 5 - 6);
            lowWord |= (uint)instruction.AddressingMode << (32 - 6 - 4 - 5 - 6 - 9 - 1);
            lowWord |= 0x1u << (32 - 6 - 4 - 5 - 6 - 9 - 1 - 1);
            highWord = instruction.I_ImmediateValue;
            return (lowWord, highWord);
        }
    }

    // Jump type instruction
    public readonly struct JumpInstruction(Opcode opcode, Condition conditional, AddressingMode adrMode, uint jumpTargetAddress) : IInstruction
    {
        public readonly Opcode Opcode { get; } = opcode;
        public readonly Condition Conditional { get; } = conditional;
        public readonly AddressingMode AddressingMode { get; } = adrMode;
        public readonly bool Extension { get; } = true;

        public readonly InstructionType InstructionType { get; } = InstructionType.Jump;
        public readonly uint J_JumpTargetAddress { get; } = jumpTargetAddress;
        public readonly AddressingMode J_AddressingMode { get; } = adrMode;

        // Explicit cast from binary instruction tuple to JumpInstruction
        public static explicit operator JumpInstruction((uint, uint) instructionTuple)
        {
            uint lowWord = instructionTuple.Item1;
            uint highWord = instructionTuple.Item2;
            Opcode opcode = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_OPCODE);
            Condition conditional = ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_CONDITIONAL);
            AddressingMode adrMode = (AddressingMode)ExtractWithBitmaskAndShiftRight(lowWord, BITMASK_ADRMODE);
            uint jumpTargetAddress = highWord;
            return new JumpInstruction(opcode, conditional, adrMode, jumpTargetAddress);
        }
        // Explicit cast from JumpInstruction to binary instruction tuple
        public static explicit operator (uint, uint)(JumpInstruction instruction)
        {
            uint lowWord = 0x0u;
            uint highWord = 0x0u;
            lowWord |= instruction.Opcode <<      (32 - 6);
            lowWord |= instruction.Conditional << (32 - 6 - 4);
            lowWord |= (uint)instruction.J_AddressingMode << (32 - 6 - 4 - 20 - 1);
            lowWord |= 0x1u << (32 - 6 - 4 - 20 - 1 - 1);
            highWord = instruction.J_JumpTargetAddress;
            return (lowWord, highWord);
        }
    }
}