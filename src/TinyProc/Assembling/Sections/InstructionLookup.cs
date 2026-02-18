using TinyProc.Application;
using TinyProc.Processor;
using TinyProc.Processor.CPU;
using static TinyProc.Processor.CPU.CPU;
using static TinyProc.Assembling.Assembler;
using System.Text.RegularExpressions;

namespace TinyProc.Assembling.Sections;

public sealed class InstructionLookup
{
    // Maps instruction mnemonics from a specific type to 1. an Opcode and 2. an optional ALU opcode.
    // This lookup table can both be used by the assembler and the disassembler.
    public static readonly Dictionary<(string, Instructions.InstructionType), (Instructions.Opcode, CPU.ALU.ALUOpcode)> MnemonicOpcodeMap = new()
	{
		{ ("NOP",   Instructions.InstructionType.Jump),      (Instructions.Opcode.NOP,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("MOV",   Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.TransferB) },
		{ ("MOV",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.TransferB) },
		{ ("ADD",   Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.Addition) },
		{ ("ADD",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.Addition) },
		{ ("SUB",   Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.AB_SubtractionSigned) },
		{ ("SUB",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.AB_SubtractionSigned) },
		{ ("INC",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.A_Increment) },
		{ ("DEC",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.A_Decrement) },
		{ ("AND",   Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.LogicalAND) },
		{ ("AND",   Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.LogicalAND) },
		{ ("OR",    Instructions.InstructionType.Register),  (Instructions.Opcode.AOPR,  CPU.ALU.ALUOpcode.LogicalOR) },
		{ ("OR",    Instructions.InstructionType.Immediate), (Instructions.Opcode.AOPI,  CPU.ALU.ALUOpcode.LogicalOR) },

		{ ("LD",    Instructions.InstructionType.Immediate), (Instructions.Opcode.LD,    DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("LDR",   Instructions.InstructionType.Register),  (Instructions.Opcode.LDR,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("ST",   Instructions.InstructionType.Immediate),  (Instructions.Opcode.ST,    DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("STR",  Instructions.InstructionType.Register),   (Instructions.Opcode.STR,   DEFAULT_EMPTY_ALU_OPCODE) },

		{ ("PUSH",  Instructions.InstructionType.Register),  (Instructions.Opcode.PUSH,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("POP",   Instructions.InstructionType.Register),  (Instructions.Opcode.POP,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("JMP",   Instructions.InstructionType.Jump),      (Instructions.Opcode.JMP,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("B",     Instructions.InstructionType.Jump),      (Instructions.Opcode.JMP,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CALL",  Instructions.InstructionType.Jump),      (Instructions.Opcode.CALL,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CALLR", Instructions.InstructionType.Register),  (Instructions.Opcode.CALLR, DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("RET",   Instructions.InstructionType.Jump),      (Instructions.Opcode.RET,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("INT",   Instructions.InstructionType.Jump),      (Instructions.Opcode.INT,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("IRET",  Instructions.InstructionType.Jump),      (Instructions.Opcode.IRET,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("TST",   Instructions.InstructionType.Register),  (Instructions.Opcode.TST,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CLC",   Instructions.InstructionType.Register),  (Instructions.Opcode.CLC,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CLZ",   Instructions.InstructionType.Register),  (Instructions.Opcode.CLZ,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CLOF",  Instructions.InstructionType.Register),  (Instructions.Opcode.CLOF,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CLNG",  Instructions.InstructionType.Register),  (Instructions.Opcode.CLNG,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CLA",   Instructions.InstructionType.Register),  (Instructions.Opcode.CLA,   DEFAULT_EMPTY_ALU_OPCODE) }
    };

	internal static bool IsJumpInstruction(string[] words)
	{
		return
			words[0].StartsWith("JMP", StringComparison.OrdinalIgnoreCase) ||
			words[0].StartsWith("B", StringComparison.OrdinalIgnoreCase) ||
			words[0].StartsWith("CALL", StringComparison.OrdinalIgnoreCase);
	}
	internal static bool IsLoadStoreInstruction(string[] words)
	{
		return
			words[0].StartsWith("LD", StringComparison.OrdinalIgnoreCase) ||
			words[0].StartsWith("LDR", StringComparison.OrdinalIgnoreCase) ||
			words[0].StartsWith("ST", StringComparison.OrdinalIgnoreCase) ||
			words[0].StartsWith("STR", StringComparison.OrdinalIgnoreCase);
	}
	internal static Instructions.AddressingMode? GetAddressingMode(string[] words)
	{
		bool isJump = IsJumpInstruction(words);
		bool isLoadStore = IsLoadStoreInstruction(words);
		if((isJump || isLoadStore) && Regex.IsMatch(words.Last(), @"\[pc \+ .*?\]"))
			return Instructions.AddressingMode.PCRelative;
		else if ((isJump || isLoadStore) && Regex.IsMatch(words.Last(), @"\[.*?\]"))
			return Instructions.AddressingMode.Absolute;
		return null;
	}

	internal static Instructions.IInstruction ParseAsInstruction(string[] words, Instructions.AddressingMode? adrMode)
	{
		// Strip addressing brackets (e.g. in "ld gp1, [0x00000000]")
		if (adrMode.HasValue)
			words[^1] = words[^1][1..^1];
		// Set default addressing mode, even if the instruction doesn't need it
		adrMode ??= Instructions.AddressingMode.Absolute;
		
		string mnemonic = words[0].ToUpper();

		Instructions.Condition conditional = Instructions.Condition.ALWAYS;
		if (mnemonic.Length >= 3)
		{
			string possibleConditionCode = mnemonic[^2..];
			try
			{
				Logging.LogDebug($"Mnemonic: {mnemonic}, searching condition code: {possibleConditionCode}");
				conditional = (Instructions.Condition)possibleConditionCode;
				// Mnemonic has conditional at this point
				Logging.LogDebug($"Mnemonic {mnemonic} has condition code {conditional}");
				mnemonic = mnemonic[..^2];
				words[0] = mnemonic;
			}
			catch (KeyNotFoundException) { Logging.LogDebug($"Mnemonic {mnemonic} has no condition code."); }
		}
		else
			Logging.LogDebug($"Mnemonic {mnemonic} has no condition code.");

		// Determine instruction type (R/I/J)
		Instructions.InstructionType type;
		bool isWord2ParseableAsUInt = true;
		try { ConvertStringToUInt(words[2]); } catch (Exception) { isWord2ParseableAsUInt = false; }
		if (mnemonic == "TST") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLC") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLZ") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLOF") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLNG") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLA") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "MOV" && isWord2ParseableAsUInt) { type = Instructions.InstructionType.Immediate; }
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
		else if (mnemonic == "LD") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "LDR") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "ST") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "STR") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "PUSH") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "POP") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "NOP") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "B") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "JMP") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "CALL") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "CALLR") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "RET") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "INT") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "IRET") { type = Instructions.InstructionType.Jump; }
		else { throw new ArgumentException("Cannot determine instruction type."); }

		Logging.LogDebug($"Type: {type}");
		return type switch
		{
			Instructions.InstructionType.Register => RegRegInstructionLookup(words, adrMode.Value, conditional),
			Instructions.InstructionType.Immediate => RegImmInstructionLookup(words, adrMode.Value, conditional),
			Instructions.InstructionType.Jump => JumpInstructionLookup(words, adrMode.Value, conditional),
			_ => throw new ArgumentException($"Line {string.Join(" ", words)} does not parse as an instruction.")
		};
	}

    private static Instructions.RegRegInstruction RegRegInstructionLookup(
		string[] words, Instructions.AddressingMode adrMode, Instructions.Condition conditional)
	{
		string mnemonic = words[0].ToUpper();
		Instructions.Opcode opcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Register)].Item1;
		CPU.ALU.ALUOpcode aluOpcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Register)].Item2;

		return mnemonic switch
		{
			"MOV" or "ADD" or "SUB" or "AND" or "OR" or "LDR" or "STR"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)words[1],
					(Instructions.AddressableRegisterCode)words[2],
					aluOpcode,
					adrMode
				),
			
			"PUSH" or "CALLR"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					DEFAULT_UNUSED_REGISTER,
					(Instructions.AddressableRegisterCode)words[1],
					aluOpcode,
					adrMode
				),

			"POP"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)words[1],
					DEFAULT_UNUSED_REGISTER,
					aluOpcode,
					adrMode
				),
			
			"TST" or "CLC" or "CLZ" or "CLOF" or "CLNG" or "CLA"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					DEFAULT_UNUSED_REGISTER,
					DEFAULT_UNUSED_REGISTER,
					aluOpcode,
					adrMode
				),

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
	public static readonly Instructions.AddressableRegisterCode DEFAULT_UNUSED_REGISTER = (Instructions.AddressableRegisterCode)0x0u;

    private static Instructions.RegImmInstruction RegImmInstructionLookup(
        string[] words, Instructions.AddressingMode adrMode, Instructions.Condition conditional)
    {
        string mnemonic = words[0].ToUpper();
		Instructions.Opcode opcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Immediate)].Item1;
		CPU.ALU.ALUOpcode aluOpcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Immediate)].Item2;

        return mnemonic switch
		{
			"MOV" or "ADD" or "SUB" or "AND" or "OR" or "LD" or "ST"
				=> new Instructions.RegImmInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)words[1],
					aluOpcode,
					adrMode,
					ConvertStringToUInt(words[2])),
			
			// For LOAD and STORE instructions:
			// Multiple ALU opcodes required during execution; Therefore, none are set here.

			"INC" or "DEC"
				=> new Instructions.RegImmInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)words[1],
					aluOpcode,
					adrMode,
					IMMEDIATE_DEFAULT_VALUE),

			_ => throw new ArgumentException($"Lookup for instruction mnemonic {mnemonic} failed. No associated I-Type instruction.")
		};
    }

    private static Instructions.JumpInstruction JumpInstructionLookup(
        string[] words, Instructions.AddressingMode adrMode, Instructions.Condition conditional)
    {
        string mnemonic = words[0].ToUpper();
		Instructions.Opcode opcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Jump)].Item1;
		CPU.ALU.ALUOpcode aluOpcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Jump)].Item2;

        return mnemonic switch
		{
			"NOP" => new Instructions.JumpInstruction(
					opcode,
					conditional,
					adrMode,
					IMMEDIATE_DEFAULT_VALUE),

			"JMP" or "B" or "CALL" or "INT"
				=> new Instructions.JumpInstruction(
					opcode,
					conditional,
					adrMode,
					ConvertStringToUInt(words[1])),
			
			"RET" or "IRET"
				=> new Instructions.JumpInstruction(
					opcode,
					conditional,
					adrMode,
					IMMEDIATE_DEFAULT_VALUE
				),

			_ => throw new ArgumentException($"Lookup for instruction mnemonic {mnemonic} failed. No associated J-Type instruction.")
		};
    }
}