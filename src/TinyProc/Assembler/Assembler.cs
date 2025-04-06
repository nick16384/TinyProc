using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
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
            string mnemonic = line.Split(" ")[0].ToUpper();
            Instructions.OpCode opCode = (Instructions.OpCode)mnemonic;

            Instructions.Condition conditional = Instructions.Condition.ALWAYS;
            if (mnemonic.Length >= 3)
            {
                string possibleConditionCode = mnemonic[^2..];
                try { conditional = (Instructions.Condition)possibleConditionCode; }
                catch (KeyNotFoundException) { Console.WriteLine($"Mnemonic {mnemonic} has no condition code."); }
            }

            Instructions.InstructionType type = Instructions.DetermineInstructionType(opCode);
            switch (type)
            {
                case Instructions.InstructionType.Register:
                    string operand1 = line.Split(" ")[1].Split(",")[0].Trim().ToUpper();
                    string operand2 = line.Split(",")[1].Trim().ToUpper();
                    Instructions.AddressableRegisterCode destRegCode = (Instructions.AddressableRegisterCode)operand1;
                    Instructions.AddressableRegisterCode srcRegCode = (Instructions.AddressableRegisterCode)operand2;
                    ALU.ALU_OpCode aluOpCode = GetALUOpCodeFromInstructionOpCode(opCode);

                    Instructions.InstructionTypeR instruction = new(opCode, conditional, destRegCode, srcRegCode, aluOpCode);
                    (uint, uint) instructionBinaryTuple = Instructions.ForgeBinaryInstruction(instruction);
                    assembledMachineCode.Add(instructionBinaryTuple.Item1);
                    assembledMachineCode.Add(instructionBinaryTuple.Item2);

                    break;
                
                case Instructions.InstructionType.Immediate:
                    // TODO: Implement
            }
        }

        return [];
    }

    private static ALU.ALU_OpCode GetALUOpCodeFromInstructionOpCode(Instructions.OpCode opCode)
    {
        ALU.ALU_OpCode aluOpCode;
            if      (opCode == Instructions.OpCode.ADD || opCode == Instructions.OpCode.ADDR)
                return ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned];
            else if (opCode == Instructions.OpCode.SUB || opCode == Instructions.OpCode.SUBR)
                return ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AB_SubtractionSigned];
                // TODO Finish this
            else
                throw new ArgumentException($"OpCode {opCode} has no associated ALU OpCode.");
        return aluOpCode;
    }
}