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

    private readonly ControlUnit _CU;
    private readonly ALU _ALU;
    private readonly MMU _MMU;

    public CPU(RawMemory memory)
    {
        _ALU = new ALU();
        _MMU = new MMU(memory);
        _CU = new ControlUnit(this);

        GP1 = new Register(false, RegisterRWAccess.ReadWrite);
        GP2 = new Register(false, RegisterRWAccess.ReadWrite);
        GP3 = new Register(false, RegisterRWAccess.ReadWrite);
        GP4 = new Register(false, RegisterRWAccess.ReadWrite);
        GP5 = new Register(false, RegisterRWAccess.ReadWrite);
        GP6 = new Register(false, RegisterRWAccess.ReadWrite);
        GP7 = new Register(false, RegisterRWAccess.ReadWrite);
        GP8 = new Register(false, RegisterRWAccess.ReadWrite);
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