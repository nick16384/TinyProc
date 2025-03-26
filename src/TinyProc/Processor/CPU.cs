using TinyProc.Memory;

namespace TinyProc.Processor;

public partial class CPU
{
    readonly Register[] GPRs;

    private readonly ControlUnit _CU;
    private readonly ALU _ALU;
    private readonly MMU _MMU;

    public CPU(RawMemory memory)
    {
        _ALU = new ALU();
        _MMU = new MMU(memory);
        _CU = new ControlUnit(this);

        GPRs = new Register[8];
        for (int i = 0; i < GPRs.Length; i++)
        {
            GPRs[i] = new Register(false, RegisterRWAccess.ReadWrite);
        }
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
        
        _CU._ControlBus.ControlState = ControlUnit.ControlState.Fetch1;
        _CU._ControlBus.ControlState = ControlUnit.ControlState.Fetch2;
        _CU._ControlBus.ControlState = ControlUnit.ControlState.Decode;
        _CU._ControlBus.ControlState = ControlUnit.ControlState.Execute;

        Console.WriteLine();

        Console.WriteLine("Cycle finished. Waiting for next clock pulse.");
    }
}