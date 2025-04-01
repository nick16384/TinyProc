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
    private class ALU : IBusAttachable
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

        // CPU internal bus related
        public const uint IBUS_SUBCOMP_ALU_X = 0x20;
        public const uint IBUS_SUBCOMP_ALU_Y = 0x21;
        public const uint IBUS_SUBCOMP_ALU_CONFIG = 0x22;
        public const uint IBUS_SUBCOMP_ALU_RESULT = 0x23;
        private bool[] _CPUIntBusDataArray = [];
        public void SetBusDataArray(bool[] busDataArray)
        {
            _CPUIntBusDataArray = busDataArray;
        }
        public void HandleBusUpdate()
        {
            uint subcompAddress = Bus.BoolArrayToUInt(_CPUIntBusDataArray, 0) >> 16;
            bool isWriteRequest = _CPUIntBusDataArray[8];
            uint data = Bus.BoolArrayToUInt(_CPUIntBusDataArray, 15);

            if (subcompAddress == IBUS_SUBCOMP_ALU_X && !isWriteRequest)
                _CPUIntBusDataArray = Bus.FillBoolArrayWithUInt(_CPUIntBusDataArray, X, 15);
            else if (subcompAddress == IBUS_SUBCOMP_ALU_X && isWriteRequest)
                X = data;
            if (subcompAddress == IBUS_SUBCOMP_ALU_Y && !isWriteRequest)
                _CPUIntBusDataArray = Bus.FillBoolArrayWithUInt(_CPUIntBusDataArray, Y, 15);
            else if (subcompAddress == IBUS_SUBCOMP_ALU_Y && isWriteRequest)
                Y = data;
            if (subcompAddress == IBUS_SUBCOMP_ALU_CONFIG && !isWriteRequest)
            {
                _CPUIntBusDataArray[42] = Config.zx;
                _CPUIntBusDataArray[43] = Config.nx;
                _CPUIntBusDataArray[44] = Config.zy;
                _CPUIntBusDataArray[45] = Config.ny;
                _CPUIntBusDataArray[46] = Config.f;
                _CPUIntBusDataArray[47] = Config.no;
            }
            else if (subcompAddress == IBUS_SUBCOMP_ALU_CONFIG && isWriteRequest)
            {
                Config = new OPConfig(
                    _CPUIntBusDataArray[42],
                    _CPUIntBusDataArray[43],
                    _CPUIntBusDataArray[44],
                    _CPUIntBusDataArray[45],
                    _CPUIntBusDataArray[46],
                    _CPUIntBusDataArray[47]
                );
            }
            if (subcompAddress == IBUS_SUBCOMP_ALU_RESULT && !isWriteRequest)
                _CPUIntBusDataArray = Bus.FillBoolArrayWithUInt(_CPUIntBusDataArray, Out, 15);
            else if (subcompAddress == IBUS_SUBCOMP_ALU_RESULT && isWriteRequest)
                Console.Error.WriteLine("Ignoring ALU Result override via internal bus.");
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