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
        else if (opcode == Opcode.JMP)   { return InstructionType.Jump; }
        else if (opcode == Opcode.B)     { return InstructionType.Jump; }
        else if (opcode == Opcode.AOPI)  { return InstructionType.Immediate; }
        else if (opcode == Opcode.AOPR)  { return InstructionType.Register; }
        else if (opcode == Opcode.CLZ)   { return InstructionType.Register; }
        else if (opcode == Opcode.CLOF)  { return InstructionType.Register; }
        else if (opcode == Opcode.CLNG)  { return InstructionType.Register; }
        else if (opcode == Opcode.LOAD)  { return InstructionType.Immediate; }
        else if (opcode == Opcode.LOADR) { return InstructionType.Register; }
        else if (opcode == Opcode.STORE) { return InstructionType.Immediate; }
        else if (opcode == Opcode.STORR) { return InstructionType.Register; }
        throw new NotImplementedException($"Instruction opcode {opcode:x8} not linked to instruction type (R/I/J).");
    }
    public static InstructionType DetermineInstructionType(Opcode opcode)
    {
        return DetermineInstructionType(opcode << 26);
    }

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
        public Opcode GetOpcode();
        public Condition GetConditional();
        public InstructionType GetInstructionType();
        
        // Register-Type only
        public AddressableRegisterCode R_GetAddressableDestRegCode()
        { throw new Exception("This instruction is not a Reg-Reg-Type instruction; It has no R-Type destination register code."); }
        internal InternalRegisterCode R_GetDestRegCode() => (InternalRegisterCode)(uint)R_GetAddressableDestRegCode();
        public AddressableRegisterCode R_GetAddressableSrcRegCode()
        { throw new Exception("This instruction is not a Reg-Reg-Type instruction; It has no R-Type source register code."); }
        internal InternalRegisterCode R_GetSrcRegCode() => (InternalRegisterCode)(uint)R_GetAddressableSrcRegCode();
        public ALU.ALUOpcode R_GetALUOpcode()
        { throw new Exception("This instruction is not a Reg-Reg-Type instruction; It has no R-Type ALU Opcode."); }

        // Immediate-Type only
        public AddressableRegisterCode I_GetAddressableDestRegCode()
        { throw new Exception("This instruction is not a Reg-Imm-Type instruction; It has no I-Type destination register code."); }
        internal InternalRegisterCode I_GetDestRegCode() => (InternalRegisterCode)(uint)I_GetAddressableDestRegCode();
        public ALU.ALUOpcode I_GetALUOpcode()
        { throw new Exception("This instruction is not a Reg-Imm-Type instruction; It has no I-Type ALU Opcode."); }
        public uint I_GetImmediateValue()
        { throw new Exception("This instruction is not a Reg-Imm-Type instruction; It has no I-Type immediate value."); }

        // Jump-Type only
        public uint J_GetJumpTargetAddress()
        { throw new Exception("This instruction is not a Jump-Type instruction; It has no J-Type target jump address."); }
    }

    // Register type instruction
    public readonly struct RegRegInstruction(Opcode opcode, Condition conditional,
        AddressableRegisterCode destRegCode, AddressableRegisterCode srcRegCode,
        ALU.ALUOpcode aluOpcode) : IInstruction
    {
        public readonly Opcode Opcode = opcode;
        public readonly Condition Conditional = conditional;
        public readonly AddressableRegisterCode DestRegCode = destRegCode;
        public readonly AddressableRegisterCode SrcRegCode = srcRegCode;
        public readonly ALU.ALUOpcode ALUOpcode = aluOpcode;

        public InstructionType GetInstructionType() => InstructionType.Register;
        public Opcode GetOpcode() => Opcode;
        public Condition GetConditional() => Conditional;
        public AddressableRegisterCode R_GetAddressableDestRegCode() => DestRegCode;
        public AddressableRegisterCode R_GetAddressableSrcRegCode() => SrcRegCode;
        public ALU.ALUOpcode R_GetALUOpcode() => ALUOpcode;

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
            lowBytes |= instruction.DestRegCode << (32 - 6 - 4 - 5);
            lowBytes |= instruction.SrcRegCode <<  (32 - 6 - 4 - 5 - 5);
            lowBytes |= GetUIntFromALUOpcode(instruction.ALUOpcode) << (32 - 6 - 4 - 5 - 5 - 6);
            return (lowBytes, highBytes);
        }
    }

    // Immediate type instruction
    public readonly struct RegImmInstruction(Opcode opcode, Condition conditional,
        AddressableRegisterCode destRegCode, ALU.ALUOpcode aluOpcode, uint immediate) : IInstruction
    {
        public readonly Opcode Opcode = opcode;
        public readonly Condition Conditional = conditional;
        public readonly AddressableRegisterCode DestRegCode = destRegCode;
        public readonly ALU.ALUOpcode ALUOpcode = aluOpcode;
        public readonly uint Immediate = immediate;

        public InstructionType GetInstructionType() => InstructionType.Immediate;
        public Opcode GetOpcode() => Opcode;
        public Condition GetConditional() => Conditional;
        public AddressableRegisterCode I_GetAddressableDestRegCode() => DestRegCode;
        public ALU.ALUOpcode I_GetALUOpcode() => ALUOpcode;
        public uint I_GetImmediateValue() => Immediate;

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
            lowBytes |= instruction.DestRegCode << (32 - 6 - 4 - 5);
            lowBytes |= GetUIntFromALUOpcode(instruction.ALUOpcode) << (32 - 6 - 4 - 5 - 6);
            highBytes = instruction.Immediate;
            return (lowBytes, highBytes);
        }
    }

    // Jump type instruction
    public readonly struct JumpInstruction(Opcode opcode, Condition conditional, uint jumpTargetAddress) : IInstruction
    {
        public readonly Opcode Opcode = opcode;
        public readonly Condition Conditional = conditional;
        public readonly uint JumpTargetAddress = jumpTargetAddress;

        public InstructionType GetInstructionType() => InstructionType.Jump;
        public Opcode GetOpcode() => Opcode;
        public Condition GetConditional() => Conditional;
        public uint J_GetJumpTargetAddress() => JumpTargetAddress;

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
            highBytes = instruction.JumpTargetAddress;
            return (lowBytes, highBytes);
        }
    }
}