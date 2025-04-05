namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        public enum OpCode
        {
            NOP   = 0x00,

            JMP   = 0x01,
            B     = 0x02,

            MOV   = 0x03,

            CLZ   = 0x07,
            CLOF  = 0x08,
            CLNG  = 0x09,

            ADD   = 0x10,
            ADDR  = 0x11,
            SUB   = 0x12,
            SUBR  = 0x13,
            MUL   = 0x14,
            MULR  = 0x15,
            AND   = 0x16,
            ANDR  = 0x17,
            OR    = 0x18,
            ORR   = 0x19,
            XOR   = 0x1A,
            XORR  = 0x1B,
            LS    = 0x1C,
            LSR   = 0x1D,
            RS    = 0x1E,
            RSR   = 0x1F,
            SRS   = 0x20,
            SRSR  = 0x21,
            ROL   = 0x22,
            ROLR  = 0x23,
            ROR   = 0x24,
            RORR  = 0x25,

            LOAD  = 0x30,
            LOADR = 0x31,
            STORE = 0x32,
            STORR = 0x33,
        }

        public enum Condition
        {
            ALWAYS = 0x00,
            EQ     = 0x01,
            NE     = 0x02,
            OF     = 0x03,
            NO     = 0x04,
            ZR     = 0x05,
            NZ     = 0x06,
            NG     = 0x07,
            NN     = 0x08
        }

        // Lists all register codes that can appear in an instruction.
        // Mostly same with internal RCODE_* values, however, RCODE values do contain
        // some registers that are inaddressable by an instruction (e.g. MDR), so not all RCODE
        // codes are listed here.
        public enum AddressableRegisterCode
        {
            PC  = 0x00,
            GP1 = 0x01,
            GP2 = 0x02,
            GP3 = 0x03,
            GP4 = 0x04,
            GP5 = 0x05,
            GP6 = 0x06,
            GP7 = 0x07,
            GP8 = 0x08,
            SR  = 0x10
        }
    }
}