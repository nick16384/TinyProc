using static TinyProc.Processor.CPU.CPU;

namespace TinyProc.Processor;

public sealed class Instructions
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
        OpCode opCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
        if      (opCode == OpCode.NOP)   { return InstructionType.Jump; }
        else if (opCode == OpCode.JMP)   { return InstructionType.Jump; }
        else if (opCode == OpCode.B)     { return InstructionType.Jump; }
        else if (opCode == OpCode.MOV)   { return InstructionType.Register; }
        else if (opCode == OpCode.CLZ)   { return InstructionType.Register; }
        else if (opCode == OpCode.CLOF)  { return InstructionType.Register; }
        else if (opCode == OpCode.CLNG)  { return InstructionType.Register; }
        else if (opCode == OpCode.ADD)   { return InstructionType.Immediate; }
        else if (opCode == OpCode.ADDR)  { return InstructionType.Register; }
        else if (opCode == OpCode.SUB)   { return InstructionType.Immediate; }
        else if (opCode == OpCode.SUBR)  { return InstructionType.Register; }
        else if (opCode == OpCode.MUL)   { return InstructionType.Immediate; }
        else if (opCode == OpCode.MULR)  { return InstructionType.Register; }
        else if (opCode == OpCode.AND)   { return InstructionType.Immediate; }
        else if (opCode == OpCode.ANDR)  { return InstructionType.Register; }
        else if (opCode == OpCode.OR)    { return InstructionType.Immediate; }
        else if (opCode == OpCode.ORR)   { return InstructionType.Register; }
        else if (opCode == OpCode.XOR)   { return InstructionType.Immediate; }
        else if (opCode == OpCode.XORR)  { return InstructionType.Register; }
        else if (opCode == OpCode.LS)    { return InstructionType.Immediate; }
        else if (opCode == OpCode.LSR)   { return InstructionType.Register; }
        else if (opCode == OpCode.RS)    { return InstructionType.Immediate; }
        else if (opCode == OpCode.RSR)   { return InstructionType.Register; }
        else if (opCode == OpCode.SRS)   { return InstructionType.Immediate; }
        else if (opCode == OpCode.SRSR)  { return InstructionType.Register; }
        else if (opCode == OpCode.ROL)   { return InstructionType.Immediate; }
        else if (opCode == OpCode.ROLR)  { return InstructionType.Register; }
        else if (opCode == OpCode.ROR)   { return InstructionType.Immediate; }
        else if (opCode == OpCode.RORR)  { return InstructionType.Register; }
        else if (opCode == OpCode.LOAD)  { return InstructionType.Immediate; }
        else if (opCode == OpCode.LOADR) { return InstructionType.Register; }
        else if (opCode == OpCode.STORE) { return InstructionType.Immediate; }
        else if (opCode == OpCode.STORR) { return InstructionType.Register; }
        throw new NotImplementedException($"Instruction opcode {opCode:X8} not linked to instruction type (R/I/J).");
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
    private static uint GetUIntFromALUOpCode(ALU.ALU_OpCode aluOpCode)
    {
        uint aluOpCodeUInt = 0x0u;
        aluOpCodeUInt |= Convert.ToUInt32(aluOpCode.zx) << 5;
        aluOpCodeUInt |= Convert.ToUInt32(aluOpCode.nx) << 4;
        aluOpCodeUInt |= Convert.ToUInt32(aluOpCode.zy) << 3;
        aluOpCodeUInt |= Convert.ToUInt32(aluOpCode.ny) << 2;
        aluOpCodeUInt |= Convert.ToUInt32(aluOpCode.f)  << 1;
        aluOpCodeUInt |= Convert.ToUInt32(aluOpCode.no) << 0;
        return aluOpCodeUInt;
    }

    public static (uint, uint) ForgeBinaryInstruction(InstructionTypeR instruction)
    {
        uint lowBytes = 0x0u;
        uint highBytes = 0x0u;
        lowBytes |= instruction.OpCode <<      (32 - 6);
        lowBytes |= instruction.Conditional << (32 - 6 - 4);
        lowBytes |= instruction.DestRegCode << (32 - 6 - 4 - 5);
        lowBytes |= instruction.SrcRegCode <<  (32 - 6 - 4 - 5 - 5);
        lowBytes |= GetUIntFromALUOpCode(instruction.ALUOpCode) << (32 - 6 - 4 - 5 - 5 - 6);
        return (lowBytes, highBytes);
    }
    public static (uint, uint) ForgeBinaryInstruction(InstructionTypeI instruction)
    {
        uint lowBytes = 0x0u;
        uint highBytes = 0x0u;
        lowBytes |= instruction.OpCode <<      (32 - 6);
        lowBytes |= instruction.Conditional << (32 - 6 - 4);
        lowBytes |= instruction.DestRegCode << (32 - 6 - 4 - 5);
        lowBytes |= GetUIntFromALUOpCode(instruction.ALUOpCode) << (32 - 6 - 4 - 5 - 6);
        highBytes = instruction.Immediate;
        return (lowBytes, highBytes);
    }
    public static (uint, uint) ForgeBinaryInstruction(InstructionTypeJ instruction)
    {
        uint lowBytes = 0x0u;
        uint highBytes = 0x0u;
        lowBytes |= instruction.OpCode <<      (32 - 6);
        lowBytes |= instruction.Conditional << (32 - 6 - 4);
        highBytes = instruction.JumpTargetAddress;
        return (lowBytes, highBytes);
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
    public static InstructionTypeR ParseInstructionAsRType(uint lowBytes, uint highBytes)
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
    public static InstructionTypeI ParseInstructionAsIType(uint lowBytes, uint highBytes)
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
    public static InstructionTypeJ ParseInstructionAsJType(uint lowBytes, uint highBytes)
    {
        OpCode opCode = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_OPCODE);
        Condition conditional = ExtractWithBitmaskAndShiftRight(lowBytes, BITMASK_CONDITIONAL);
        uint jumpTargetAddress = highBytes;
        return new InstructionTypeJ(opCode, conditional, jumpTargetAddress);
    }

    // Class contains a lot of pseudo-enums, which act as enums externally,
    // but have implicit conversions implemented.

    public sealed class OpCode
    {
        private static readonly Dictionary<uint, OpCode> _values = [];

        public static readonly OpCode NOP   = new(0x00, "NOP");

        public static readonly OpCode JMP   = new(0x01, "JMP");
        public static readonly OpCode B     = new(0x02, "B");

        public static readonly OpCode MOV   = new(0x03, "MOV");
        public static readonly OpCode MOVR  = new(0x04, "MOVR");

        public static readonly OpCode CLZ   = new(0x07, "CLZ");
        public static readonly OpCode CLOF  = new(0x08, "CLOF");
        public static readonly OpCode CLNG  = new(0x09, "CLNG");

        public static readonly OpCode ADD   = new(0x10, "ADD");
        public static readonly OpCode ADDR  = new(0x11, "ADDR");
        public static readonly OpCode SUB   = new(0x12, "SUB");
        public static readonly OpCode SUBR  = new(0x13, "SUBR");
        public static readonly OpCode MUL   = new(0x14, "MUL");
        public static readonly OpCode MULR  = new(0x15, "MULR");
        public static readonly OpCode AND   = new(0x16, "AND");
        public static readonly OpCode ANDR  = new(0x17, "ANDR");
        public static readonly OpCode OR    = new(0x18, "OR");
        public static readonly OpCode ORR   = new(0x19, "ORR");
        public static readonly OpCode XOR   = new(0x1A, "XOR");
        public static readonly OpCode XORR  = new(0x1B, "XORR");
        public static readonly OpCode LS    = new(0x1C, "LS");
        public static readonly OpCode LSR   = new(0x1D, "LSR");
        public static readonly OpCode RS    = new(0x1E, "RS");
        public static readonly OpCode RSR   = new(0x1F, "RSR");
        public static readonly OpCode SRS   = new(0x20, "SRS");
        public static readonly OpCode SRSR  = new(0x21, "SRSR");
        public static readonly OpCode ROL   = new(0x22, "ROL");
        public static readonly OpCode ROLR  = new(0x23, "ROLR");
        public static readonly OpCode ROR   = new(0x24, "ROR");
        public static readonly OpCode RORR  = new(0x25, "RORR");

        public static readonly OpCode LOAD  = new(0x30, "LOAD");
        public static readonly OpCode LOADR = new(0x31, "LOADR");
        public static readonly OpCode STORE = new(0x32, "STORE");
        public static readonly OpCode STORR = new(0x33, "STORR");

        private readonly uint _value;
        private readonly string _name;
        private OpCode(uint value, string name)
        {
            _value = value;
            _name = name;
            _values.Add(value, this);
        }
        public static implicit operator OpCode(uint value)
        {
            try { return _values[value]; }
            catch (Exception) { throw new KeyNotFoundException($"Invalid OpCode {value:X8}"); }
        }
        public static explicit operator OpCode(string mnemonic)
        {
            foreach (OpCode opCode in _values.Values)
                if (opCode._name.Equals(mnemonic))
                    return opCode;
            throw new KeyNotFoundException($"Mnemonic {mnemonic} does not map to a valid OpCode.");
        }
        public static implicit operator uint(OpCode opCode) => opCode._value;

        public override string ToString() => _name;
    }

    public sealed class Condition
    {
        private static readonly Dictionary<uint, Condition> _values = [];

        public static readonly Condition ALWAYS = new(0x00, "ALWAYS");
        public static readonly Condition EQ     = new(0x01, "EQ");
        public static readonly Condition NE     = new(0x02, "NE");
        public static readonly Condition OF     = new(0x03, "OF");
        public static readonly Condition NO     = new(0x04, "NO");
        public static readonly Condition ZR     = new(0x05, "ZR");
        public static readonly Condition NZ     = new(0x06, "NZ");
        public static readonly Condition NG     = new(0x07, "NG");
        public static readonly Condition NN     = new(0x08, "NN");

        private readonly uint _value;
        private readonly string _name;
        private Condition(uint value, string name)
        {
            _value = value;
            _name = name;
            _values.Add(value, this);
        }
        public static implicit operator Condition(uint value)
        {
            try { return _values[value]; }
            catch (Exception) { throw new KeyNotFoundException($"Invalid Conditional {value:X8}"); }
        }
        public static explicit operator Condition(string conditionCode)
        {
            foreach (Condition conditional in _values.Values)
                if (conditional._name.Equals(conditionCode))
                    return conditional;
            throw new KeyNotFoundException($"Condition code {conditionCode} does not map to a valid Condition.");
        }
        public static implicit operator uint(Condition opCode) => opCode._value;

        public override string ToString() => _name;
    }

    // Lists all register codes that can appear in an instruction.
    // Mostly same with internal RCODE_* values, however, RCODE values do contain
    // some registers that are inaddressable by an instruction (e.g. MDR), so not all RCODE
    // codes are listed here.
    public sealed class AddressableRegisterCode
    {
        private static readonly Dictionary<uint, AddressableRegisterCode> _values = [];

        public static readonly AddressableRegisterCode PC  = new(0x00, "PC");
        public static readonly AddressableRegisterCode GP1 = new(0x01, "GP1");
        public static readonly AddressableRegisterCode GP2 = new(0x02, "GP2");
        public static readonly AddressableRegisterCode GP3 = new(0x03, "GP3");
        public static readonly AddressableRegisterCode GP4 = new(0x04, "GP4");
        public static readonly AddressableRegisterCode GP5 = new(0x05, "GP5");
        public static readonly AddressableRegisterCode GP6 = new(0x06, "GP6");
        public static readonly AddressableRegisterCode GP7 = new(0x07, "GP7");
        public static readonly AddressableRegisterCode GP8 = new(0x08, "GP8");
        public static readonly AddressableRegisterCode SR  = new(0x10, "SR");

        private readonly uint _value;
        private readonly string _name;
        private AddressableRegisterCode(uint value, string name)
        {
            _value = value;
            _name = name;
            _values.Add(value, this);
        }
        public static implicit operator AddressableRegisterCode(uint value)
        {
            try { return _values[value]; }
            catch (Exception) { throw new KeyNotFoundException($"Invalid register code {value:X8}"); }
        }
        public static explicit operator AddressableRegisterCode(string registerCode)
        {
            foreach (AddressableRegisterCode addressableRegister in _values.Values)
                if (addressableRegister._name.Equals(registerCode))
                    return addressableRegister;
            throw new KeyNotFoundException($"Register code {registerCode} does not map to a valid addressable register.");
        }
        public static implicit operator uint(AddressableRegisterCode opCode) => opCode._value;

        public override string ToString() => _name;
    }
}