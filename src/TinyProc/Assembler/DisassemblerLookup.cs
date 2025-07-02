using System.Net.Http.Headers;
using TinyProc.Application;
using TinyProc.Processor;
using TinyProc.Processor.CPU;

namespace TinyProc.Assembler;

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
        return $"{mnemonic}{GetConditionString(instruction.GetConditional())} " +
            $"{instruction.R_GetAddressableDestRegCode()}, {instruction.R_GetAddressableSrcRegCode()}";
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
            return $"{mnemonic}{GetConditionString(instruction.GetConditional())} " +
                $"{instruction.I_GetAddressableDestRegCode()}";
        else
            return $"{mnemonic}{GetConditionString(instruction.GetConditional())} " +
                $"{instruction.I_GetAddressableDestRegCode()}, 0x{instruction.I_GetImmediateValue():X8}";
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
            return $"{mnemonic}{instruction.GetConditional()}";
        else
            return $"{mnemonic}{GetConditionString(instruction.GetConditional())} " +
                $"0x{instruction.J_GetJumpTargetAddress():X8}";
    }

    private static string GetConditionString(Instructions.Condition condition)
    {
        if (condition == Instructions.Condition.ALWAYS)
            return "";
        else
            return condition.ToString();
    }
}