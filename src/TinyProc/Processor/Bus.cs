namespace TinyProc.Processor;

public interface IBusAttachable
{
    public void AttachToBus(uint ubid, Bus bus);
    public bool[] HandleBusUpdate(uint ubid, bool[] newBusData) => newBusData;
}

public interface ISelectableBusAttachable : IBusAttachable
{
    public bool[] HandleBusUpdateSelected(uint ubid, bool[] newBusData);
}

// A bus is a set of parallel lines (bits) that connect to multiple components.
// Every bus member holds a reference to the bus object.
// There should be at most one bus master, who controls data flow to mitigate garbled data from multiple sources.
// All other bus members update the data array, when the HandleBusUpdate() method is called.
// An externally managed bus with zero bus masters is also possible.
public class Bus
{
    private readonly IBusAttachable[] _registeredComponents;
    private bool[] _data;
    public bool[] Data
    {
        get => _data;
        set
        {
            if (value.Length != _data.Length)
                throw new ArgumentException($"Bus write data has different size {value.Length} than bus width {_data.Length}.");
            _data = value;
            foreach (IBusAttachable component in _registeredComponents)
                _data = component.HandleBusUpdate(_UBID, _data);
        }
    }
    // Unique bus identifier
    // Useful, when attaching multiple busses to a single class, to identify which bus is "speaking"
    public readonly uint _UBID;
    private static readonly List<uint> KnownUBIDs = [];

    public Bus(int busWidth, uint UBID, IBusAttachable[] registeredComponents)
    {
        _data = new bool[busWidth];
        _UBID = UBID;
        if (KnownUBIDs.Contains(_UBID))
            Console.Error.WriteLine($"Warning: Conflicting bus UBID {_UBID:x8} already in use by another bus!");
        KnownUBIDs.Add(_UBID);
        _registeredComponents = registeredComponents;
        foreach (IBusAttachable component in _registeredComponents)
            component.AttachToBus(_UBID, this);
    }

    // TOOD: Please make these methods more pleasing to look at!
    public static uint BoolArrayToUInt(bool[] boolArray, int startIndex)
    {
        uint resultUInt = 0x0u;
        for (int i = startIndex; i < sizeof(uint)*8 && i < boolArray.Length; i++)
        {
            uint boolAsUInt = boolArray[i] ? 0x1u : 0x0u;
            resultUInt |= boolAsUInt << (sizeof(uint)*8 - (i - startIndex + 1));
        }
        return resultUInt;
    }
    public static bool[] UIntToBoolArray(uint uintIn)
    {
        bool[] resultBoolArray = new bool[32];
        for (int i = 0; i < sizeof(uint)*8; i++)
        {
            bool boolAtIdx = (uintIn & (0b10000000_00000000_00000000_00000000u >> i)) >= 0x1u;
            resultBoolArray[i] = boolAtIdx;
        }
        return resultBoolArray;
    }
    public static bool[] FillBoolArrayWithUInt(bool[] boolArray, uint uintIn, int startIndex)
    {
        for (int i = startIndex; i < sizeof(uint)*8 + startIndex; i++)
        {
            boolArray[i] = (uintIn & (0b10000000_00000000_00000000_00000000u >> (i - startIndex))) >= 0x1u;
        }
        return boolArray;
    }
}

// TODO: Reduce duplicate code by inheriting from Bus class.
/*public class SelectiveBus
{
    // Sets the selected bus component.
    // If no component should be selected, then this is null.
    private ISelectableBusAttachable? _selectedComponent;
    public ISelectableBusAttachable? SelectedComponent
    {
        get => _selectedComponent;
        set
        {
            if (_registeredComponents.Contains(value))
                _selectedComponent = value;
            else
                throw new ArgumentException($"Selected bus component {value} is not part of this bus {this}.");
        }
    }
    public bool IsComponentSelected
    {
        get => SelectedComponent != null;
        set => _selectedComponent = null;
    }
    private readonly ISelectableBusAttachable[] _registeredComponents;
    private bool[] _data;
    private bool[] Data
    {
        get => _data;
        set
        {
            if (value.Length != _data.Length)
                throw new ArgumentException($"Bus write data has different size {value.Length} than bus width {_data.Length}.");
            _data = value;
            foreach (ISelectableBusAttachable component in _registeredComponents)
            {
                component.HandleBusUpdate(_UBID);
                if (component.Equals(SelectedComponent))
                    component.HandleBusUpdateSelected(_UBID);
            }
        }
    }
    private readonly uint _UBID;

    public SelectiveBus(int busWidth, uint UBID, ISelectableBusAttachable[] registeredComponents)
    {
        _UBID = UBID;
        _data = new bool[busWidth];
        _registeredComponents = registeredComponents;
        foreach (ISelectableBusAttachable component in _registeredComponents)
            component.SetBusDataArray(Data, _UBID);
    }
}*/