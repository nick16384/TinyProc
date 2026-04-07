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
		{ ("CMP",   Instructions.InstructionType.Register),  (Instructions.Opcode.CMPR,  CPU.ALU.ALUOpcode.AB_SubtractionSigned) },
		{ ("CMP",   Instructions.InstructionType.Immediate), (Instructions.Opcode.CMP,   CPU.ALU.ALUOpcode.AB_SubtractionSigned) },

		{ ("LD",    Instructions.InstructionType.Immediate), (Instructions.Opcode.LD,    DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("LD",    Instructions.InstructionType.Register),  (Instructions.Opcode.LDR,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("ST",    Instructions.InstructionType.Immediate), (Instructions.Opcode.ST,    DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("ST",    Instructions.InstructionType.Register),  (Instructions.Opcode.STR,   DEFAULT_EMPTY_ALU_OPCODE) },

		{ ("PUSH",  Instructions.InstructionType.Register),  (Instructions.Opcode.PUSH,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("POP",   Instructions.InstructionType.Register),  (Instructions.Opcode.POP,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("JMP",   Instructions.InstructionType.Jump),      (Instructions.Opcode.JMP,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("JMP",   Instructions.InstructionType.Register),  (Instructions.Opcode.JMPR,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("B",     Instructions.InstructionType.Jump),      (Instructions.Opcode.JMP,   DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CALL",  Instructions.InstructionType.Jump),      (Instructions.Opcode.CALL,  DEFAULT_EMPTY_ALU_OPCODE) },
		{ ("CALL",  Instructions.InstructionType.Register),  (Instructions.Opcode.CALLR, DEFAULT_EMPTY_ALU_OPCODE) },
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

	internal static bool IsJumpInstruction(Statement instruction)
	{
		return
			instruction.Tokens[0].Value.StartsWith("JMP", StringComparison.OrdinalIgnoreCase) ||
			instruction.Tokens[0].Value.StartsWith("B", StringComparison.OrdinalIgnoreCase) ||
			instruction.Tokens[0].Value.StartsWith("CALL", StringComparison.OrdinalIgnoreCase);
	}
	internal static bool IsLoadStoreInstruction(Statement instruction)
	{
		return
			instruction.Tokens[0].Value.StartsWith("LD", StringComparison.OrdinalIgnoreCase) ||
			instruction.Tokens[0].Value.StartsWith("ST", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Checks if the nth operand of the assembly instruction is representing a number.
	/// </summary>
	/// <param name="instruction"></param>
	/// <param name="n"></param>
	/// <returns>Returns false if there are less operands than n, or the nth operand is not a number.
	/// Returns true otherwise.</returns>
	private static bool IsNthOperandNumber(Statement instruction, int n)
	{
		bool result = instruction.STLength - 1 >= n && TryConvertStringToUInt(instruction.Tokens[n].Value, out _);
		Logging.LogDebug($"Operand {n}: {instruction.Tokens[n].Value}");
		return result;
	}

	internal static Instructions.IInstruction ParseAsInstruction(Statement instruction, Instructions.AddressingMode? adrMode)
	{
		// Set default addressing mode, even if the instruction doesn't need it
		adrMode ??= Instructions.AddressingMode.Absolute;
		
		string mnemonic = instruction.Tokens[0].Value.ToUpper();

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
				instruction.Tokens[0].Value = mnemonic;
			}
			catch (KeyNotFoundException) { Logging.LogDebug($"Mnemonic {mnemonic} has no condition code."); }
		}
		else
			Logging.LogDebug($"Mnemonic {mnemonic} has no condition code.");

		// Determine instruction type (R/I/J)
		Instructions.InstructionType type;
		bool isFirstOperandNumber = IsNthOperandNumber(instruction, 1);
		bool isSecondOperandNumber = IsNthOperandNumber(instruction, 2);

		if (mnemonic == "TST") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLC") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLZ") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLOF") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLNG") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CLA") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "MOV" && isSecondOperandNumber) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "MOV" && !isSecondOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "ADD" && isSecondOperandNumber) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "ADD" && !isSecondOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "SUB" && isSecondOperandNumber) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "SUB" && !isSecondOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "INC") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "DEC") { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "AND" && isSecondOperandNumber) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "AND" && !isSecondOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "OR" && isSecondOperandNumber) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "CMP" && !isSecondOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CMP" && isSecondOperandNumber) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "OR" && !isSecondOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "LD" && isSecondOperandNumber) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "LD" && !isSecondOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "ST" && isSecondOperandNumber) { type = Instructions.InstructionType.Immediate; }
		else if (mnemonic == "ST" && !isSecondOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "PUSH") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "POP") { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "NOP") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "B") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "JMP" && isFirstOperandNumber) { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "JMP" && !isFirstOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "CALL" && isFirstOperandNumber) { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "CALL" && !isFirstOperandNumber) { type = Instructions.InstructionType.Register; }
		else if (mnemonic == "RET") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "INT") { type = Instructions.InstructionType.Jump; }
		else if (mnemonic == "IRET") { type = Instructions.InstructionType.Jump; }
		else { throw new ArgumentException("Cannot determine instruction type."); }

		Logging.LogDebug($"Type: {type}");
		return type switch
		{
			Instructions.InstructionType.Register => RegRegInstructionLookup(instruction, adrMode.Value, conditional),
			Instructions.InstructionType.Immediate => RegImmInstructionLookup(instruction, adrMode.Value, conditional),
			Instructions.InstructionType.Jump => JumpInstructionLookup(instruction, adrMode.Value, conditional),
			_ => throw new ArgumentException($"Line {instruction} does not parse as an instruction.")
		};
	}

    private static Instructions.RegRegInstruction RegRegInstructionLookup(
		Statement instruction, Instructions.AddressingMode adrMode, Instructions.Condition conditional)
	{
		string mnemonic = instruction.Tokens[0].Value.ToUpper();
		Instructions.Opcode opcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Register)].Item1;
		CPU.ALU.ALUOpcode aluOpcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Register)].Item2;

		return mnemonic switch
		{
			"MOV" or "ADD" or "SUB" or "AND" or "OR" or "LD" or "ST" or "CMP"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)instruction.Tokens[1].Value,
					(Instructions.AddressableRegisterCode)instruction.Tokens[2].Value,
					aluOpcode,
					adrMode
				),
			
			"PUSH" or "CALL" or "JMP"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					DEFAULT_UNUSED_REGISTER,
					(Instructions.AddressableRegisterCode)instruction.Tokens[1].Value,
					aluOpcode,
					adrMode
				),

			"POP"
				=> new Instructions.RegRegInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)instruction.Tokens[1].Value,
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
        Statement instruction, Instructions.AddressingMode adrMode, Instructions.Condition conditional)
    {
        string mnemonic = instruction.Tokens[0].Value.ToUpper();
		Instructions.Opcode opcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Immediate)].Item1;
		CPU.ALU.ALUOpcode aluOpcode = MnemonicOpcodeMap[(mnemonic, Instructions.InstructionType.Immediate)].Item2;

        return mnemonic switch
		{
			"MOV" or "ADD" or "SUB" or "AND" or "OR" or "LD" or "ST" or "CMP"
				=> new Instructions.RegImmInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)instruction.Tokens[1].Value,
					aluOpcode,
					adrMode,
					ConvertStringToUInt(instruction.Tokens[2].Value)),
			
			// For LOAD and STORE instructions:
			// Multiple ALU opcodes required during execution; Therefore, none are set here.

			"INC" or "DEC"
				=> new Instructions.RegImmInstruction(
					opcode,
					conditional,
					(Instructions.AddressableRegisterCode)instruction.Tokens[1].Value,
					aluOpcode,
					adrMode,
					IMMEDIATE_DEFAULT_VALUE),

			_ => throw new ArgumentException($"Lookup for instruction mnemonic {mnemonic} failed. No associated I-Type instruction.")
		};
    }

    private static Instructions.JumpInstruction JumpInstructionLookup(
        Statement instruction, Instructions.AddressingMode adrMode, Instructions.Condition conditional)
    {
        string mnemonic = instruction.Tokens[0].Value.ToUpper();
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
					ConvertStringToUInt(instruction.Tokens[1].Value)),
			
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