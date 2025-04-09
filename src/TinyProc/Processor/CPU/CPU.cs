using TinyProc.Memory;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private readonly Register GP1;
    private readonly Register GP2;
    private readonly Register GP3;
    private readonly Register GP4;
    private readonly Register GP5;
    private readonly Register GP6;
    private readonly Register GP7;
    private readonly Register GP8;

    // Special registers whose values cannot be changed and always output plus / negative one.
    // Can also act as "void" registers, since writes have no effect
    private class ConstantValueRegister(uint constValue) : Register(true, RegisterRWAccess.ReadOnly)
    {
        private protected override uint Value
        {
            get => constValue;
            set {}
        }
    }
    private static readonly ConstantValueRegister CV_P1_SPECIAL_REG = new(1u);
    private static readonly ConstantValueRegister CV_N1_SPECIAL_REG = new(SignedIntToUInt(-1));
    private static readonly ConstantValueRegister CV_P2_SPECIAL_REG = new(2u);
    private static readonly ConstantValueRegister CV_0_SPECIAL_REG = new(0u);
    private static uint SignedIntToUInt(int intIn) { unchecked { return (uint)intIn; } }

    private const uint RCODE_GP1 = 0x01u;
    private const uint RCODE_GP2 = 0x02u;
    private const uint RCODE_GP3 = 0x03u;
    private const uint RCODE_GP4 = 0x04u;
    private const uint RCODE_GP5 = 0x05u;
    private const uint RCODE_GP6 = 0x06u;
    private const uint RCODE_GP7 = 0x07u;
    private const uint RCODE_GP8 = 0x08u;

    private const uint RCODE_PC = 0x00u;
    private const uint RCODE_SR = 0x10u;

    private const uint RCODE_SPECIAL_MAR = 0x70000000;
    private const uint RCODE_SPECIAL_MDR = 0x70000001;
    private const uint RCODE_SPECIAL_IRA = 0x70000002;
    private const uint RCODE_SPECIAL_IRB = 0x70000003;
    // Constant value (read-only) registers
    // +1
    private const uint RCODE_SPECIAL_CV_P1 = 0x70000004;
    // -1
    private const uint RCODE_SPECIAL_CV_N1 = 0x70000005;
    // +2
    private const uint RCODE_SPECIAL_CV_P2 = 0x70000006;
    // 0
    private const uint RCODE_SPECIAL_CV_0 = 0x70000007;
    // Void register: Since writes to CV0 are discarded, this refers to it in another context.
    private const uint RCODE_SPECIAL_VOID = 0x70000007;

    private readonly ControlUnit _CU;
    private readonly ALU _ALU;
    private readonly MMU _MMU;

    public CPU(RawMemory[] rams)
    {
        GP1 = new Register(false, RegisterRWAccess.ReadWrite);
        GP2 = new Register(false, RegisterRWAccess.ReadWrite);
        GP3 = new Register(false, RegisterRWAccess.ReadWrite);
        GP4 = new Register(false, RegisterRWAccess.ReadWrite);
        GP5 = new Register(false, RegisterRWAccess.ReadWrite);
        GP6 = new Register(false, RegisterRWAccess.ReadWrite);
        GP7 = new Register(false, RegisterRWAccess.ReadWrite);
        GP8 = new Register(false, RegisterRWAccess.ReadWrite);

        _ALU = new ALU();
        _MMU = new MMU(null, rams);
        _CU = new ControlUnit(this, _ALU, _MMU);
    }

    private bool _clockLevel;
    public bool ClockLevel
    {
        get => _clockLevel;
        // If rising edge, initiate next clock cycle
        set
        {
            bool clockLevelOld = _clockLevel;
            _clockLevel = value;
            if (clockLevelOld == false && value == true)
                NextClock();
        }
    }

    private void NextClock()
    {
        Console.WriteLine("====================================================================");

        _CU.Temp_InstructionFetch1();
        _CU.Temp_InstructionFetch2();
        _CU.Temp_InstructionDecode();
        _CU.Temp_InstructionExecute();

        Console.WriteLine("\n====================================================================");
        Console.WriteLine("Cycle finished. Waiting for next clock pulse.");
    }

    /*public static (uint, uint) ForgeInstruction(
        ControlUnit.OpCode opCode, ControlUnit.Condition condition=ControlUnit.Condition.ALWAYS, uint operand2=0x00000000u)
    {
        byte opCodeBits =
            ControlUnit.INSTRUCTION_SIXBIT_OPCODE_DICT.FirstOrDefault(opCodeX => opCodeX.Value == opCode).Key;
        byte conditionalBits =
            ControlUnit.INSTRUCTION_FOURBIT_CONDITIONAL_DICT.FirstOrDefault(condX => condX.Value == condition).Key;

        uint instructionA = 0x0u;
        instructionA |= (uint)(opCodeBits << 26);
        instructionA |= (uint)(conditionalBits << 22);
            
        uint instructionB = operand2;

        return (instructionA, instructionB);
    }*/
}