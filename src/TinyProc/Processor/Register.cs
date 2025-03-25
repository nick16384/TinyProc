namespace TinyProc.Memory;

public class Register(RegisterType type)
{
    public static readonly ulong SYSTEM_WORD_SIZE = 64u;
    public ulong Value { get; set; } = 0x0;
    public readonly RegisterType Type = type;
}

public enum RegisterType
{
    ProgramCounter,
    GeneralPurpose,
    ArithmeticComparison
}

public class SystemRegisterCollection
{
    // Program counter:
    // Keeps address for next instruction
    public readonly Register PC;
    // General purpose registers
    public readonly Register[] GPRs;
    // Arithmetic comparison register:
    // Keeps result of last comparison instruction
    public readonly Register ACMP;

    public SystemRegisterCollection(ulong gprCount)
    {
        PC = new Register(RegisterType.ProgramCounter);
        GPRs = new Register[gprCount];
        for (ulong i = 0; i < gprCount; i++)
            GPRs[i] = new Register(RegisterType.GeneralPurpose);
        ACMP = new Register(RegisterType.ArithmeticComparison);
    }
}