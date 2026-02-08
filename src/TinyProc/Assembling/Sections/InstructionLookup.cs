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

		{ ("ALD",   Instructions.InstructionType.Immediate), (Instructions.Opcode.ALD,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("LD",    Instructions.InstructionType.Immediate), (Instructions.Opcode.LD,    DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("ALDR",  Instructions.InstructionType.Register),  (Instructions.Opcode.ALDR,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("LDR",   Instructions.InstructionType.Register),  (Instructions.Opcode.LDR,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("ASTR",  Instructions.InstructionType.Immediate), (Instructions.Opcode.ASTR,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("STR",   Instructions.InstructionType.Immediate), (Instructions.Opcode.STR,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("ASTRR", Instructions.InstructionType.Register),  (Instructions.Opcode.ASTRR, DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("STRR",  Instructions.InstructionType.Register),  (Instructions.Opcode.STRR,  DEFAULT_EMPTY_ALU_OPCODE) },

		{ ("PUSH",  Instructions.InstructionType.Register),  (Instructions.Opcode.PUSH,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("POP",   Instructions.InstructionType.Register),  (Instructions.Opcode.POP,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("AJMP",  Instructions.InstructionType.Jump),      (Instructions.Opcode.AJMP,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("JMP",   Instructions.InstructionType.Jump),      (Instructions.Opcode.JMP,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("AB",    Instructions.InstructionType.Jump),      (Instructions.Opcode.AB,    DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("B",     Instructions.InstructionType.Jump),      (Instructions.Opcode.B,     DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("ACALL", Instructions.InstructionType.Jump),      (Instructions.Opcode.ACALL, DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CALL",  Instructions.InstructionType.Jump),      (Instructions.Opcode.CALL,  DEFAULT_EMPTY_ALU_OPCODE) },
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
		else if (mnemonic == "ALD") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "LD") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "ALDR") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "LDR") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "ASTR") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "STR") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "ASTRR") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "STRR") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "PUSH") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "POP") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "NOP") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "AJMP") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "JMP") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "AB") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "B") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "ACALL") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "CALL") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "RET") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "INT") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "IRET") { type = Instructions.InstructionType.Jump; }
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
			"MOV" or "ADD" or "SUB" or "AND" or "OR" or "ALD" or "LD" or "ALDR" or "LDR" or "ASTR" or "STR" or "ASTRR" or "STRR"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)words[1],
					(Instructions.AddressableRegisterCode)words[2],
					aluOpcode
				),
			
			"PUSH"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)words[1],
					DEFAULT_UNUSED_REGISTER,
					aluOpcode
				),

			"POP"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					DEFAULT_UNUSED_REGISTER,
					(Instructions.AddressableRegisterCode)words[1],
					aluOpcode
				),
			
			"TST" or "CLC" or "CLZ" or "CLOF" or "CLNG" or "CLA"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					DEFAULT_UNUSED_REGISTER,
					DEFAULT_UNUSED_REGISTER,
					aluOpcode
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

			"AJMP" or "JMP" or "AB" or "B" or "ACALL" or "CALL" or "INT"
				=> new Instructions.JumpInstruction(
					opcode,
					conditional,
					ConvertStringToUInt(words[1])),
			
			"RET" or "IRET"
				=> new Instructions.JumpInstruction(
					opcode,
					conditional,
					IMMEDIATE_DEFAULT_VALUE
				),

			_ => throw new ArgumentException($"Lookup for instruction mnemonic {mnemonic} failed. No associated J-Type instruction.")
		};
    }
}