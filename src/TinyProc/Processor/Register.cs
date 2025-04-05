namespace TinyProc.Processor;

// A register is a simple, yet fast storage of a single word.
// It can optionally be attached to a bus, which then takes over read/write operations to/from the register.
public class Register(bool isSpecial = false, RegisterRWAccess access = RegisterRWAccess.ReadWrite) : IBusAttachable
{
    public static readonly int SYSTEM_WORD_SIZE = 32;
    public readonly RegisterRWAccess Access = access;
    public readonly bool IsSpecial = isSpecial;
    // The value actually stored in the register
    private protected uint _storedValue;
    // This is the only value that can be overridden by subclasses.
    private protected virtual uint Value
    {
        get => _storedValue;
        set => _storedValue = value;
    }
    // Safeguard that cannot be overriden by subclasses
    // Is used when bus operations handle register content.
    private uint ValueBus
    {
        get => Value;
        set
        {
            Value = value;
            // Update bus
            if (BusReadEnable)
                _busses[ReadSourceUBID].Data = Bus.UIntToBoolArray(Value);
        }
    }
    // Used to directly (not via bus) communicate with this register.
    // Cannot be set when attached to bus, since it takes over control over register content.
    public uint ValueDirect
    {
        get => Value;
        set
        {
            if (IsConnectedToBus)
                throw new InvalidOperationException($"Register {this} attached to bus; Direct write operation illegal.");
            Value = value;
        }
    }
    // When attached to a bus, controls how the register behaves on the bus.
    // Enable register -> bus connection
    private bool _busReadEnable;
    public bool BusReadEnable
    {
        get => _busReadEnable;
        set
        {
            _busReadEnable = value;
            if (_busReadEnable && IsConnectedToBus && _busses.ContainsKey(WriteTargetUBID))
                _busses[WriteTargetUBID].Data = Bus.UIntToBoolArray(ValueBus);
        }
    }
    // Enable bus -> register connection
    private bool _busWriteEnable;
    public bool BusWriteEnable
    {
        get => _busWriteEnable;
        set { _busWriteEnable = value; }
    }

    // Externally controls which of the attached busses to write to.
    public uint WriteTargetUBID;
    // Externally controls which of the attached busses to read from.
    public uint ReadSourceUBID;

    private readonly Dictionary<uint, Bus> _busses = [];
    private bool IsConnectedToBus { get => _busses.Values.ToArray().Length > 0; }
    public void AttachToBus(uint ubid, Bus bus)
    {
        if (bus.Data.Length != SYSTEM_WORD_SIZE)
            throw new ArgumentException(
                $"Bus cannot connect to register {this}, since it has a different width of {bus.Data.Length}.");
        _busses.Add(ubid, bus);
    }
    public bool[] HandleBusUpdate(uint ubid, bool[] newBusData)
    {
        if (BusWriteEnable)
            Value = Bus.BoolArrayToUInt(_busses[ubid].Data, 0);
        return newBusData;
    }
}

public class RegisterFile /*: IBusAttachable*/
{
    // TODO: Implement later
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