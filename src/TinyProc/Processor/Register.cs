namespace TinyProc.Processor;

public class Register(bool isSpecial = false, RegisterRWAccess access = RegisterRWAccess.ReadWrite)
{
    public static readonly uint SYSTEM_WORD_SIZE = 32u;
    public virtual uint Value { get; set; } = 0x0;
    public readonly RegisterRWAccess Access = access;
    public readonly bool IsSpecial = isSpecial;
}

// Note that the access type does not prevent illegal access to any register with an associated type.
// It is merely an information to make clear how the register is accessible globally on the CPU.
// This may have some use later.
public enum RegisterRWAccess
{
    ReadOnly,
    ReadWrite,
    WriteOnly
}