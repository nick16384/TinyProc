using TinyProc.Application;
using TinyProc.Processor;

namespace TinyProc.Assembler;

public partial class Assembler
{
    private static string StringFromInstructionWords((uint, uint) instruction)
    {
        Instructions.InstructionType type = Instructions.DetermineInstructionType(instruction.Item1);
        Logging.LogDebug($"Instruction type: {type}");

        return (type) switch
        {
            Instructions.InstructionType.Register => StringFromRegRegInstruction((Instructions.RegRegInstruction)instruction),
            Instructions.InstructionType.Immediate => StringFromRegImmInstruction((Instructions.RegImmInstruction)instruction),
            Instructions.InstructionType.Jump => StringFromJumpInstruction((Instructions.JumpInstruction)instruction)
        };
    }

    private static string StringFromRegRegInstruction(Instructions.RegRegInstruction instruction)
    {
        if (instruction.Opcode == Instructions.Opcode.NOP)
            return "NOP";
        else
            throw new ArgumentException($"Unknown opcode {instruction.Opcode:X8}");
    }

    private static string StringFromRegImmInstruction(Instructions.RegImmInstruction instruction)
    {

    }
    
    private static string StringFromJumpInstruction(Instructions.JumpInstruction instruction)
    {

    }
}