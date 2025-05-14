using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        public readonly struct ALUOpcode((bool, bool, bool, bool, bool, bool) opcodeBits)
        {
            private readonly (bool, bool, bool, bool, bool, bool) _opcodeBits = opcodeBits;
            // If true, set x = 0
            public readonly bool zx = opcodeBits.Item1;
            // If true, set x = !x
            public readonly bool nx = opcodeBits.Item2;
            // If true, set y = 0
            public readonly bool zy = opcodeBits.Item3;
            // If true, set y = !y
            public readonly bool ny = opcodeBits.Item4;
            // If true, out = x + y; If false, out = x & y
            public readonly bool f = opcodeBits.Item5;
            // If true, out = !out
            public readonly bool no = opcodeBits.Item6;

            public override readonly string ToString()
            {
                string? name = null;
                string bitsString =
                    (zx ? "1" : "0") +
                    (nx ? "1" : "0") +
                    (zy ? "1" : "0") +
                    (ny ? "1" : "0") +
                    (f  ? "1" : "0") +
                    (no ? "1" : "0");

                if      (_opcodeBits == TransferA._opcodeBits)            name = "TraA";
                else if (_opcodeBits == TransferB._opcodeBits)            name = "TraB";
                else if (_opcodeBits == AB_SubtractionSigned._opcodeBits) name = "AB_SubSigned";
                else if (_opcodeBits == BA_SubtractionSigned._opcodeBits) name = "BA_SubSigned";
                else if (_opcodeBits == A_Negative._opcodeBits)           name = "A_Neg";
                else if (_opcodeBits == B_Negative._opcodeBits)           name = "B_Neg";
                else if (_opcodeBits == A_Increment._opcodeBits)          name = "A_Inc";
                else if (_opcodeBits == B_Increment._opcodeBits)          name = "B_Inc";
                else if (_opcodeBits == A_Decrement._opcodeBits)          name = "A_Dec";
                else if (_opcodeBits == B_Decrement._opcodeBits)          name = "B_Dec";
                else if (_opcodeBits == LogicalAND._opcodeBits)           name = "AND";
                else if (_opcodeBits == LogicalOR._opcodeBits)            name = "OR";
                else if (_opcodeBits == A_LogicalNOT._opcodeBits)         name = "A_NOT";
                else if (_opcodeBits == B_LogicalNOT._opcodeBits)         name = "B_NOT";

                if (name == null)
                    return "UnknownOp / " + bitsString;
                else
                    return name + " / " + bitsString;
            }

            public static explicit operator ALUOpcode((bool, bool, bool, bool, bool, bool) opcodeBits) => new(opcodeBits);
            public static implicit operator (bool, bool, bool, bool, bool, bool)(ALUOpcode opcode) => opcode._opcodeBits;

            // List of all commonly used opcodes; Acts as a kind of enum or Dictionary<OpName, ALUOpcode>.
            public static readonly ALUOpcode TransferA            = new((false, false, true, true, false, false));
            public static readonly ALUOpcode TransferB            = new((true, true, false, false, false, false));
            public static readonly ALUOpcode Addition             = new((false, false, false, false, true, false));
            public static readonly ALUOpcode AB_SubtractionSigned = new((false, true, false, false, true, true));
            public static readonly ALUOpcode BA_SubtractionSigned = new((false, false, false, true, true, true));
            public static readonly ALUOpcode A_Negative           = new((false, false, true, true, true, true));
            public static readonly ALUOpcode B_Negative           = new((true, true, false, false, true, true));
            public static readonly ALUOpcode A_Increment          = new((false, true, true, true, true, true));
            public static readonly ALUOpcode B_Increment          = new((true, true, false, true, true, true));
            public static readonly ALUOpcode A_Decrement          = new((false, false, true, true, true, false));
            public static readonly ALUOpcode B_Decrement          = new((true, true, false, false, true, false));
            public static readonly ALUOpcode LogicalAND           = new((false, false, false, false, false, false));
            public static readonly ALUOpcode LogicalOR            = new((false, true, false, true, false, true));
            public static readonly ALUOpcode A_LogicalNOT         = new((false, false, true, true, false, true));
            public static readonly ALUOpcode B_LogicalNOT         = new((true, true, false, false, false, true));
        }

        public class ALUInputRegister(ALU alu) : Register(0, true)
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

        public class ALUResultRegister(ALU alu) : Register(0, true)
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

        public readonly ALUInputRegister A;
        public readonly ALUInputRegister B;
        public ALUOpcode CurrentOpcode = new((false, false, false, false, false, false));
        public readonly ALUResultRegister R;

        // Status register
        // Currently independent of internal busses
        public readonly Register SR = new(0, true);
        private const uint SR_FLAG_MASK_ENABLE   = 0b10000000_00000000_00000000_00000000u;
        private const uint SR_FLAG_MASK_OVERFLOW = 0b01000000_00000000_00000000_00000000u;
        private const uint SR_FLAG_MASK_ZERO     = 0b00100000_00000000_00000000_00000000u;
        private const uint SR_FLAG_MASK_NEGATIVE = 0b00010000_00000000_00000000_00000000u;
        private const uint SR_FLAG_MASK_CARRY    = 0b00001000_00000000_00000000_00000000u;
        public bool Status_EnableFlags
        {
            get => (SR.ValueDirect & SR_FLAG_MASK_ENABLE) >> 31 == 1;
            set
            {
                // Sets the corresponding bit in the SR
                // If this magic code piece doesn't work, please blame ChatGPT before opening an issue
                SR.ValueDirect = (SR.ValueDirect & ~SR_FLAG_MASK_ENABLE) | ((value ? 0xFFFFFFFF : 0x0) & SR_FLAG_MASK_ENABLE);
            }
        }
        public bool Status_Overflow
        {
            get => (SR.ValueDirect & SR_FLAG_MASK_OVERFLOW) >> 30 == 1;
            set
            {
                SR.ValueDirect = (SR.ValueDirect & ~SR_FLAG_MASK_OVERFLOW) | ((value ? 0xFFFFFFFF : 0x0) & SR_FLAG_MASK_OVERFLOW);
            }
        }
        public bool Status_Zero
        {
            get => (SR.ValueDirect & SR_FLAG_MASK_ZERO) >> 29 == 1;
            set
            {
                SR.ValueDirect = (SR.ValueDirect & ~SR_FLAG_MASK_ZERO) | ((value ? 0xFFFFFFFF : 0x0) & SR_FLAG_MASK_ZERO);
            }
        }
        public bool Status_Negative
        {
            get => (SR.ValueDirect & SR_FLAG_MASK_NEGATIVE) >> 28 == 1;
            set
            {
                SR.ValueDirect = (SR.ValueDirect & ~SR_FLAG_MASK_NEGATIVE) | ((value ? 0xFFFFFFFF : 0x0) & SR_FLAG_MASK_NEGATIVE);
            }
        }
        public bool Status_Carry
        {
            get => (SR.ValueDirect & SR_FLAG_MASK_CARRY) >> 27 == 1;
            set
            {
                SR.ValueDirect = (SR.ValueDirect & ~SR_FLAG_MASK_CARRY) | ((value ? 0xFFFFFFFF : 0x0) & SR_FLAG_MASK_CARRY);
            }
        }

        public ALU()
        {
            A = new ALUInputRegister(this);
            B = new ALUInputRegister(this);
            R = new ALUResultRegister(this);
        }

        private uint ComputeResult()
        {
            uint x = A.ValueDirect;
            uint y = B.ValueDirect;

            if (CurrentOpcode.zx)
                x = 0x0u;
            if (CurrentOpcode.nx)
                x = ~x;
            if (CurrentOpcode.zy)
                y = 0x0u;
            if (CurrentOpcode.ny)
                y = ~y;

            uint @out;
            if (CurrentOpcode.f)
                @out = x + y;
            else
                @out = x & y;
        
            if (CurrentOpcode.no)
                @out = ~@out;

            // Only override current flags, if "enable flags" flag is set.
            if (Status_EnableFlags)
            {
                Status_Overflow = CurrentOpcode.f && ((ulong)((uint)x + (uint)y) != (ulong)x + (ulong)y);
                Status_Zero     = @out == 0;
                Status_Negative = ((@out & 0x80000000) >> 31) == 1;
                Status_Carry    = Status_Overflow;
            }

            return @out;
        }
    }
}