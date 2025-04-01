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
    private readonly Register CONST_PLUSONE = new(true, RegisterRWAccess.ReadOnly);
    private readonly Register CONST_MINUSONE = new(true, RegisterRWAccess.ReadOnly);

    private const uint GP1_REGISTER_CODE = 0x01u;
    private const uint GP2_REGISTER_CODE = 0x02u;
    private const uint GP3_REGISTER_CODE = 0x03u;
    private const uint GP4_REGISTER_CODE = 0x04u;
    private const uint GP5_REGISTER_CODE = 0x05u;
    private const uint GP6_REGISTER_CODE = 0x06u;
    private const uint GP7_REGISTER_CODE = 0x07u;
    private const uint GP8_REGISTER_CODE = 0x08u;

    private const uint PC_REGISTER_CODE = 0x00u;
    private const uint SR_REGISTER_CODE = 0x10u;

    private const uint MAR_SPECIAL_REGISTER_CODE = 0x70000000;
    private const uint MDR_SPECIAL_REGISTER_CODE = 0x70000001;
    private const uint IRA_SPECIAL_REGISTER_CODE = 0x70000002;
    private const uint IRB_SPECIAL_REGISTER_CODE = 0x70000003;
    private const uint CONST_PLUSONE_SPECIAL_REGISTER_CODE = 0x70000004;
    private const uint CONST_MINUSONE_SPECIAL_REGISTER_CODE = 0x70000005;

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
        _CU = new ControlUnit(this, _ALU);
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
        Console.WriteLine("Clock pulse received; Executing next cycle...");

        Console.WriteLine("Fetching next instruction from memory");
        Console.WriteLine("Stage fetch");
        _CU._ControlBus.ControlState = ControlUnit.ControlState.Fetch1;
        _CU._ControlBus.ControlState = ControlUnit.ControlState.Fetch2;
        Console.WriteLine("Stage decode");
        _CU._ControlBus.ControlState = ControlUnit.ControlState.Decode;
        Console.WriteLine("Stage execute");
        _CU._ControlBus.ControlState = ControlUnit.ControlState.Execute;

        Console.WriteLine();

        Console.WriteLine("Cycle finished. Waiting for next clock pulse.");
    }

    public static (uint, uint) ForgeInstruction(
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
        }
}