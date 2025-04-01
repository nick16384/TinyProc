namespace TinyProc.Processor;

public class Register(bool isSpecial = false, RegisterRWAccess access = RegisterRWAccess.ReadWrite)
{
    public static readonly uint SYSTEM_WORD_SIZE = 32u;
    public virtual uint Value { get; set; } = 0x0;
    public readonly RegisterRWAccess Access = access;
    public readonly bool IsSpecial = isSpecial;
}
public class RegisterFile(Dictionary<uint, Register> registers) : ISelectableBusAttachable
{
    // Maps register addresses to their corresponding object
    private readonly Dictionary<uint, Register> _registers = registers;
    public bool WriteEnable { get; set; } = false;
    public uint RegisterAddress { get; set; } = 0x0u;
    public uint RegisterValue
    {
        get
        {
            try { return _registers[RegisterAddress].Value; }
            catch (Exception)
            {
                Console.Error.WriteLine($"Register address {RegisterAddress:X} is not part of register file {this}.");
                throw;
            }
        }
        set
        {
            if (!WriteEnable)
            {
                Console.Error.WriteLine("Register value written without write enable. Discarding changes.");
                return;
            }
            _registers[RegisterAddress].Value = value;
        }
    }

    private bool[] _BusDataArray = [];
    public void SetBusDataArray(bool[] busDataArray)
    {
        _BusDataArray = busDataArray;
    }
    public void HandleBusUpdate()
    {
        // Ignore everything unless selected
    }
    public void HandleBusUpdateSelected()
    {
        throw new NotImplementedException();
    }
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