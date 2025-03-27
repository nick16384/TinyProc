using System.Diagnostics.Contracts;

namespace TinyProc.Processor;

public partial class CPU
{
    public partial class ControlUnit
    {
        public enum OpCode
        {
            NOP,

            JMP,
            B,

            MOV,

            CLZ,
            CLOF,
            CLNG,

            ADD,
            ADDR,
            SUB,
            SUBR,
            MUL,
            MULR,
            AND,
            ANDR,
            OR,
            ORR,
            XOR,
            XORR,
            LS,
            LSR,
            RS,
            RSR,
            SRS,
            SRSR,
            ROL,
            ROLR,
            ROR,
            RORR,

            LOAD,
            LOADR,
            STORE,
            STORR
        }
        public static readonly Dictionary<byte, OpCode> INSTRUCTION_SIXBIT_OPCODE_DICT = new()
        {
            { 0x00, OpCode.NOP   },

            { 0x01, OpCode.JMP   },
            { 0x02, OpCode.B     },

            { 0x03, OpCode.MOV   },

            { 0x07, OpCode.CLZ   },
            { 0x08, OpCode.CLOF  },
            { 0x09, OpCode.CLNG  },

            { 0x10, OpCode.ADD   },
            { 0x11, OpCode.ADDR  },
            { 0x12, OpCode.SUB   },
            { 0x13, OpCode.SUBR  },
            { 0x14, OpCode.MUL   },
            { 0x15, OpCode.MULR  },
            { 0x16, OpCode.AND   },
            { 0x17, OpCode.ANDR  },
            { 0x18, OpCode.OR    },
            { 0x19, OpCode.ORR   },
            { 0x1A, OpCode.XOR   },
            { 0x1B, OpCode.XORR  },
            { 0x1C, OpCode.LS    },
            { 0x1D, OpCode.LSR   },
            { 0x1E, OpCode.RS    },
            { 0x1F, OpCode.RSR   },
            { 0x20, OpCode.SRS   },
            { 0x21, OpCode.SRSR  },
            { 0x22, OpCode.ROL   },
            { 0x23, OpCode.ROLR  },
            { 0x24, OpCode.ROR   },
            { 0x25, OpCode.RORR  },

            { 0x30, OpCode.LOAD  },
            { 0x31, OpCode.LOADR },
            { 0x32, OpCode.STORE },
            { 0x33, OpCode.STORR }
        };

        public enum Condition
        {
            ALWAYS,
            EQ,
            NE,
            OF,
            NO,
            ZR,
            NZ,
            NG,
            NN
        }
        public static readonly Dictionary<byte, Condition> INSTRUCTION_FOURBIT_CONDITIONAL_DICT = new()
        {
            { 0x00, Condition.ALWAYS },
            { 0x01, Condition.EQ     },
            { 0x02, Condition.NE     },
            { 0x03, Condition.OF     },
            { 0x04, Condition.NO     },
            { 0x05, Condition.ZR     },
            { 0x06, Condition.NZ     },
            { 0x07, Condition.NG     },
            { 0x08, Condition.NN     }
        };
    }
}