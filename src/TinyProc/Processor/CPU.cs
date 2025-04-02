using System.Runtime.InteropServices;
using TinyProc.Memory;

namespace TinyProc.Processor;

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
    private static readonly Register CV_P1_SPECIAL_REG = new(true, RegisterRWAccess.ReadOnly){ Value = 1u };
    private static readonly Register CV_N1_SPECIAL_REG = new(true, RegisterRWAccess.ReadOnly){ Value = SignedIntToUInt(-1) };
    private static readonly Register CV_P2_SPECIAL_REG = new(true, RegisterRWAccess.ReadOnly){ Value = 2u };
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

    private readonly ControlUnit _CU;
    private readonly ALU _ALU;
    private readonly MMU _MMU;

    public CPU(RawMemory memory)
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
        _MMU = new MMU(memory);
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
        Console.WriteLine("Clock edge rising");

        _CU.Temp_InstructionFetch1();
        Console.Error.WriteLine("Debug: Skipping fetch2, decode, exec.");
        //_CU.Temp_InstructionFetch2();
        //_CU.Temp_InstructionDecode();
        //_CU.Temp_InstructionExecute();

        Console.WriteLine();

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