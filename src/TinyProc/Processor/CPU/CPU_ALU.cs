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
        public readonly struct ALUOpcode((bool, bool, bool, bool, bool, bool) opCodeBits, string name = "")
        {
            private readonly (bool, bool, bool, bool, bool, bool) _opCodeBits = opCodeBits;
            // If true, set x = 0
            public readonly bool zx = opCodeBits.Item1;
            // If true, set x = !x
            public readonly bool nx = opCodeBits.Item2;
            // If true, set y = 0
            public readonly bool zy = opCodeBits.Item3;
            // If true, set y = !y
            public readonly bool ny = opCodeBits.Item4;
            // If true, out = x + y; If false, out = x & y
            public readonly bool f = opCodeBits.Item5;
            // If true, out = !out
            public readonly bool no = opCodeBits.Item6;

            public readonly string _name = name;

            public override readonly string ToString()
            {
                string bitsString =
                    (zx ? "1" : "0") +
                    (nx ? "1" : "0") +
                    (zy ? "1" : "0") +
                    (ny ? "1" : "0") +
                    (f  ? "1" : "0") +
                    (no ? "1" : "0");
                if (string.IsNullOrWhiteSpace(_name))
                    return bitsString;
                else
                    return _name + " / " + bitsString;
            }

            public static explicit operator ALUOpcode((bool, bool, bool, bool, bool, bool) opCodeBits) => new(opCodeBits);
            public static implicit operator (bool, bool, bool, bool, bool, bool)(ALUOpcode opCode) => opCode._opCodeBits;

            // List of all commonly used opcodes; Acts as a kind of enum or Dictionary<OpName, ALUOpcode>.
            public static readonly ALUOpcode TransferA            = new((false, false, true, true, false, false), "TraA");
            public static readonly ALUOpcode TransferB            = new((true, true, false, false, false, false), "TraB");
            public static readonly ALUOpcode Addition             = new((false, false, false, false, true, false), "Add");
            public static readonly ALUOpcode AB_SubtractionSigned = new((false, true, false, false, true, true), "AB_SubSig");
            public static readonly ALUOpcode BA_SubtractionSigned = new((false, false, false, true, true, true), "BA_SubSig");
            public static readonly ALUOpcode A_Negative           = new((false, false, true, true, true, true), "A_Neg");
            public static readonly ALUOpcode B_Negative           = new((true, true, false, false, true, true), "B_Neg");
            public static readonly ALUOpcode A_Increment          = new((false, true, true, true, true, true), "A_Inc");
            public static readonly ALUOpcode B_Increment          = new((true, true, false, true, true, true), "B_Inc");
            public static readonly ALUOpcode A_Decrement          = new((false, false, true, true, true, false), "A_Dec");
            public static readonly ALUOpcode B_Decrement          = new((true, true, false, false, true, false), "B_Dec");
            public static readonly ALUOpcode LogicalAND           = new((false, false, false, false, false, false), "AND");
            public static readonly ALUOpcode LogicalOR            = new((false, true, false, true, false, true), "OR");
            public static readonly ALUOpcode A_LogicalNOT         = new((false, false, true, true, false, true), "A_NOT");
            public static readonly ALUOpcode B_LogicalNOT         = new((true, true, false, false, false, true), "B_NOT");
        }

        public class ALUInputRegister(ALU alu) : Register(true, RegisterRWAccess.ReadWrite)
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

        public readonly ALUInputRegister A;
        public readonly ALUInputRegister B;
        public ALUOpcode CurrentOpCode = new((false, false, false, false, false, false));
        public readonly ALUResultRegister R;

        // Status register
        // Currently independent of internal busses
        public readonly Register SR = new(true, RegisterRWAccess.ReadOnly);
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

            if (CurrentOpCode.zx)
                x = 0x0u;
            if (CurrentOpCode.nx)
                x = ~x;
            if (CurrentOpCode.zy)
                y = 0x0u;
            if (CurrentOpCode.ny)
                y = ~y;

            uint @out;
            if (CurrentOpCode.f)
                @out = x + y;
            else
                @out = x & y;
        
            if (CurrentOpCode.no)
                @out = ~@out;

            // Only override current flags, if "enable flags" flag is set.
            if (Status_EnableFlags)
            {
                Status_Overflow = CurrentOpCode.f && ((ulong)((uint)x + (uint)y) != (ulong)x + (ulong)y);
                Status_Zero     = @out == 0;
                Status_Negative = ((@out & 0x80000000) >> 31) == 1;
                Status_Carry    = Status_Overflow;
            }

            return @out;
        }
    }
}