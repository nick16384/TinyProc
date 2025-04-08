using TinyProc.Processor;
using static TinyProc.Processor.CPU.CPU;

namespace TinyProc.Assembler;

public class Assembler()
{
    public static uint[] AssembleToMachineCode(string assemblyCode)
    {
        List<string> assemblyLines = [.. assemblyCode.Split("\n")];
        // Filter out comments & Remove empty lines
        assemblyLines = [.. assemblyLines
            .ConvertAll(line => line.Split(";")[0].Trim())
            .Where(line => !string.IsNullOrEmpty(line))];

        Console.WriteLine("===== Assembly begin =====");
        assemblyLines.ForEach(Console.WriteLine);
        Console.WriteLine("=====  Assembly end  =====");

        List<uint> assembledMachineCode = [];

        foreach (string line in assemblyLines)
        {
            Console.WriteLine($"Attempting to parse assembly line: {line}");
            string[] words = line.Split([" ", ","], StringSplitOptions.RemoveEmptyEntries);
            string mnemonic = words[0].ToUpper();
            Instructions.OpCode opCode = (Instructions.OpCode)mnemonic;

            Instructions.Condition conditional = Instructions.Condition.ALWAYS;
            if (mnemonic.Length >= 3)
            {
                string possibleConditionCode = mnemonic[^2..];
                try { conditional = (Instructions.Condition)possibleConditionCode; }
                catch (KeyNotFoundException) { Console.WriteLine($"Mnemonic {mnemonic} has no condition code."); }
            }

            Instructions.InstructionType type = Instructions.DetermineInstructionType(opCode);
            Console.Error.WriteLine($"Type: {type}");
            switch (type)
            {
                case Instructions.InstructionType.Register:
                // Case statements with brackets to create separate scope for each case statement.
                {
                    string operand1 = words[1].ToUpper();
                    string operand2 = words[2].ToUpper();
                    Instructions.AddressableRegisterCode destRegCode = (Instructions.AddressableRegisterCode)operand1;
                    Instructions.AddressableRegisterCode srcRegCode = (Instructions.AddressableRegisterCode)operand2;
                    ALU.ALU_OpCode aluOpCode = new(false, false, false, false, false, false);
                    try { GetALUOpCodeFromInstructionOpCode(opCode); }
                    catch (ArgumentException) {}

                    Instructions.InstructionTypeR instruction = new(opCode, conditional, destRegCode, srcRegCode, aluOpCode);
                    (uint, uint) instructionBinaryTuple = Instructions.ForgeBinaryInstruction(instruction);
                    assembledMachineCode.Add(instructionBinaryTuple.Item1);
                    assembledMachineCode.Add(instructionBinaryTuple.Item2);
                    break;
                }
                
                case Instructions.InstructionType.Immediate:
                {
                    string operand1 = words[1].ToUpper();
                    string immediateStr = words[2];
                    Instructions.AddressableRegisterCode destRegCode = (Instructions.AddressableRegisterCode)operand1;
                    uint immediate = ConvertStringToUInt(immediateStr);
                    ALU.ALU_OpCode aluOpCode = new(false, false, false, false, false, false);
                    try { GetALUOpCodeFromInstructionOpCode(opCode); }
                    catch (ArgumentException) {}

                    Instructions.InstructionTypeI instruction = new(opCode, conditional, destRegCode, aluOpCode, immediate);
                    (uint, uint) instructionBinaryTuple = Instructions.ForgeBinaryInstruction(instruction);
                    assembledMachineCode.Add(instructionBinaryTuple.Item1);
                    assembledMachineCode.Add(instructionBinaryTuple.Item2);
                    break;
                }

                case Instructions.InstructionType.Jump:
                {
                    uint targetJumpAddress = 0x00000000u;
                    if (words.Length > 1)
                    {
                        string targetJumpAddressStr = words[1];
                        targetJumpAddress = ConvertStringToUInt(targetJumpAddressStr);
                        Console.Error.WriteLine($"Jump addr: {targetJumpAddress}");
                    }

                    Instructions.InstructionTypeJ instruction = new(opCode, conditional, targetJumpAddress);
                    (uint, uint) instructionBinaryTuple = Instructions.ForgeBinaryInstruction(instruction);
                    assembledMachineCode.Add(instructionBinaryTuple.Item1);
                    assembledMachineCode.Add(instructionBinaryTuple.Item2);
                    break;
                }
            }
        }

        return [.. assembledMachineCode];
    }

    private static ALU.ALU_OpCode GetALUOpCodeFromInstructionOpCode(Instructions.OpCode opCode)
    {
        if      (opCode == Instructions.OpCode.ADD || opCode == Instructions.OpCode.ADDR)
            return ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
        else if (opCode == Instructions.OpCode.SUB || opCode == Instructions.OpCode.SUBR)
            return ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AB_SubtractionSigned];
        else if (opCode == Instructions.OpCode.AND || opCode == Instructions.OpCode.ANDR)
            return ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalAND];
        else if (opCode == Instructions.OpCode.OR || opCode == Instructions.OpCode.ORR)
            return ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalOR];
        else
            throw new ArgumentException($"OpCode {opCode} has no associated ALU OpCode.");
    }

    // Converts a number string from
    // 1. Base 2 (prefix 0b),
    // 2. Base 10 (no prefix) or
    // 3. Base 16 (prefix 0x)
    // to a uint
    private static uint ConvertStringToUInt(string numStr)
    {
        if (numStr.StartsWith("0b"))
        {
            // Base 2
            return Convert.ToUInt32(numStr, 2);
        }
        else if (numStr.StartsWith("0x"))
        {
            // Base 16
            return Convert.ToUInt32(numStr, 16);
        }
        else
        {
            // Base 10 or unknown
            return Convert.ToUInt32(numStr);
        }
    }
}