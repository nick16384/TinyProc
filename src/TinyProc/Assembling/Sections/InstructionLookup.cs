using TinyProc.Application;
using TinyProc.Processor;
using TinyProc.Processor.CPU;
using static TinyProc.Processor.CPU.CPU;
using static TinyProc.Assembling.Assembler;

namespace TinyProc.Assembling.Sections;

public sealed class InstructionLookup
{
    // Maps instruction mnemonics from a specific type to 1. an Opcode and 2. an optional ALU opcode.
    // This lookup table can both be used by the assembler and the disassembler.
    public static readonly Dictionary<(string, Instructions.InstructionType), (Instructions.Opcode, CPU.ALU.ALUOpcode)> MnemonicOpcodeMap = new()
    {
        { ("NOP",   Instructions.InstructionType.Jump),      (Instructions.Opcode.NOP,   DEFAULT_EMPTY_ALU_OPCODE) },
        { ("MOV",   Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.TransferB) },
        { ("MOV",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.TransferA) },
        { ("ADD",   Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.Addition) },
        { ("ADD",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.Addition) },
        { ("SUB",   Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.AB_SubtractionSigned) },
        { ("SUB",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.BA_SubtractionSigned) },
		{ ("INC",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.B_Increment) },
		{ ("DEC",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.B_Decrement) },
		{ ("AND",   Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.LogicalAND) },
        { ("AND",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.LogicalAND) },
        { ("OR",    Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.LogicalOR) },
        { ("OR",    Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.LogicalOR) },
        { ("LOADR", Instructions.InstructionType.Register),  (Instructions.Opcode.LOADR, DEFAULT_EMPTY_ALU_OPCODE) },
        { ("LOAD",  Instructions.InstructionType.Immediate), (Instructions.Opcode.LOAD,  DEFAULT_EMPTY_ALU_OPCODE) },
        { ("STORR", Instructions.InstructionType.Register),  (Instructions.Opcode.STORR, DEFAULT_EMPTY_ALU_OPCODE) },
        { ("STORE", Instructions.InstructionType.Immediate), (Instructions.Opcode.STORE, DEFAULT_EMPTY_ALU_OPCODE) },
        { ("JMP",   Instructions.InstructionType.Jump),      (Instructions.Opcode.JMP,   DEFAULT_EMPTY_ALU_OPCODE) },
        { ("B",     Instructions.InstructionType.Jump),      (Instructions.Opcode.B,     DEFAULT_EMPTY_ALU_OPCODE) }
    };

	internal static Instructions.IInstruction ParseAsInstruction(string[] words)
	{
		string mnemonic = words[0].ToUpper();

		Instructions.Condition conditional = Instructions.Condition.ALWAYS;
		if (mnemonic.Length >= 3)
		{
			string possibleConditionCode = mnemonic[^2..];
			try
			{
				conditional = (Instructions.Condition)possibleConditionCode;
				// Mnemonic has conditional at this point
				Logging.LogDebug($"Mnemonic {mnemonic} has condition code {conditional}");
				mnemonic = mnemonic[..^2];
				words[0] = mnemonic;
			}
			catch (KeyNotFoundException) { Logging.LogDebug($"Mnemonic {mnemonic} has no condition code."); }
		}

		// Determine instruction type (R/I/J)
		Instructions.InstructionType type;
		bool isWord2ParseableAsUInt = true;
		try { ConvertStringToUInt(words[2]); } catch (Exception) { isWord2ParseableAsUInt = false; }
		if (mnemonic == "MOV" && isWord2ParseableAsUInt) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "MOV" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "ADD" && isWord2ParseableAsUInt) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "ADD" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "SUB" && isWord2ParseableAsUInt) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "SUB" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "INC") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "DEC") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "AND" && isWord2ParseableAsUInt) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "AND" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "OR" && isWord2ParseableAsUInt) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "OR" && !isWord2ParseableAsUInt) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "LOAD") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "LOADR") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "STORE") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "STORR") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "NOP") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "JMP") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "B") { type = Instructions.InstructionType.Jump; }
		else { throw new ArgumentException("Cannot determine instruction type."); }

		Logging.LogDebug($"Type: {type}");
		return type switch
		{
			Instructions.InstructionType.Register => RegRegInstructionLookup(words, conditional),
			Instructions.InstructionType.Immediate => RegImmInstructionLookup(words, conditional),
			Instructions.InstructionType.Jump => JumpInstructionLookup(words, conditional),
			_ => throw new ArgumentException($"Line {string.Join(" ", words)} does not parse as an instruction.")
		};
	}

    private static Instructions.RegRegInstruction RegRegInstructionLookup(
		string[] words, Instructions.Condition conditional)
	{
		string mnemonic = words[0].ToUpper();
		Instructions.Opcode opcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Register)].Item1;
		CPU.ALU.ALUOpcode aluOpcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Register)].Item2;

		return mnemonic switch
		{
			"MOV" or "ADD" or "SUB" or "AND" or "OR" or "LOADR" or "STORR"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)words[1],
					(Instructions.AddressableRegisterCode)words[2],
					aluOpcode),

			// No INC / DEC; They are exclusively immediate type instructions

			_ => throw new ArgumentException($"Lookup for instruction mnemonic {mnemonic} failed. No associated R-Type instruction.")
		};
	}

    // Some immediate type instructions don't receive any immediate values. (e.g. INC)
    // The default value passed in the instruction is ignored in this case.
    public const uint IMMEDIATE_DEFAULT_VALUE = 0x0u;
    // When an instruction changes the ALU Opcode multiple times during execution, setting this has no effect.
    // This specifies the default value for such cases.
    public static readonly ALU.ALUOpcode DEFAULT_EMPTY_ALU_OPCODE = new((false, false, false, false, false, false));

    private static Instructions.RegImmInstruction RegImmInstructionLookup(
        string[] words, Instructions.Condition conditional)
    {
        string mnemonic = words[0].ToUpper();
		Instructions.Opcode opcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Immediate)].Item1;
		CPU.ALU.ALUOpcode aluOpcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Immediate)].Item2;

        return mnemonic switch
		{
			"MOV" or "ADD" or "SUB" or "AND" or "OR" or "LOAD" or "STORE"
				=> new Instructions.RegImmInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)words[1],
					aluOpcode,
					ConvertStringToUInt(words[2])),
			
			// For LOAD and STORE instructions:
			// Multiple ALU opcodes required during execution; Therefore, none are set here.

			"INC" or "DEC"
				=> new Instructions.RegImmInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)words[1],
					aluOpcode,
					IMMEDIATE_DEFAULT_VALUE),

			_ => throw new ArgumentException($"Lookup for instruction mnemonic {mnemonic} failed. No associated I-Type instruction.")
		};
    }

    private static Instructions.JumpInstruction JumpInstructionLookup(
        string[] words, Instructions.Condition conditional)
    {
        string mnemonic = words[0].ToUpper();
		Instructions.Opcode opcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Jump)].Item1;
		CPU.ALU.ALUOpcode aluOpcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Jump)].Item2;

        return mnemonic switch
		{
			"NOP" => new Instructions.JumpInstruction(
					opcode,
					conditional,
					IMMEDIATE_DEFAULT_VALUE),

			"JMP" or "B"
				=> new Instructions.JumpInstruction(
					opcode,
					conditional,
					ConvertStringToUInt(words[1])),

			_ => throw new ArgumentException($"Lookup for instruction mnemonic {mnemonic} failed. No associated J-Type instruction.")
		};
    }
}