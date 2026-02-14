using static TinyProc.Processor.CPU.CPU;

namespace TinyProc.Processor;

public sealed partial class Instructions
{
    // This file covers the structure of processor executable instructions, without defining
    // the instruction set of the LLTP/x25-32 architecture.

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

    public enum InstructionType
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

    public static InstructionType DetermineInstructionType(uint lowBytes)
    {
        Opcode opcode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
        if      (opcode == Opcode.NOP)   { return InstructionType.Jump; }
        else if (opcode == Opcode.AJMP)  { return InstructionType.Jump; }
        else if (opcode == Opcode.JMP)   { return InstructionType.Jump; }
        else if (opcode == Opcode.ACALL) { return InstructionType.Jump; }
        else if (opcode == Opcode.CALL)  { return InstructionType.Jump; }
        else if (opcode == Opcode.ACALR) { return InstructionType.Register; }
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
        else if (opcode == Opcode.ALD)   { return InstructionType.Immediate; }
        else if (opcode == Opcode.LD)    { return InstructionType.Immediate; }
        else if (opcode == Opcode.ALDR)  { return InstructionType.Register; }
        else if (opcode == Opcode.LDR)   { return InstructionType.Register; }
        else if (opcode == Opcode.ASTR)  { return InstructionType.Immediate; }
        else if (opcode == Opcode.STR)   { return InstructionType.Immediate; }
        else if (opcode == Opcode.ASTRR) { return InstructionType.Register; }
        else if (opcode == Opcode.STRR)  { return InstructionType.Register; }
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
        public InstructionType InstructionType { get; }
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
        ALU.ALUOpcode aluOpcode) : IInstruction
    {
        public readonly Opcode Opcode { get; } = opcode;
        public readonly Condition Conditional { get; } = conditional;

        public readonly InstructionType InstructionType { get; } = InstructionType.Register;
        public readonly AddressableRegisterCode R_AddressableDestRegCode { get; } = destRegCode;
        public readonly AddressableRegisterCode R_AddressableSrcRegCode { get; } = srcRegCode;
        public readonly ALU.ALUOpcode R_ALUOpcode { get; } = aluOpcode;

        // Explicit cast from binary instruction tuple to RegRegInstruction
        public static explicit operator RegRegInstruction((uint, uint) instructionTuple)
        {
            uint lowBytes = instructionTuple.Item1;
            uint highBytes = instructionTuple.Item2;
            Opcode opcode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
            Condition conditional = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_CONDITIONAL);
            AddressableRegisterCode destRegCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_R_RDEST);
            AddressableRegisterCode srcRegCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_R_RSOURCE);
            ALU.ALUOpcode aluOpcode = GetALUOpcodeFromUInt(ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_R_ALUOP));
            return new RegRegInstruction(opcode, conditional, destRegCode, srcRegCode, aluOpcode);
        }
        // Explicit cast from RegRegInstruction to binary instruction tuple
        public static explicit operator (uint, uint)(RegRegInstruction instruction)
        {
            uint lowBytes = 0x0u;
            uint highBytes = 0x0u;
            lowBytes |= instruction.Opcode <<      (32 - 6);
            lowBytes |= instruction.Conditional << (32 - 6 - 4);
            lowBytes |= instruction.R_AddressableDestRegCode << (32 - 6 - 4 - 5);
            lowBytes |= instruction.R_AddressableSrcRegCode <<  (32 - 6 - 4 - 5 - 5);
            lowBytes |= GetUIntFromALUOpcode(instruction.R_ALUOpcode) << (32 - 6 - 4 - 5 - 5 - 6);
            return (lowBytes, highBytes);
        }
    }

    // Immediate type instruction
    public readonly struct RegImmInstruction(Opcode opcode, Condition conditional,
        AddressableRegisterCode destRegCode, ALU.ALUOpcode aluOpcode, uint immediate) : IInstruction
    {
        public readonly Opcode Opcode { get; } = opcode;
        public readonly Condition Conditional { get; } = conditional;

        public readonly InstructionType InstructionType { get; } = InstructionType.Immediate;
        public readonly AddressableRegisterCode I_AddressableDestRegCode { get; } = destRegCode;
        public readonly ALU.ALUOpcode I_ALUOpcode { get; } = aluOpcode;
        public readonly uint I_ImmediateValue { get; } = immediate;

        // Explicit cast from binary instruction tuple to RegImmInstruction
        public static explicit operator RegImmInstruction((uint, uint) instructionTuple)
        {
            uint lowBytes = instructionTuple.Item1;
            uint highBytes = instructionTuple.Item2;
            Opcode opcode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
            Condition conditional = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_CONDITIONAL);
            AddressableRegisterCode destRegCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_I_RDEST);
            ALU.ALUOpcode aluOpcode = GetALUOpcodeFromUInt(ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_I_ALUOP));
            uint immediateValue = highBytes;
            return new RegImmInstruction(opcode, conditional, destRegCode, aluOpcode, immediateValue);
        }
        // Explicit cast from RegImmInstruction to binary instruction tuple
        public static explicit operator (uint, uint)(RegImmInstruction instruction)
        {
            uint lowBytes = 0x0u;
            uint highBytes = 0x0u;
            lowBytes |= instruction.Opcode <<      (32 - 6);
            lowBytes |= instruction.Conditional << (32 - 6 - 4);
            lowBytes |= instruction.I_AddressableDestRegCode << (32 - 6 - 4 - 5);
            lowBytes |= GetUIntFromALUOpcode(instruction.I_ALUOpcode) << (32 - 6 - 4 - 5 - 6);
            highBytes = instruction.I_ImmediateValue;
            return (lowBytes, highBytes);
        }
    }

    // Jump type instruction
    public readonly struct JumpInstruction(Opcode opcode, Condition conditional, uint jumpTargetAddress) : IInstruction
    {
        public readonly Opcode Opcode { get; } = opcode;
        public readonly Condition Conditional { get; } = conditional;

        public readonly InstructionType InstructionType { get; } = InstructionType.Jump;
        public readonly uint J_JumpTargetAddress { get; } = jumpTargetAddress;

        // Explicit cast from binary instruction tuple to JumpInstruction
        public static explicit operator JumpInstruction((uint, uint) instructionTuple)
        {
            uint lowBytes = instructionTuple.Item1;
            uint highBytes = instructionTuple.Item2;
            Opcode opcode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
            Condition conditional = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_CONDITIONAL);
            uint jumpTargetAddress = highBytes;
            return new JumpInstruction(opcode, conditional, jumpTargetAddress);
        }
        // Explicit cast from JumpInstruction to binary instruction tuple
        public static explicit operator (uint, uint)(JumpInstruction instruction)
        {
            uint lowBytes = 0x0u;
            uint highBytes = 0x0u;
            lowBytes |= instruction.Opcode <<      (32 - 6);
            lowBytes |= instruction.Conditional << (32 - 6 - 4);
            highBytes = instruction.J_JumpTargetAddress;
            return (lowBytes, highBytes);
        }
    }
}