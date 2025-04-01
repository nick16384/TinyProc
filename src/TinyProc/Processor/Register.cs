using System.Runtime.InteropServices.Marshalling;

namespace TinyProc.Processor;

// A register is a simple, yet fast storage of a single word.
// It can optionally be attached to a bus, which then takes over read/write operations to/from the register.
public class Register(bool isSpecial = false, RegisterRWAccess access = RegisterRWAccess.ReadWrite) : IBusAttachable
{
    public static readonly int SYSTEM_WORD_SIZE = 32;
    public readonly RegisterRWAccess Access = access;
    public readonly bool IsSpecial = isSpecial;
    private uint _value;
    public virtual uint Value
    {
        get => _value;
        set
        {
            if (IsConnectedToBus)
                throw new InvalidOperationException($"Register {this} attached to bus; Direct write operation illegal.");
            _value = value;
        }
    }
    // When attached to a bus, controls how the register behaves on the bus.
    private bool _busReadEnable;
    public bool BusReadEnable
    {
        get => _busReadEnable;
        set
        {
            _busReadEnable = value;
            if (_busReadEnable && IsConnectedToBus && _busDataArrays.ContainsKey(CurrentUBID))
                _busDataArrays[CurrentUBID] = Bus.UIntToBoolArray(Value);
        }
    }
    private bool _busWriteEnable;
    public bool BusWriteEnable
    {
        get => _busWriteEnable;
        set { _busWriteEnable = value; }
    }

    // Externally controls which of the attached busses to read / write to / from.
    public uint CurrentUBID;

    private readonly Dictionary<uint, bool[]> _busDataArrays = [];
    private bool IsConnectedToBus { get => _busDataArrays.Values.ToArray().Length > 0; }
    public void SetBusDataArray(bool[] busDataArray, uint ubid)
    {
        if (busDataArray.Length != SYSTEM_WORD_SIZE)
            throw new ArgumentException(
                $"Bus cannot connect to register {this}, since it has a different width of {busDataArray.Length}.");
        _busDataArrays.Add(ubid, busDataArray);
    }
    public void HandleBusUpdate(uint ubid)
    {
        if (BusWriteEnable)
            _value = Bus.BoolArrayToUInt(_busDataArrays[ubid], 0);
    }
}
public class RegisterFile(Dictionary<uint, Register> registers) /*: ISelectableBusAttachable*/
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