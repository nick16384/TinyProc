namespace TinyProc.Processor;

public class Register(bool isSpecial, RegisterRWAccess access)
{
    public static readonly uint SYSTEM_WORD_SIZE = 32u;
    public uint Value { get; set; } = 0x0;
    public readonly RegisterRWAccess Access = access;
    public readonly bool IsSpecial = isSpecial;
}

// Note that the access type does not prevent illegal access to any register with an associated type.
// It is merely an information to make clear which access type register is in use.
// This may have a use later.
public enum RegisterRWAccess
{
    ReadOnly,
    ReadWrite,
    WriteOnly
}