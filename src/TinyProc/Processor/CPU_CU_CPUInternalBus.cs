namespace TinyProc.Processor;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Controls data flow into and out of the Control Unit.
        // 32 bits - data
        // ALU A (Input 1)
        private readonly Bus _IntBus1;
        // ALU B (Input 2)
        private readonly Bus _IntBus2;
        // ALU Res (Output)
        private readonly Bus _IntBus3;
        private bool[] _IntBus1DataArray = [];
        private bool[] _IntBus2DataArray = [];
        private bool[] _IntBus3DataArray = [];

        // Implement bus methods
        public void SetBusDataArray(bool[] busDataArray)
        {
            _IntBus1DataArray = busDataArray;
        }
        public void HandleBusUpdate()
        {
            uint subcompAddress = Bus.BoolArrayToUInt(_IntBus1DataArray, 0) >> 16;
            bool isWriteRequest = _IntBus1DataArray[8];
            uint data = Bus.BoolArrayToUInt(_IntBus1DataArray, 15);

            if (subcompAddress == IBUS_SUBCOMP_CU_PC && !isWriteRequest)
                _IntBus1DataArray = Bus.FillBoolArrayWithUInt(_IntBus1DataArray, PC.Value, 15);
            else if (subcompAddress == IBUS_SUBCOMP_CU_PC && isWriteRequest)
                PC.Value = data;
            else if (subcompAddress == IBUS_SUBCOMP_CU_IRA && !isWriteRequest)
                _IntBus1DataArray = Bus.FillBoolArrayWithUInt(_IntBus1DataArray, IRA.Value, 15);
            else if (subcompAddress == IBUS_SUBCOMP_CU_IRA && isWriteRequest)
                IRA.Value = data;
            else if (subcompAddress == IBUS_SUBCOMP_CU_IRB && !isWriteRequest)
                _IntBus1DataArray = Bus.FillBoolArrayWithUInt(_IntBus1DataArray, IRB.Value, 15);
            else if (subcompAddress == IBUS_SUBCOMP_CU_IRB && isWriteRequest)
                IRB.Value = data;
        }
    }
}