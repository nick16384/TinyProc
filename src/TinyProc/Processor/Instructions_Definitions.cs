namespace TinyProc.Processor;

public sealed partial class Instructions
{
    // This file contains actual definitions for all supported instructions and condition types,
    // of which the structure was defined in a separate file of the same class.
    // Note: This class contains a lot of pseudo-enums, which act as enums externally,
    // but have implicit conversions implemented.

    public sealed class OpCode
    {
        private static readonly Dictionary<uint, OpCode> _values = [];

        public static readonly OpCode NOP   = new(0x00, "NOP");

        public static readonly OpCode JMP   = new(0x01, "JMP");
        public static readonly OpCode B     = new(0x02, "B");

        public static readonly OpCode AOPR  = new(0x10, "AOPR");
        public static readonly OpCode AOPI  = new(0x11, "AOPI");

        public static readonly OpCode CLZ   = new(0x07, "CLZ");
        public static readonly OpCode CLOF  = new(0x08, "CLOF");
        public static readonly OpCode CLNG  = new(0x09, "CLNG");

        public static readonly OpCode LOAD  = new(0x30, "LOAD");
        public static readonly OpCode LOADR = new(0x31, "LOADR");
        public static readonly OpCode STORE = new(0x32, "STORE");
        public static readonly OpCode STORR = new(0x33, "STORR");

        private readonly uint _value;
        private readonly string _name;
        private OpCode(uint value, string name)
        {
            _value = value;
            _name = name;
            _values.Add(value, this);
        }
        public static implicit operator OpCode(uint value)
        {
            try { return _values[value]; }
            catch (Exception) { throw new KeyNotFoundException($"Invalid OpCode {value:X8}"); }
        }
        public static explicit operator OpCode(string mnemonic)
        {
            mnemonic = mnemonic.ToUpper();
            foreach (OpCode opCode in _values.Values)
                if (opCode._name.Equals(mnemonic))
                    return opCode;
            throw new KeyNotFoundException($"Mnemonic {mnemonic} does not map to a valid OpCode.");
        }
        public static implicit operator uint(OpCode opCode) => opCode._value;

        public override string ToString() => _name;
    }

    public sealed class Condition
    {
        private static readonly Dictionary<uint, Condition> _values = [];

        public static readonly Condition ALWAYS = new(0x00, "ALWAYS");
        public static readonly Condition EQ     = new(0x01, "EQ");
        public static readonly Condition NE     = new(0x02, "NE");
        public static readonly Condition OF     = new(0x03, "OF");
        public static readonly Condition NO     = new(0x04, "NO");
        public static readonly Condition ZR     = new(0x05, "ZR");
        public static readonly Condition NZ     = new(0x06, "NZ");
        public static readonly Condition NG     = new(0x07, "NG");
        public static readonly Condition NN     = new(0x08, "NN");

        private readonly uint _value;
        private readonly string _name;
        private Condition(uint value, string name)
        {
            _value = value;
            _name = name;
            _values.Add(value, this);
        }
        public static implicit operator Condition(uint value)
        {
            try { return _values[value]; }
            catch (Exception) { throw new KeyNotFoundException($"Invalid Conditional {value:X8}"); }
        }
        public static explicit operator Condition(string conditionCode)
        {
            conditionCode = conditionCode.ToUpper();
            foreach (Condition conditional in _values.Values)
                if (conditional._name.Equals(conditionCode))
                    return conditional;
            throw new KeyNotFoundException($"Condition code {conditionCode} does not map to a valid Condition.");
        }
        public static implicit operator uint(Condition opCode) => opCode._value;

        public override string ToString() => _name;
    }

    // Lists all register codes that can appear in an instruction.
    // Mostly same with internal RCODE_* values, however, RCODE values do contain
    // some registers that are inaddressable by an instruction (e.g. MDR), so not all RCODE
    // codes are listed here.
    public sealed class AddressableRegisterCode
    {
        private static readonly Dictionary<uint, AddressableRegisterCode> _values = [];

        public static readonly AddressableRegisterCode PC  = new(0x00, "PC");
        public static readonly AddressableRegisterCode GP1 = new(0x01, "GP1");
        public static readonly AddressableRegisterCode GP2 = new(0x02, "GP2");
        public static readonly AddressableRegisterCode GP3 = new(0x03, "GP3");
        public static readonly AddressableRegisterCode GP4 = new(0x04, "GP4");
        public static readonly AddressableRegisterCode GP5 = new(0x05, "GP5");
        public static readonly AddressableRegisterCode GP6 = new(0x06, "GP6");
        public static readonly AddressableRegisterCode GP7 = new(0x07, "GP7");
        public static readonly AddressableRegisterCode GP8 = new(0x08, "GP8");
        public static readonly AddressableRegisterCode SR  = new(0x10, "SR");

        private readonly uint _value;
        private readonly string _name;
        private AddressableRegisterCode(uint value, string name)
        {
            _value = value;
            _name = name;
            _values.Add(value, this);
        }
        public static implicit operator AddressableRegisterCode(uint value)
        {
            try { return _values[value]; }
            catch (Exception) { throw new KeyNotFoundException($"Invalid register code {value:X8}"); }
        }
        public static explicit operator AddressableRegisterCode(string registerCode)
        {
            registerCode = registerCode.ToUpper();
            foreach (AddressableRegisterCode addressableRegister in _values.Values)
                if (addressableRegister._name.Equals(registerCode))
                    return addressableRegister;
            throw new KeyNotFoundException($"Register code {registerCode} does not map to a valid addressable register.");
        }
        public static implicit operator uint(AddressableRegisterCode opCode) => opCode._value;

        public override string ToString() => _name;
    }
}