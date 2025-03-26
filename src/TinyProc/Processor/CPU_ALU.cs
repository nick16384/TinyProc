namespace TinyProc.Processor;

/*
Source of info for this ALU:
https://www.youtube.com/watch?v=PEs855FNCOw&list=PLrDd_kMiAuNmSb-CKWQqq9oBFN_KNMTaI&index=17
*/

struct OPConfig(bool zx, bool nx, bool zy, bool ny, bool f, bool no)
{
    // If true, set x = 0
    public bool zx = zx;
    // If true, set x = !x
    public bool nx = nx;
    // If true, set y = 0
    public bool zy = zy;
    // If true, set y = !y
    public bool ny = ny;
    // If true, out = x + y; If false, out = x & y
    public bool f = f;
    // If true, out = !out
    public bool no = no;
}

enum ArithmeticOperation
{
    AdditionSigned,
    XY_SubtractionSigned,
    YX_SubtractionSigned,
    X_Negative,
    Y_Negative,
    X_Increment,
    Y_Increment,
    X_Decrement,
    Y_Decrement,
    LogicalAND,
    LogicalOR,
    X_LogicalNOT,
    Y_LogicalNOT
}


// Arithmetic Logic Unit
// Controlled by the CU to do arithmetic on register contents
// x,y -> [op] -> out
public partial class CPU
{
    private class ALU
    {
        public uint X { get; set; }
        public uint Y { get; set; }
        public OPConfig Config { get; set; }
        public uint Out { get => ComputeResult(); }
        public bool CarryBit { get; private set; }

        private uint ComputeResult()
        {
            uint x = X;
            uint y = Y;

            if (Config.zx)
                x = 0x0u;
            if (Config.nx)
                x = ~x;
            if (Config.zy)
                y = 0x0u;
            if (Config.ny)
                y = ~y;

            uint @out;
            if (Config.f)
                @out = x + y;
            else
                @out = x & y;
        
            if (Config.no)
                @out = ~@out;
        
            return @out;
        }

        public static readonly Dictionary<ArithmeticOperation, OPConfig> ARITHMETIC_OP_LOOKUP = new()
        {
            // TODO: Complete this list
            { ArithmeticOperation.AdditionSigned,       new OPConfig(false, false, false, false, true, false) },
            { ArithmeticOperation.XY_SubtractionSigned, new OPConfig(false, true, false, false, true, true) },
            { ArithmeticOperation.YX_SubtractionSigned, new OPConfig(false, false, false, true, true, true) },
            { ArithmeticOperation.X_Negative,           new OPConfig(false, false, true, true, true, true) },
            { ArithmeticOperation.Y_Negative,           new OPConfig(true, true, false, false, true, true) },
            { ArithmeticOperation.X_Increment,          new OPConfig(false, true, true, true, true, true) },
            { ArithmeticOperation.Y_Increment,          new OPConfig(true, true, false, true, true, true) },
            { ArithmeticOperation.X_Decrement,          new OPConfig(false, false, true, true, true, false) },
            { ArithmeticOperation.Y_Decrement,          new OPConfig(true, true, false, false, true, false) },
            { ArithmeticOperation.LogicalAND,           new OPConfig(false, false, false, false, false, false) },
            { ArithmeticOperation.LogicalOR,            new OPConfig(false, false, false, false, false, false) },
            { ArithmeticOperation.X_LogicalNOT,         new OPConfig(false, false, false, false, false, false) },
            { ArithmeticOperation.Y_LogicalNOT,         new OPConfig(false, true, false, true, false, true) }
        };
    }
}