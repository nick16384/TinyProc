using static TinyProc.Processor.Instructions;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;
using System.Data;
using static TinyProc.Assembling.Sections.DataSection;

namespace TinyProc.Assembling.Sections;

public readonly struct TextSection : IAssemblySection
{
    private static readonly Token TOKEN_BRACKET_OPEN = new(TokenType.BRACKET_OPEN, "(");
    private static readonly Token TOKEN_BRACKET_CLOSE = new(TokenType.BRACKET_CLOSE, ")");
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
        {
            entryPoint = header.EntryPoint; // Absolute value already determined
            Logging.LogDebug($"Entry point (direct): {entryPoint:x8}");
        }
        else
        {
            Logging.LogDebug($"Entry point seems to reference a label, resolving later.");
        }
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
            if (labelAddressMap.TryGetValue(header.EntryPoint, out entryPoint))
                Logging.LogDebug($"Entry point (from label): {entryPoint:x8}");
            else
                throw new Exception($"Cannot infer entry point {header.EntryPoint.B}");
        currentAddress = 0;

        // Parse each line / statement separately
        for (int i = 0; i < assemblyStatements.Count; i++)
        {
            Statement statement = assemblyStatements[i];
            Logging.NewlineDebug();
            Logging.LogDebug($"Parsing line: {statement}");
            // Check for blatantly invalid instruction syntax:
            if (statement.STLength < 1 || statement.Tokens[0].Type != TokenType.MNEMONIC)
                throw new Exception($"Instruction has zero length (?) or invalid mnemonic: {statement}");

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
                // Skip brackets, square brackets, mnemonics, etc.
                // Only literal words are relevant here for potential replacement
                if (token.Type != TokenType.LITERAL_WORD)
                    continue;
                    
                if (dataSection.ImmediateSequences.Any(seq => seq.Alias == token.Value) && labelAddressMap.ContainsKey(token.Value))
                    // If both of the "dictionaries" contain the same word, the reference is ambiguous, since the source is indeterminable.
                    throw new Exception($"Address label / .data section reference is ambiguous for \"{token.Value}\".");
                
                if (labelAddressMap.TryGetValue(token.Value, out uint labelAddress))
                {
                    // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                    uint labelRelAddress = labelAddress - currentAddress - 2;
                    Logging.LogDebug($".text replace label \"{token.Value}\" --> {labelRelAddress:x8}h / {labelRelAddress}");
                    token.Type = TokenType.NUMERIC_VALUE;
                    token.Value = labelRelAddress.ToString();
                    adrMode = AddressingMode.PCRelative;
                }

                // Although the data section loads at a predefined address, we'll use relative addressing
                // for future proofing, since relative addresses are shorter and could therefore be encoded shorter.
                if (dataSection.ImmediateSequences.Any(seq => seq.Alias == token.Value))
                {
                    ImmediateSequence immediateSequence = dataSection.ImmediateSequences.First(seq => seq.Alias == token.Value);
                    token.Type = TokenType.NUMERIC_VALUE;
                    adrMode = AddressingMode.PCRelative;
                    
                    // If the immediate sequence is a constant or a single value, do a replacement with the instruction operand
                    if (immediateSequence.Data.Length == 1)
                    {
                        Logging.LogDebug($".text replace single word / constant (R) \"{token.Value}\" --> {immediateSequence.Data[0]:x8}h / {immediateSequence.Data[0]}");
                        token.Value = immediateSequence.Data[0].ToString();
                    }
                    // If the immediate sequence is a multi-word, replace the operand with a relative address
                    else if (immediateSequence.Data.Length > 1)
                    {
                        uint relAddr = immediateSequence.Offset!.Value - currentAddress - dataSection.Size - 1;
                        Logging.LogDebug($".text replace multi-word (R) \"{token.Value}\" --> {relAddr:x8}h / {relAddr}");
                        token.Value = relAddr.ToString();
                    }
                    // This should not be possible
                    else
                        throw new Exception("Immediate sequence has length of zero. This is an internal error.");
                }

                // At this point, the token type should be a numeric value, so if no replacement was found
                // this indicates the word doesn't map to anything useful for further assembling.
                if (token.Type == TokenType.LITERAL_WORD)
                    throw new Exception($"Unresolved reference {token.Value} in {statement}");
            }

            // Step 2: Evaluate and substitute constant value expressions (e.g. "mov gp1, (pointer + 2)")
            List<Statement> constantExpressions = GetEnclosedStatements(TOKEN_BRACKET_OPEN, TOKEN_BRACKET_CLOSE, statement);
            foreach (Statement expression in constantExpressions)
            {
                // Evaluate constant expression
                uint expressionValue = Convert.ToUInt32(new DataTable().Compute(expression.ToString(), null));
                // TODO: This could be made more clean?
                int expressionTokenIdx = Array.IndexOf(statement.Tokens, expression.Tokens[0]);
                List<Token> statementTokensNew = [.. statement.Tokens];
                statementTokensNew.RemoveAll(token => expression.Tokens.Contains(token));
                statementTokensNew.Insert(expressionTokenIdx, new Token(TokenType.NUMERIC_VALUE, expressionValue.ToString()));
                statement = new Statement([.. statementTokensNew]);
            }

            Logging.LogDebug($"[{currentAddress:x8}] \"{assemblyStatements[i]}\" -> \"{statement}\"");

            // Parse assembly line as instruction object
            IInstruction instruction = InstructionLookup.ParseAsInstruction(statement, adrMode);

            // Note: Since the introduction of relative / absolute jumps, it is no longer necessary
            // to adjust any instructions pointing to memory, since they're either absolute in memory
            // or relative to PC-2.
            
            instructions.Add(instruction);
            currentAddress += 2;
        }

        Logging.NewlineDebug();
        Logging.LogDebug($".text section successfully parsed into a total of {instructions.Count * 2} word(s).");
        return new TextSection(instructions, labelAddressMap);
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
        if (header.STLength < 2 || header.Tokens[0].Type != TokenType.DIRECTIVE_SECTION || header.Tokens[^2].Type != TokenType.DIRECTIVE_SECTION_TEXT)
            throw new ArgumentException("Cannot parse assembly .text section: Incorrect header");

        if (header.STLength > 2)
        {
            // Header contains attributes
            // Remove first word "#SECTION" and last word ".text"
            header = new Statement(header.Tokens[1..^2]);
            // Split by brackets
            List<Statement> attributes = GetEnclosedStatements(TOKEN_BRACKET_OPEN, TOKEN_BRACKET_CLOSE, header);

            foreach (Statement attribute in attributes)
            {
                // Check if the attribute is even in the correct format
                if (attribute.STLength != 3 || attribute.Tokens[0].Type != TokenType.LITERAL_WORD || attribute.Tokens[1].Type != TokenType.SYMBOL_EQUALS)
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

    /// <summary>
    /// Takes in a statement and searches for inner statements enclosed between delimiter tokens.
    /// An illustrative example would be GetEnclosedStatements("(", ")", "Random text (with words) inside (brackets)")
    /// which would return "with words" and "brackets", thereby giving statements inside opening and closing bracket delimiters.
    /// </summary>
    /// <remarks>
    /// Note: Since the == operator for tokens is overridden to only compare the type and value, it is safe to
    /// assume the delimiter tokens are equal to any other delimiter token with the same type and value.
    /// </remarks>
    /// <param name="delimiterOpen">The token for a sub-statement opening</param>
    /// <param name="delimiterClose">The token for a sub-statement close</param>
    /// <param name="statement">The original statement / sequence of tokens</param>
    /// <returns>A list of sub-statements enclosed in the delimiters</returns>
    private static List<Statement> GetEnclosedStatements(Token delimiterOpen, Token delimiterClose, Statement statement)
    {
        List<Statement> enclosedStatements = [];
        bool isOpen = false;
        foreach (Token token in statement.Tokens)
        {
            Console.WriteLine(token.Value);
            if (token == delimiterOpen)
            {
                // Open in-bracket statement
                if (isOpen)
                    Logging.LogWarn($"Warning: Double open delimiter \"{delimiterOpen.Value}\" in enclosed statement.");
                isOpen = true;
                enclosedStatements.Add(new Statement([]));
                continue;
            }
            else if (token == delimiterClose)
            {
                // Close attribute with EOS
                isOpen = false;
                enclosedStatements[^1] = new Statement([.. enclosedStatements[^1].Tokens, Token.CreateEOS()]);
                continue;
            }
            if (isOpen)
                // Add attribute content
                enclosedStatements[^1] = new Statement([.. enclosedStatements[^1].Tokens, token]);
        }
        if (isOpen)
            throw new Exception($"Unclosed delimiter in enclosed statement: missing \"{delimiterClose.Value}\"");
        return enclosedStatements;
    }
}