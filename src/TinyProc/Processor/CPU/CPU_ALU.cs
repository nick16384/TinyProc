namespace TinyProc.Processor.CPU;

/*
Source of info for this ALU:
https://www.youtube.com/watch?v=PEs855FNCOw&list=PLrDd_kMiAuNmSb-CKWQqq9oBFN_KNMTaI&index=17
*/
// Arithmetic Logic Unit
// Controlled by the CU to do arithmetic on register contents
// x,y -> [op] -> out
public partial class CPU
{
    public class ALU
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

            public override string ToString()
            {
                return
                    (zx ? "1" : "0") +
                    (nx ? "1" : "0") +
                    (zy ? "1" : "0") +
                    (ny ? "1" : "0") +
                    (f  ? "1" : "0") +
                    (no ? "1" : "0");
            }
        }

        public enum ALU_Operation
        {
            TransferA,
            TransferB,
            AdditionSigned,
            AB_SubtractionSigned,
            BA_SubtractionSigned,
            A_Negative,
            B_Negative,
            A_Increment,
            B_Increment,
            A_Decrement,
            B_Decrement,
            LogicalAND,
            LogicalOR,
            A_LogicalNOT,
            B_LogicalNOT
        }

        public class ALUDataRegister(ALU alu) : Register(true, RegisterRWAccess.ReadWrite)
        {
            private readonly ALU _alu = alu;
            private protected override uint Value
            {
                get => _storedValue;
                set
                {
                    _storedValue = value;
                    // Update result register --> Trigger bus update
                    uint? unassigned = _alu?.R?.ValueDirect;
                }
            }
        }

        public class ALUResultRegister(ALU alu) : Register(true, RegisterRWAccess.ReadOnly)
        {
            private readonly ALU _alu = alu;
            private protected override uint Value
            {
                get
                {
                    _storedValue = _alu.ComputeResult();
                    return _storedValue;
                }
                set => _storedValue = value;
            }
        }

        public readonly ALUDataRegister A;
        public readonly ALUDataRegister B;
        public /*required*/ ALU_OpCode OpCode = new(false, false, false, false, false, false);
        public readonly ALUResultRegister R;

        // Status register
        public readonly Register SR = new(true, RegisterRWAccess.ReadOnly);
        private const uint SR_FLAG_MASK_OVERFLOW = 0b10000000_00000000_00000000_00000000u;
        private const uint SR_FLAG_MASK_ZERO     = 0b01000000_00000000_00000000_00000000u;
        private const uint SR_FLAG_MASK_NEGATIVE = 0b00100000_00000000_00000000_00000000u;
        private const uint SR_FLAG_MASK_CARRY    = 0b00010000_00000000_00000000_00000000u;
        public bool Status_Overflow
        {
            get => (SR.ValueDirect & SR_FLAG_MASK_OVERFLOW) >> 32 == 1;
            private set => SR.ValueDirect |= ((value ? 1u : 0u) << 32) | SR_FLAG_MASK_OVERFLOW;
        }
        public bool Status_Zero
        {
            get => (SR.ValueDirect & SR_FLAG_MASK_ZERO) >> 31 == 1;
            private set => SR.ValueDirect |= ((value ? 1u : 0u) << 31) | SR_FLAG_MASK_ZERO;
        }
        public bool Status_Negative
        {
            get => (SR.ValueDirect & SR_FLAG_MASK_NEGATIVE) >> 30 == 1;
            private set => SR.ValueDirect |= ((value ? 1u : 0u) << 30) | SR_FLAG_MASK_NEGATIVE;
        }
        public bool Status_Carry
        {
            get => (SR.ValueDirect & SR_FLAG_MASK_CARRY) >> 29 == 1;
            private set => SR.ValueDirect |= ((value ? 1u : 0u) << 29) | SR_FLAG_MASK_CARRY;
        }

        public ALU()
        {
            A = new ALUDataRegister(this);
            B = new ALUDataRegister(this);
            R = new ALUResultRegister(this);
        }

        private uint ComputeResult()
        {
            uint x = A.ValueDirect;
            uint y = B.ValueDirect;

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

            if (@out == 0)
                Status_Zero = true;
            // TODO: implement Overflow, Negative and Carry flags

            return @out;
        }

        public static readonly Dictionary<ALU_Operation, ALU_OpCode> ARITHMETIC_OP_LOOKUP = new()
        {
            { ALU_Operation.TransferA,            new ALU_OpCode(false, false, true, true, false, false) },
            { ALU_Operation.TransferB,            new ALU_OpCode(true, true, false, false, false, false) },
            { ALU_Operation.AdditionSigned,       new ALU_OpCode(false, false, false, false, true, false) },
            { ALU_Operation.AB_SubtractionSigned, new ALU_OpCode(false, true, false, false, true, true) },
            { ALU_Operation.BA_SubtractionSigned, new ALU_OpCode(false, false, false, true, true, true) },
            { ALU_Operation.A_Negative,           new ALU_OpCode(false, false, true, true, true, true) },
            { ALU_Operation.B_Negative,           new ALU_OpCode(true, true, false, false, true, true) },
            { ALU_Operation.A_Increment,          new ALU_OpCode(false, true, true, true, true, true) },
            { ALU_Operation.B_Increment,          new ALU_OpCode(true, true, false, true, true, true) },
            { ALU_Operation.A_Decrement,          new ALU_OpCode(false, false, true, true, true, false) },
            { ALU_Operation.B_Decrement,          new ALU_OpCode(true, true, false, false, true, false) },
            { ALU_Operation.LogicalAND,           new ALU_OpCode(false, false, false, false, false, false) },
            { ALU_Operation.LogicalOR,            new ALU_OpCode(false, true, false, true, false, true) },
            { ALU_Operation.A_LogicalNOT,         new ALU_OpCode(false, false, true, true, false, true) },
            { ALU_Operation.B_LogicalNOT,         new ALU_OpCode(true, true, false, false, false, true) }
        };
    }
}