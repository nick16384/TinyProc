namespace TinyProc.Processor;

public class Register(bool isSpecial, RegisterRWAccess access)
{
    public static readonly uint SYSTEM_WORD_SIZE = 32u;
    public uint Value { get; set; } = 0x0;
    public readonly RegisterRWAccess Access = access;
    public readonly bool IsSpecial = isSpecial;
}

public enum RegisterRWAccess
{
    ReadOnly,
    ReadWrite,
    WriteOnly
}