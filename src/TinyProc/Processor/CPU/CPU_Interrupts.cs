using TinyProc.Application;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    // Calls the reset interrupt vector
    public void Reset()
    {
        // Clear some special internal registers not available to assembled code
        // General-purpose registers and the stack pointer are reset in assembly
        _CU.CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SPECIAL_CONST_ZERO, InternalRegisterCode.RCODE_SPECIAL_IRA);
        _CU.CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SPECIAL_CONST_ZERO, InternalRegisterCode.RCODE_SPECIAL_IRB);
        _CU.CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SPECIAL_CONST_ZERO, InternalRegisterCode.RCODE_SPECIAL_MAR);
        _CU.CopyFromRegisterToRegister(InternalRegisterCode.RCODE_SPECIAL_CONST_ZERO, InternalRegisterCode.RCODE_SR);
        _CU.Reset_Hardware();
    }

    private const uint SHIT_BASE_OFFSET = 0x00010000;
    private enum Fault : byte
    {
        RESET = 0x00,
        DOUBLE_FAULT = 0x01,
        UNKNOWN_INSTRUCTION = 0x02,
        INVALID_ADDRESS = 0x03,
        STACK_OVERFLOW = 0x04,
        ILLEGAL_SECURE_MEMORY_WRITE = 0x05,
        DIVISION_BY_ZERO = 0x06
    }

    private static string GetFaultName(Fault fault)
    {
        return fault switch
        {
            Fault.RESET => "Reset",
            Fault.DOUBLE_FAULT => "Double fault",
            Fault.UNKNOWN_INSTRUCTION => "Unknown instruction",
            Fault.INVALID_ADDRESS => "Invalid address",
            Fault.STACK_OVERFLOW => "Stack overflow",
            Fault.ILLEGAL_SECURE_MEMORY_WRITE => "Illegal secure memory write",
            Fault.DIVISION_BY_ZERO => "Division by zero",
            _ => "//Not a hardware fault//"
        };
    }

    private void TriggerHardwareFault(Fault fault)
    {
        switch (fault)
        {
            case Fault.RESET:
                Reset();
                break;

            case Fault.DOUBLE_FAULT
            or Fault.UNKNOWN_INSTRUCTION
            or Fault.INVALID_ADDRESS
            or Fault.STACK_OVERFLOW
            or Fault.ILLEGAL_SECURE_MEMORY_WRITE
            or Fault.DIVISION_BY_ZERO:
                Logging.LogError($"Hardware fault {(byte)fault:x2} / {GetFaultName(fault)} triggered!");
                // TODO: SHIT_BASE_OFFSET + fault vector => MAR; MDR => PC
                throw new NotImplementedException("Hardware fault generated (see log). However, hardware fault handling hasn't been implemented yet.");
                break;

            default:
                throw new Exception($"Invalid fault number {fault:x2}");
        }
    }
}