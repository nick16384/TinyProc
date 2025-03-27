namespace TinyProc.Processor;

public partial class CPU
{
    public partial class ControlUnit
    {
        // Controls data flow into and out of the Control Unit.
        public class ControlBus(ControlUnit _CU, MMU _MMU)
        {
            // Define regions

            // Control unit region
            public uint IRA
            {
                get => _CU.IRA.Value;
                set => _CU.IRA.Value = value;
            }
            public uint IRB
            {
                get => _CU.IRB.Value;
                set => _CU.IRB.Value = value;
            }
            public uint PC
            {
                get => _CU.PC.Value;
                set => _CU.PC.Value = value;
            }
            public ControlState ControlState
            {
                get => _CU.CurrentControlState;
                set => _CU.CurrentControlState = value;
            }

            // Memory management unit region
            // TODO: Change code so that these registers are only accessible via the control bus.
            public uint MAR
            {
                get => _MMU.MAR.Value;
                set => _MMU.MAR.Value = value;
            }
            public uint MDR
            {
                get => _MMU.MDR.Value;
                set => _MMU.MDR.Value = value;
            }
        }
    }
}