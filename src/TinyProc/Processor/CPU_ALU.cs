namespace TinyProc.Processor;

/*
Source of info for this ALU:
https://www.youtube.com/watch?v=PEs855FNCOw&list=PLrDd_kMiAuNmSb-CKWQqq9oBFN_KNMTaI&index=17
*/
// Arithmetic Logic Unit
// Controlled by the CU to do arithmetic on register contents
// x,y -> [op] -> out
public partial class CPU
{
    private class ALU
    {
        public class ALU_OpCode(bool zx, bool nx, bool zy, bool ny, bool f, bool no)
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

        public enum ALU_Operation
        {
            TransferA,
            TransferB,
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

        public class ALUDataARegister(ALU alu) : Register(true, RegisterRWAccess.ReadOnly)
        {
            private readonly ALU _alu = alu;
            public override uint Value
            {
                get => _alu.A.Value;
                set
                {
                    _alu.A.Value = value;
                    _alu._R.Value = _alu.ComputeResult();
                }
            }
        }
        public class ALUDataBRegister(ALU alu) : Register(true, RegisterRWAccess.ReadOnly)
        {
            private readonly ALU _alu = alu;
            public override uint Value
            {
                get => _alu.B.Value;
                set
                {
                    _alu.B.Value = value;
                    _alu._R.Value = _alu.ComputeResult();
                }
            }
        }

        public readonly ALUDataARegister A;
        public readonly ALUDataBRegister B;
        public /*required*/ ALU_OpCode OpCode = ARITHMETIC_OP_LOOKUP[ALU_Operation.TransferA];
        private readonly Register _R = new(true, RegisterRWAccess.ReadOnly);
        public Register R
        {
            get
            {
                _R.Value = ComputeResult();
                Console.Error.WriteLine($"ALU res: {_R.Value}");
                return _R;
            }
        }
        // Status register
        // TODO: Implement
        public readonly Register SR = new(true, RegisterRWAccess.ReadOnly);

        public ALU()
        {
            A = new ALUDataARegister(this);
            B = new ALUDataBRegister(this);
        }

        private uint ComputeResult()
        {
            uint x = A.Value;
            uint y = B.Value;

            if (OpCode.zx)
                x = 0x0u;
            if (OpCode.nx)
                x = ~x;
            if (OpCode.zy)
                y = 0x0u;
            if (OpCode.ny)
                y = ~y;

            uint @out;
            if (OpCode.f)
                @out = x + y;
            else
                @out = x & y;
        
            if (OpCode.no)
                @out = ~@out;

            OpCode.f = true;
        
            return @out;
        }

        public static readonly Dictionary<ALU_Operation, ALU_OpCode> ARITHMETIC_OP_LOOKUP = new()
        {
            { ALU_Operation.TransferA,            new ALU_OpCode(false, false, true, true, false, false) },
            { ALU_Operation.TransferB,            new ALU_OpCode(true, true, false, false, false, false) },
            { ALU_Operation.AdditionSigned,       new ALU_OpCode(false, false, false, false, true, false) },
            { ALU_Operation.XY_SubtractionSigned, new ALU_OpCode(false, true, false, false, true, true) },
            { ALU_Operation.YX_SubtractionSigned, new ALU_OpCode(false, false, false, true, true, true) },
            { ALU_Operation.X_Negative,           new ALU_OpCode(false, false, true, true, true, true) },
            { ALU_Operation.Y_Negative,           new ALU_OpCode(true, true, false, false, true, true) },
            { ALU_Operation.X_Increment,          new ALU_OpCode(false, true, true, true, true, true) },
            { ALU_Operation.Y_Increment,          new ALU_OpCode(true, true, false, true, true, true) },
            { ALU_Operation.X_Decrement,          new ALU_OpCode(false, false, true, true, true, false) },
            { ALU_Operation.Y_Decrement,          new ALU_OpCode(true, true, false, false, true, false) },
            { ALU_Operation.LogicalAND,           new ALU_OpCode(false, false, false, false, false, false) },
            { ALU_Operation.LogicalOR,            new ALU_OpCode(false, false, false, false, false, false) },
            { ALU_Operation.X_LogicalNOT,         new ALU_OpCode(false, false, false, false, false, false) },
            { ALU_Operation.Y_LogicalNOT,         new ALU_OpCode(false, true, false, true, false, true) }
        };
    }
}