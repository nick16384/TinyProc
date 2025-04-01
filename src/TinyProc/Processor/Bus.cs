namespace TinyProc.Processor;

public interface IBusAttachable
{
    public void SetBusDataArray(bool[] busDataArray, uint ubid);
    // No need to pass new data, since a reference to the data should be kept after SetBusArrayData() has been called.
    public void HandleBusUpdate();
}

public interface ISelectableBusAttachable : IBusAttachable
{
    public void HandleBusUpdateSelected(uint ubid);
}

// A bus is a set of parallel lines (bits) that connect to multiple components.
// Only the bus master should have a reference to the bus object.
// The bus master however, also is part of the attached components array.
// All other bus members update the data array via a reference (similar to a callback)
public class Bus
{
    private readonly IBusAttachable[] _registeredComponents;
    private bool[] _data;
    private bool[] Data
    {
        get => _data;
        set
        {
            if (value.Length != _data.Length)
                throw new ArgumentException($"Bus write data has different size {value.Length} than bus width {_data.Length}.");
            _data = value;
            foreach (IBusAttachable component in _registeredComponents)
                component.HandleBusUpdate();
        }
    }
    // Unique bus identifier
    // Useful, when attaching multiple busses to a single class, to identify which bus is "speaking"
    private readonly uint _UBID;

    public Bus(int busWidth, IBusAttachable[] registeredComponents)
    {

        _data = new bool[busWidth];
        _registeredComponents = registeredComponents;
        foreach (IBusAttachable component in _registeredComponents)
            component.SetBusDataArray(Data, _UBID);
    }

    public static uint BoolArrayToUInt(bool[] boolArray, int startIndex)
    {
        uint resultUInt = 0x0u;
        for (int i = startIndex; i < sizeof(uint)*8 && i < boolArray.Length; i++)
        {
            uint boolAsUInt = boolArray[i] ? 0x1u : 0x0u;
            resultUInt |= boolAsUInt;
        }
        return resultUInt;
    }
    public static bool[] UIntToBoolArray(uint uintIn)
    {
        bool[] resultBoolArray = new bool[32];
        for (int i = 0; i < sizeof(uint)*8; i++)
        {
            bool boolAtIdx = (uintIn >> (sizeof(uint)*8 - i)) == 0x1u;
            resultBoolArray[i] = boolAtIdx;
        }
        return resultBoolArray;
    }
    public static bool[] FillBoolArrayWithUInt(bool[] boolArray, uint uintIn, int startIndex)
    {
        for (int i = startIndex; i < sizeof(uint)*8; i++)
        {
            boolArray[i] = (uintIn & (0x1u << i)) != 0x0u;
        }
        return boolArray;
    }
}

// TODO: Reduce duplicate code by inheriting from Bus class.
public class SelectiveBus
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
                component.HandleBusUpdate();
                if (component.Equals(SelectedComponent))
                    component.HandleBusUpdateSelected();
            }
        }
    }

    public SelectiveBus(int busWidth, ISelectableBusAttachable[] registeredComponents)
    {
        _data = new bool[busWidth];
        _registeredComponents = registeredComponents;
        foreach (ISelectableBusAttachable component in _registeredComponents)
            component.SetBusDataArray(Data);
    }
}

public class AddressBus
{

}