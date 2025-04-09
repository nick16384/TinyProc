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

            // Determine instruction type (R/I/J)
            Instructions.InstructionType? type = null;
            bool isWord2ParseableAsUInt = true;
            try { ConvertStringToUInt(words[2]); } catch (Exception) { isWord2ParseableAsUInt = false; }
            if      (mnemonic == "MOV" && isWord2ParseableAsUInt)  { type = Instructions.InstructionType.Immediate; }
            else if (mnemonic == "MOV" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
            else if (mnemonic == "ADD" && isWord2ParseableAsUInt)  { type = Instructions.InstructionType.Immediate; }
            else if (mnemonic == "ADD" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
            else if (mnemonic == "SUB" && isWord2ParseableAsUInt)  { type = Instructions.InstructionType.Immediate; }
            else if (mnemonic == "SUB" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
            else if (mnemonic == "INC" && isWord2ParseableAsUInt)  { type = Instructions.InstructionType.Immediate; }
            else if (mnemonic == "DEC" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
            else if (mnemonic == "AND" && isWord2ParseableAsUInt)  { type = Instructions.InstructionType.Immediate; }
            else if (mnemonic == "AND" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
            else if (mnemonic == "OR" && isWord2ParseableAsUInt)   { type = Instructions.InstructionType.Immediate; }
            else if (mnemonic == "OR" && !isWord2ParseableAsUInt)  { type = Instructions.InstructionType.Register; }
            else if (mnemonic == "LOAD")  { type = Instructions.InstructionType.Immediate; }
            else if (mnemonic == "LOADR") { type = Instructions.InstructionType.Register; }
            else if (mnemonic == "STORE") { type = Instructions.InstructionType.Immediate; }
            else if (mnemonic == "STORR") { type = Instructions.InstructionType.Register; }
            else if (mnemonic == "NOP")   { type = Instructions.InstructionType.Jump; }
            else if (mnemonic == "JMP")   { type = Instructions.InstructionType.Jump; }
            else if (mnemonic == "B")     { type = Instructions.InstructionType.Jump; }

            Console.Error.WriteLine($"Type: {type}");
            switch (type)
            {
                case Instructions.InstructionType.Register:
                // Case statements with brackets to create separate scope for each case statement.
                {
                    Instructions.InstructionTypeR instruction = InstructionTypeRLookup(words, conditional);
                    (uint, uint) instructionBinaryTuple = Instructions.ForgeBinaryInstruction(instruction);
                    assembledMachineCode.Add(instructionBinaryTuple.Item1);
                    assembledMachineCode.Add(instructionBinaryTuple.Item2);
                    break;
                }
                
                case Instructions.InstructionType.Immediate:
                {
                    Instructions.InstructionTypeI instruction = InstructionTypeILookup(words, conditional);
                    (uint, uint) instructionBinaryTuple = Instructions.ForgeBinaryInstruction(instruction);
                    assembledMachineCode.Add(instructionBinaryTuple.Item1);
                    assembledMachineCode.Add(instructionBinaryTuple.Item2);
                    break;
                }

                case Instructions.InstructionType.Jump:
                {
                    Instructions.InstructionTypeJ instruction = InstructionTypeJLookup(words, conditional);
                    (uint, uint) instructionBinaryTuple = Instructions.ForgeBinaryInstruction(instruction);
                    assembledMachineCode.Add(instructionBinaryTuple.Item1);
                    assembledMachineCode.Add(instructionBinaryTuple.Item2);
                    break;
                }
            }
        }

        return [.. assembledMachineCode];
    }

    private static Instructions.InstructionTypeR InstructionTypeRLookup(
        string[] words, Instructions.Condition conditional)
    {
        string mnemonic = words[0].ToUpper();
        return mnemonic switch
        {
            "MOV" => new Instructions.InstructionTypeR(
                    Instructions.OpCode.AOPR,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    (Instructions.AddressableRegisterCode)words[2],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferB]),

            "ADD" => new Instructions.InstructionTypeR(
                    Instructions.OpCode.AOPR,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    (Instructions.AddressableRegisterCode)words[2],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned]),
            
            "SUB" => new Instructions.InstructionTypeR(
                    Instructions.OpCode.AOPR,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    (Instructions.AddressableRegisterCode)words[2],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AB_SubtractionSigned]),

            // No INC / DEC; They are exclusively immediate type instructions
            
            "AND" => new Instructions.InstructionTypeR(
                    Instructions.OpCode.AOPR,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    (Instructions.AddressableRegisterCode)words[2],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalAND]),

            "OR" => new Instructions.InstructionTypeR(
                    Instructions.OpCode.AOPR,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    (Instructions.AddressableRegisterCode)words[2],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalOR]),

            "LOADR" => new Instructions.InstructionTypeR(
                    Instructions.OpCode.LOADR,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    (Instructions.AddressableRegisterCode)words[2],
                    DEFAULT_EMPTY_ALU_OPCODE),
            
            "STORR" => new Instructions.InstructionTypeR(
                    Instructions.OpCode.STORR,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    (Instructions.AddressableRegisterCode)words[2],
                    DEFAULT_EMPTY_ALU_OPCODE),
            _ => throw new ArgumentException("Lookup for instruction mnemonic {mnemonic} failed. No associated R-Type instruction.")
        };
    }

    // Some immediate type instructions don't receive any immediate values. (e.g. INC)
    // The default value passed in the instruction is ignored in this case.
    private const uint IMMEDIATE_DEFAULT_VALUE = 0x0u;
    // When an instruction changes the ALU Opcode multiple times during execution, setting this has no effect.
    // This specifies the default value for such cases.
    private static readonly ALU.ALU_OpCode DEFAULT_EMPTY_ALU_OPCODE = new(true, false, true, false, false, false);

    private static Instructions.InstructionTypeI InstructionTypeILookup(
        string[] words, Instructions.Condition conditional)
    {
        string mnemonic = words[0].ToUpper();
        return mnemonic switch
        {
            "MOV" => new Instructions.InstructionTypeI(
                    Instructions.OpCode.AOPI,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.TransferA],
                    ConvertStringToUInt(words[2])),

            "ADD" => new Instructions.InstructionTypeI(
                    Instructions.OpCode.AOPI,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.AdditionSigned],
                    ConvertStringToUInt(words[2])),
            
            "SUB" => new Instructions.InstructionTypeI(
                    Instructions.OpCode.AOPI,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.BA_SubtractionSigned],
                    ConvertStringToUInt(words[2])),

            "INC" => new Instructions.InstructionTypeI(
                    Instructions.OpCode.AOPI,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.B_Increment],
                    IMMEDIATE_DEFAULT_VALUE),
            
            "DEC" => new Instructions.InstructionTypeI(
                    Instructions.OpCode.AOPI,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.B_Decrement],
                    IMMEDIATE_DEFAULT_VALUE),
            
            "AND" => new Instructions.InstructionTypeI(
                    Instructions.OpCode.AOPI,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalAND],
                    ConvertStringToUInt(words[2])),

            "OR" => new Instructions.InstructionTypeI(
                    Instructions.OpCode.AOPI,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    ALU.ARITHMETIC_OP_LOOKUP[ALU.ALU_Operation.LogicalOR],
                    ConvertStringToUInt(words[2])),

            "LOAD" => new Instructions.InstructionTypeI(
                    Instructions.OpCode.LOAD,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    DEFAULT_EMPTY_ALU_OPCODE, // Multiple ALU opcodes required during execution; None set here.
                    ConvertStringToUInt(words[2])),

            "STORE" => new Instructions.InstructionTypeI(
                    Instructions.OpCode.STORE,
                    conditional,
                    (Instructions.AddressableRegisterCode)words[1],
                    DEFAULT_EMPTY_ALU_OPCODE, // Multiple ALU opcodes required during execution; None set here.
                    ConvertStringToUInt(words[2])),
            _ => throw new ArgumentException("Lookup for instruction mnemonic {mnemonic} failed. No associated I-Type instruction.")
        };
    }

    private static Instructions.InstructionTypeJ InstructionTypeJLookup(
        string[] words, Instructions.Condition conditional)
    {
        string mnemonic = words[0].ToUpper();
        return mnemonic switch
        {
            "NOP" => new Instructions.InstructionTypeJ(
                    Instructions.OpCode.NOP,
                    conditional,
                    IMMEDIATE_DEFAULT_VALUE),

            "JMP" => new Instructions.InstructionTypeJ(
                    Instructions.OpCode.JMP,
                    conditional,
                    ConvertStringToUInt(words[1])),

            "B" => new Instructions.InstructionTypeJ(
                    Instructions.OpCode.B,
                    conditional,
                    ConvertStringToUInt(words[1])),
            _ => throw new ArgumentException("Lookup for instruction mnemonic {mnemonic} failed. No associated J-Type instruction.")
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