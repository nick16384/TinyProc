using static TinyProc.Processor.Instructions;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;
using System.Data;
using static TinyProc.Assembling.Sections.DataSection;

namespace TinyProc.Assembling.Sections;

public readonly struct TextSection : IAssemblySection
{
    public uint Size { get; }
    public uint EntryPoint { get; }

    public List<IInstruction> Instructions { get; }
    public Dictionary<string, uint> LabelAddressMap { get; }
    public List<uint> BinaryRepresentation { get; }

    private readonly struct TextSectionHeader(Either<uint, string> entryPoint)
    {
        public readonly Either<uint, string> EntryPoint = entryPoint;
    }

    public TextSection(List<IInstruction> instructions, Dictionary<string, uint> labelAddressMap)
    {
        Size = (uint)instructions.Count * 2;
        Instructions = instructions;
        LabelAddressMap = labelAddressMap;

        BinaryRepresentation = [];
        foreach (IInstruction instruction in Instructions)
            BinaryRepresentation.AddRange(instruction.BinaryRepresentation.Item1, instruction.BinaryRepresentation.Item2);
    }

    /// <summary>
    /// Parses the text section of assembly code into a TextSection object.
    /// </summary>
    /// <param name="assemblyCodeTextSection"></param>
    /// <returns></returns>
    internal static TextSection CreateFromAssemblyCode(List<Statement> assemblyStatements, DataSection dataSection)
    {
        uint entryPoint;
        List<IInstruction> instructions = [];
        Dictionary<string, uint> labelAddressMap = [];

        // Parse section header
        TextSectionHeader header = ParseHeader(assemblyStatements[0]);
        if (header.EntryPoint.Is<uint>())
            entryPoint = header.EntryPoint; // Absolute value already determined
        else
            Logging.LogDebug($"Entry point seems to reference a label, resolving later.");
        assemblyStatements.RemoveAt(0);

        // .text section pre-parser:
        // Replace label encounters with their corresponding address.
        uint currentAddress = 0;
        for (int i = 0; i < assemblyStatements.Count; i++)
        {
            Statement statement = assemblyStatements[i];
            if (statement.Tokens[0].Type == TokenType.LABEL)
            {
                labelAddressMap.Add(statement.Tokens[0].Value, currentAddress);
                Logging.LogDebug($"Found label declaration \"{statement.Tokens[0].Value}\" at address {currentAddress:x8}");
                assemblyStatements.RemoveAt(i--);
                continue; // Prevent address increment
            }
            currentAddress += 2;
        }
        // Try to find entry point label again
        if (header.EntryPoint.Is<string>())
            if (!labelAddressMap.TryGetValue(header.EntryPoint, out entryPoint))
                throw new Exception($"Cannot infer entry point {header.EntryPoint.B}");
        currentAddress = 0;

        // Parse each line / statement separately
        foreach (Statement statement in assemblyStatements)
        {
            Logging.NewlineDebug();
            Logging.LogDebug($"Parsing line: {statement}");
            // Check for blatantly invalid instruction syntax:
            if (statement.Length < 1 || statement.Tokens[0].Type != TokenType.MNEMONIC)
                throw new Exception($"Instruction has zero length (?) or invalid mnemonic: {statement}");

            // Replace occurrences of immediate value / pointer identifiers of the .data section
            // with their corresponding numeric value.

            // Step 1: Replace occurrences of labels / .data section references with their corresponding addresses / values
            // The addressing mode specifies how an address is interpreted by the CPU.
            // There are (currently) two addressing modes:
            // 1. A (Absolute): An absolute address in the entire memory space
            // 2. R (PC-relative): The address is treated as an offset relative to the address of the next instruction (PC)
            // The instruction is relative, if if it references a label or pointer.
            // Otherwise, it is implicitly absolute.
            AddressingMode? adrMode = null;
            if (InstructionLookup.IsJumpInstruction(statement) || InstructionLookup.IsLoadStoreInstruction(statement))
                adrMode = AddressingMode.Absolute;

            foreach (Token token in statement.Tokens)
            {
                // Skip bracket and square bracket tokens (irrelevant here)
                if (token.Type == TokenType.BRACKET_OPEN ||
                    token.Type == TokenType.BRACKET_CLOSE ||
                    token.Type == TokenType.SQUARE_BRACKET_OPEN ||
                    token.Type == TokenType.SQUARE_BRACKET_CLOSE)
                    continue;
                    
                if (dataSection.ImmediateSequences.Any(seq => seq.Alias == token.Value) && labelAddressMap.ContainsKey(token.Value))
                    // If both of the "dictionaries" contain the same word, the reference is ambiguous, since the source is indeterminable.
                    throw new Exception($"Address label / .data section reference is ambiguous for \"{token.Value}\".");
                













                
                // FIXME: Fix all da red stuff















                if (labelAddressMap.TryGetValue(token.Value, out uint labelAddress))
                {
                    // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                    uint labelRelAddress = labelAddress - currentAddress - 2;
                    line = line.Replace(word, labelRelAddress.ToString());
                    adrMode = AddressingMode.PCRelative;
                    Logging.LogDebug($".text replace label \"{word}\" --> {labelRelAddress:x8}h / {labelRelAddress}");
                }

                // The instruction is a load / store instruction possibly using relative addressing modes
                if (dataSection.FixedLoadAddress.HasValue)
                {
                    // Location in memory is static for both .data and .text section
                    // (since .text is loaded immediately after .data, only check for .data is necessary)
                    // ==> Use absolute addressing
                    ImmediateSequence seq = dataSection.ImmediateSequences.First(seq => seq.Alias == word);

                    if (isImmediate)
                    {
                        uint immediateValue = immediate.Value;
                        line = line.Replace(word, immediateValue.ToString());
                        Logging.LogDebug($".text replace immediate \"{word}\" --> {immediateValue:x8}h / {immediateValue}");
                    }
                    else if (isPointer)
                    {
                        // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                        uint pointerAbsAddress = dataSection.FixedLoadAddress.Value + pointer.Offset;
                        line = line.Replace(word, pointerAbsAddress.ToString());
                        adrMode = AddressingMode.Absolute;
                        Logging.LogDebug($".text replace pointer (A) \"{word}\" --> {pointerAbsAddress:x8}h / {pointerAbsAddress}");
                    }
                    else if (isBlockPointer)
                    {
                        // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                        uint blockPointerRelAddress = dataSection.FixedLoadAddress.Value + blockPointer.Offset;
                        line = line.Replace(word, blockPointerRelAddress.ToString());
                        adrMode = AddressingMode.Absolute;
                        Logging.LogDebug($".text replace block pointer (A) \"{word}\" --> {blockPointerRelAddress:x8}h / {blockPointerRelAddress}");
                    }
                }
                else
                {
                    // Location in memory is dynamic for either the .data or the .text section.
                    // This means that absolute load / stores won't work.
                    // ==> Use relative addressing
                    bool isImmediate = dataSection.ImmediateValues.TryGetValue(word, out DataSection.ImmediateValue immediate);
                    bool isPointer = dataSection.Pointers.TryGetValue(word, out DataSection.ImmediateSequence pointer);
                    bool isBlockPointer = dataSection.BlockPointers.TryGetValue(word, out DataSection.ContinuousBlock blockPointer);
                    if (isImmediate)
                    {
                        uint immediateValue = immediate.Value;
                        line = line.Replace(word, immediateValue.ToString());
                        Logging.LogDebug($".text replace immediate \"{word}\" --> {immediateValue:x8}h / {immediateValue}");
                    }
                    else if (isPointer)
                    {
                        // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                        uint pointerRelAddress = pointer.Offset - currentAddress - dataSection.Size - 1;
                        line = line.Replace(word, pointerRelAddress.ToString());
                        adrMode = AddressingMode.PCRelative;
                        Logging.LogDebug($".text replace pointer (R) \"{word}\" --> {pointerRelAddress:x8}h / {pointerRelAddress}");
                    }
                    else if (isBlockPointer)
                    {
                        // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                        uint blockPointerRelAddress = blockPointer.Offset - currentAddress - dataSection.Size - 1;
                        line = line.Replace(word, blockPointerRelAddress.ToString());
                        adrMode = AddressingMode.PCRelative;
                        Logging.LogDebug($".text replace block pointer (R) \"{word}\" --> {blockPointerRelAddress:x8}h / {blockPointerRelAddress}");
                    }
                }
            }

            // Step 2: Evaluate and substitute constant value expressions (e.g. "mov gp1, (pointer + 2)")
            List<string> constantExpressions = [.. line.Split(["(", ")"], StringSplitOptions.None)];
            // Remove parts not lying between opening and closing brackets (in that order)
            constantExpressions = [.. constantExpressions.Where((str, index) => index % 2 != 0)];
            foreach (string expression in constantExpressions)
            {
                string expressionPreParsed = expression;
                List<string> expressionWords = [.. SplitLineIntoWords(expression)];

                // Evaluate constant expression
                uint expressionValue = Convert.ToUInt32(new DataTable().Compute(expressionPreParsed, null));
                line = line.Replace("(" + expression + ")", expressionValue.ToString());
            }

            Logging.LogDebug($"[{currentAddress:x8}] \"{lineUnparsed}\" -> \"{line}\"");
            words = SplitLineIntoWords(line);

            // Parse assembly line as instruction object
            IInstruction instruction = InstructionLookup.ParseAsInstruction(words, adrMode);

            // Note: Since the introduction of relative / absolute jumps, it is no longer necessary
            // to adjust any instructions pointing to memory, since they're either absolute in memory
            // or relative to PC-2.
            
            instructions.Add(instruction);
            currentAddress += 2;
        }

        Logging.NewlineDebug();
        Logging.LogDebug($".text section successfully parsed into a total of {instructions.Count * 2} word(s).");
        return new TextSection(fixedLoadAddress, instructions, labelAddressMap);
    }

    /// <summary>
    /// Processes the header of the .text section, additionally parsing attributes.
    /// </summary>
    /// <param name="lines"></param>
    /// <returns>The entry point as uint or a label string if the entry point is a label</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static TextSectionHeader ParseHeader(Statement header)
    {
        // By default, the entry point (offset into .text section) is zero
        Either<uint, string> entryPoint = 0x00000000;
        // Parse section header
        if (header.Length < 2 || header.Tokens[0].Type != TokenType.DIRECTIVE_SECTION || header.Tokens[^1].Type != TokenType.DIRECTIVE_SECTION_TEXT)
            throw new ArgumentException("Cannot parse assembly .text section: Incorrect header");

        if (header.Length > 2)
        {
            // Header contains attributes
            // Remove first word "#SECTION" and last word ".text"
            header = new Statement(header.Tokens[1..^1]);
            // Split by brackets
            List<Statement> attributes = [];
            bool isBracketOpen = false;
            foreach (Token token in header.Tokens)
            {
                if (token.Type == TokenType.BRACKET_OPEN)
                {
                    // Open new attribute
                    isBracketOpen = true;
                    attributes.Add(new Statement([]));
                }
                else if (token.Type == TokenType.BRACKET_CLOSE)
                {
                    // Close attribute with EOS
                    isBracketOpen = false;
                    attributes[^1] = new Statement([.. attributes[^1].Tokens, Token.CreateEOS()]);
                }
                if (isBracketOpen)
                    // Add attribute content
                    attributes[^1] = new Statement([.. attributes[^1].Tokens, token]);
            }

            foreach (Statement attribute in attributes)
            {
                // Check if the attribute is even in the correct format
                if (attribute.Length != 3 || attribute.Tokens[0].Type != TokenType.LITERAL_WORD || attribute.Tokens[1].Type != TokenType.SYMBOL_EQUALS)
                    throw new Exception($"Unable to parse attribute \"{attribute}\". Wrong syntax");
                string identifier = attribute.Tokens[0].Value;
                Token valueToken = attribute.Tokens[2];

                // Extract attribute values
                // TODO: Throw exception when encountering same attribute multiple times
                switch (identifier)
                {
                    case ASM_ATTRIBUTE_TEXT_ENTRYPOINT:
                        if (valueToken.Type == TokenType.NUMERIC_VALUE)
                            entryPoint = ConvertStringToUInt(valueToken.Value);
                        else if (valueToken.Type == TokenType.LITERAL_WORD)
                            entryPoint = valueToken.Value;
                        else
                            throw new Exception($"Invalid entry point {valueToken.Value}: Unrecognized type");
                        break;
                    default:
                        throw new ArgumentException($"Invalid attribute identifier \"{identifier}\"");
                }

                // Identify possibly illegal attribute combinations
                // Currently none
            }
        }
        Logging.LogDebug("Successfully verified and parsed .text section header.");

        return new TextSectionHeader(entryPoint);
    }
}