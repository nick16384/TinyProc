using System.Net.Http.Headers;
using TinyProc.Application;
using TinyProc.Processor;
using TinyProc.Processor.CPU;

namespace TinyProc.Assembling;

public partial class Assembler
{
    private static string StringFromSingleInstruction((uint, uint) instruction)
    {
        Logging.LogDebug($"Instruction words: {instruction.Item1:X8}, {instruction.Item2:X8}");
        Instructions.InstructionType type = Instructions.DetermineInstructionType(instruction.Item1);
        Logging.LogDebug($"Instruction type: {type}");

        return type switch
        {
            Instructions.InstructionType.Register => StringFromRegRegInstruction((Instructions.RegRegInstruction)instruction),
            Instructions.InstructionType.Immediate => StringFromRegImmInstruction((Instructions.RegImmInstruction)instruction),
            Instructions.InstructionType.Jump => StringFromJumpInstruction((Instructions.JumpInstruction)instruction),
            _ => throw new ArgumentException("Decomp internal error: Invalid cast to InstructionType.")
        };
    }

    private static string StringFromRegRegInstruction(Instructions.RegRegInstruction instruction)
    {
        Instructions.Opcode desiredOpcode = instruction.GetOpcode();
        CPU.ALU.ALUOpcode desiredALUOpcode = instruction.R_GetALUOpcode();
        string mnemonic = MnemonicOpcodeMap.First(opcodeAndALUOpcode =>
            {
                return opcodeAndALUOpcode.Value.Item1 == desiredOpcode
                && opcodeAndALUOpcode.Value.Item2 == desiredALUOpcode;
            })
            .Key
            .Item1;
        return string.Format("{0,-7} {1}, {2}",
                mnemonic + GetConditionString(instruction.GetConditional()),
                instruction.R_GetAddressableDestRegCode(),
                instruction.R_GetAddressableSrcRegCode());
    }

    private static string StringFromRegImmInstruction(Instructions.RegImmInstruction instruction)
    {
        Instructions.Opcode desiredOpcode = instruction.GetOpcode();
        CPU.ALU.ALUOpcode desiredALUOpcode = instruction.I_GetALUOpcode();
        Logging.LogDebug($"Opcode: {desiredOpcode} ALU {desiredALUOpcode}");
        string mnemonic = MnemonicOpcodeMap.First(opcodeAndALUOpcode =>
            {
                return opcodeAndALUOpcode.Value.Item1 == desiredOpcode
                && opcodeAndALUOpcode.Value.Item2 == desiredALUOpcode;
            })
            .Key
            .Item1;
        if (mnemonic == "INC" || mnemonic == "DEC")
            return string.Format("{0,-7} {1}",
                mnemonic + GetConditionString(instruction.GetConditional()),
                instruction.I_GetAddressableDestRegCode());
        else
            return string.Format("{0,-7} {1}, 0x{2:X8}",
                mnemonic + GetConditionString(instruction.GetConditional()),
                instruction.I_GetAddressableDestRegCode(),
                instruction.I_GetImmediateValue());
    }

    private static string StringFromJumpInstruction(Instructions.JumpInstruction instruction)
    {
        Instructions.Opcode desiredOpcode = instruction.GetOpcode();
        string mnemonic =
            MnemonicOpcodeMap.First(opcodeAndALUOpcode =>
            {
                return opcodeAndALUOpcode.Value.Item1 == desiredOpcode
                && opcodeAndALUOpcode.Value.Item2 == DEFAULT_EMPTY_ALU_OPCODE;
            })
            .Key
            .Item1;

        if (instruction.GetOpcode() == Instructions.Opcode.NOP)
            return string.Format("{0}",
                mnemonic + GetConditionString(instruction.GetConditional()));
        else
            return string.Format("{0,-7} 0x{1:X8}",
                mnemonic + GetConditionString(instruction.GetConditional()),
                instruction.J_GetJumpTargetAddress());
    }

    private static string GetConditionString(Instructions.Condition condition)
    {
        if (condition != Instructions.Condition.ALWAYS)
            return condition.ToString();
        return "";
    }
}