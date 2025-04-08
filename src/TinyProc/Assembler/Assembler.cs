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
            string[] words = line.Split([" ", ","], StringSplitOptions.RemoveEmptyEntries);
            string mnemonic = words[0].ToUpper();

            Instructions.Condition conditional = Instructions.Condition.ALWAYS;
            if (mnemonic.Length >= 3)
            {
                string possibleConditionCode = mnemonic[^2..];
                try { conditional = (Instructions.Condition)possibleConditionCode; }
                catch (KeyNotFoundException) { Console.WriteLine($"Mnemonic {mnemonic} has no condition code."); }
            }

            Instructions.InstructionType type;
            if (words.Length <= 2)
            {
                if (mnemonic != "INC" && mnemonic != "DEC")
                    type = Instructions.InstructionType.Jump;
                else
                {
                    // Dirty workaround
                    // TODO: Clean up
                    // TODO: Fix INC / DEC instructions to 1. work and 2. use the ALUs capability to directly increment / decrement
                    // or maybe use internal +1 -r -1 registers
                    type = Instructions.InstructionType.Immediate;
                    words = [.. words, "0x0"];
                }
            }
            else
            {
                bool parseOp2AsUIntError = false;
                try { ConvertStringToUInt(words[2]); }
                catch (FormatException) { parseOp2AsUIntError = true; }
                if (parseOp2AsUIntError)
                    type = Instructions.InstructionType.Register;
                else
                    type = Instructions.InstructionType.Immediate;
            }
            Instructions.OpCode opCode = GetOpCodeFromMnemonic(mnemonic, type);

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
                    ALU.ALU_OpCode aluOpCode = GetALUOpCodeFromInstructionMnemonic(mnemonic);

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
                    ALU.ALU_OpCode aluOpCode = GetALUOpCodeFromInstructionMnemonic(mnemonic);

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

    private static Instructions.OpCode GetOpCodeFromMnemonic(string mnemonic, Instructions.InstructionType instructionType)
    {
        if (
            mnemonic == "MOV" ||
            mnemonic == "ADD" ||
            mnemonic == "SUB" ||
            mnemonic == "INC" ||
            mnemonic == "DEC" ||
            mnemonic == "AND" ||
            mnemonic == "OR")
        {
            if (instructionType == Instructions.InstructionType.Immediate)
                return Instructions.OpCode.AOPI;
            else
                return Instructions.OpCode.AOPR;
        }

        if (mnemonic == "STORE") { return Instructions.OpCode.STORE; }
        if (mnemonic == "STORR") { return Instructions.OpCode.STORR; }
        if (mnemonic == "LOAD")  { return Instructions.OpCode.LOAD; }
        if (mnemonic == "LOADR") { return Instructions.OpCode.LOADR; }

        if (mnemonic == "NOP")   { return Instructions.OpCode.NOP; }
        if (mnemonic == "JMP")   { return Instructions.OpCode.JMP; }
        if (mnemonic == "B")     { return Instructions.OpCode.B; }

        throw new ArgumentException($"Mnemonic {mnemonic} has no associated Opcode.");
    }

    private static ALU.ALU_OpCode GetALUOpCodeFromInstructionMnemonic(string mnemonic)
    {
        if (mnemonic == "STORE" || mnemonic == "STORR" || mnemonic == "LOAD" || mnemonic == "LOADR")
            return new ALU.ALU_OpCode(false, false, false, false, false, false);
        return mnemonic switch
        {
            "MOV" => ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA],
            "ADD" => ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned],
            "SUB" => ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AB_SubtractionSigned],
            "INC" => ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.A_Increment],
            "DEC" => ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.A_Decrement],
            "AND" => ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalAND],
            "OR"  => ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalOR],
            _ => throw new ArgumentException($"Mnemonic {mnemonic} has no associated ALU Opcode."),
        };
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