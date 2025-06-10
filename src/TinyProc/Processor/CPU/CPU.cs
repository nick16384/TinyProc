using TinyProc.Application;
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

    public uint Debug_GP1Value { get => GP1.ValueDirect; }
    public uint Debug_GP2Value { get => GP2.ValueDirect; }
    public uint Debug_GP3Value { get => GP3.ValueDirect; }
    public uint Debug_GP4Value { get => GP4.ValueDirect; }
    public uint Debug_GP5Value { get => GP5.ValueDirect; }
    public uint Debug_GP6Value { get => GP6.ValueDirect; }
    public uint Debug_GP7Value { get => GP7.ValueDirect; }
    public uint Debug_GP8Value { get => GP8.ValueDirect; }
    public uint Debug_PCValue { get => _CU.Debug_PCValue; }
    public uint Debug_SRValue { get => _ALU.SR.ValueDirect; }

    private static readonly Register CONST_POS1_SPECIAL_REG = new(1u, true, false, false, true, true);
    private static readonly Register CONST_NEG1_SPECIAL_REG = new(SignedIntToUInt(-1), true, false, false, true, true);
    private static readonly Register CONST_POS2_SPECIAL_REG = new(2u, true, false, false, true, true);
    private static readonly Register CONST_ZERO_SPECIAL_REG = new(0u, true, false, false, true, true);
    private static uint SignedIntToUInt(int intIn) { unchecked { return (uint)intIn; } }

    internal enum InternalRegisterCode : uint
    {
        // General-purpose registers
        RCODE_GP1 = 0x01u,
        RCODE_GP2 = 0x02u,
        RCODE_GP3 = 0x03u,
        RCODE_GP4 = 0x04u,
        RCODE_GP5 = 0x05u,
        RCODE_GP6 = 0x06u,
        RCODE_GP7 = 0x07u,
        RCODE_GP8 = 0x08u,

        // Program counter / Instruction pointer
        RCODE_PC = 0x00u,
        // Status register
        RCODE_SR = 0x10u,

        // Memory address register
        RCODE_SPECIAL_MAR = 0x70000000u,
        // Memory data register
        RCODE_SPECIAL_MDR = 0x70000001u,
        // Instruction registers A and B
        RCODE_SPECIAL_IRA = 0x70000002u,
        RCODE_SPECIAL_IRB = 0x70000003u,

        // Constant value, read-only registers
        RCODE_SPECIAL_CONST_POS1 = 0x70000004u,
        RCODE_SPECIAL_CONST_NEG1 = 0x70000005u,
        RCODE_SPECIAL_CONST_POS2 = 0x70000006u,
        RCODE_SPECIAL_CONST_ZERO = 0x70000007u,

        // Void register: Since writes to CONST_ZERO are discarded, this refers to it in another context.
        RCODE_SPECIAL_VOID = RCODE_SPECIAL_CONST_ZERO
    }

    private readonly ControlUnit _CU;
    private readonly ALU _ALU;
    private readonly MMU _MMU;

    public CPU(Dictionary<(uint, uint), RawMemory> rams, uint entryPoint)
    {
        GP1 = new Register();
        GP2 = new Register();
        GP3 = new Register();
        GP4 = new Register();
        GP5 = new Register();
        GP6 = new Register();
        GP7 = new Register();
        GP8 = new Register();

        _ALU = new ALU();
        _MMU = new MMU(null, (0x0, 0x0), rams);
        _CU = new ControlUnit(this, entryPoint, _ALU, _MMU);
    }

    public void NextClock()
    {
        Logging.PrintDebug("====================================================================\n");

        _CU.Temp_InstructionFetch1();
        _CU.Temp_InstructionFetch2();
        _CU.Temp_InstructionDecode();
        _CU.Temp_InstructionExecute();

        Logging.PrintDebug("\n====================================================================\n");
        Logging.LogInfo("Cycle finished. Waiting for next clock pulse.");
    }
}