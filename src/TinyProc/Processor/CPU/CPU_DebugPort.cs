namespace TinyProc.Processor.CPU;

public partial class CPU
{
    /// <summary>
    /// The debug port is a piece of pseudo-hardware, allowing external code to read
    /// the internal CPU's state.
    /// </summary>
    /// <param name="cpu"></param>
    public class CPUDebugPort(CPU cpu)
    {
        private CPU _cpu = cpu;

        // General-purpose registers
        public uint GP1Value { get => _cpu.GP1.ValueDirect; }
        public uint GP2Value { get => _cpu.GP2.ValueDirect; }
        public uint GP3Value { get => _cpu.GP3.ValueDirect; }
        public uint GP4Value { get => _cpu.GP4.ValueDirect; }
        public uint GP5Value { get => _cpu.GP5.ValueDirect; }
        public uint GP6Value { get => _cpu.GP6.ValueDirect; }
        public uint GP7Value { get => _cpu.GP7.ValueDirect; }
        public uint GP8Value { get => _cpu.GP8.ValueDirect; }

        // Special registers
        public uint PCValue { get => _cpu._CU.Debug_PCValue; }
        public uint IRAValue { get => _cpu._CU.Debug_IRAValue; }
        public uint IRBValue { get => _cpu._CU.Debug_IRBValue; }
        public uint SRValue { get => _cpu._ALU.SR.ValueDirect; }
        public uint MARValue { get => _cpu._MMU.MAR.ValueDirect; }
        public uint MDRValue { get => _cpu._MMU.MDR.ValueDirect; }
    }
}