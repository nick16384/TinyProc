using static TinyProc.Processor.Instructions;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;
using System.Data;
using static TinyProc.Assembling.Sections.DataSection;

namespace TinyProc.Assembling.Sections;

public readonly struct TextSection : IAssemblySection
{
    private static readonly Token TOKEN_BRACKET_OPEN = new(TokenType.BRACKET, "(");
    private static readonly Token TOKEN_BRACKET_CLOSE = new(TokenType.BRACKET, ")");
    public uint Size { get; }
    public uint EntryPoint { get; }

    public List<IInstruction> Instructions { get; }
    public Dictionary<string, uint> LabelAddressMap { get; }
    public List<uint> BinaryRepresentation { get; }

    private readonly struct TextSectionHeader(Either<uint, string> entryPoint, AddressingMode? implicitAddressingMode)
    {
        public readonly Either<uint, string> EntryPoint = entryPoint;
        public readonly AddressingMode ImplicitAddressingMode = implicitAddressingMode ?? AddressingMode.PCRelative;
    }

    public TextSection(List<IInstruction> instructions, Dictionary<string, uint> labelAddressMap, uint entryPoint)
    {
        Size = (uint)instructions.Count * 2;
        EntryPoint = entryPoint;
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
    internal static TextSection CreateFromAssemblyCode(List<Statement> assemblyStatements, uint globalLoadAddress, DataSection dataSection)
    {
        uint entryPoint = default; // Will be overridden anyways, compiler just doesn't see that
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
            Logging.LogDebug($"Entry point {header.EntryPoint} seems to reference a label, resolving later.");
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
                // TODO: Relies on the fact that a label is the only token in a statement, which may be wrong. Revise!
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

        // .text section main parser:
        // Parse each line / statement separately
        for (int statementIdx = 0; statementIdx < assemblyStatements.Count; statementIdx++)
        {
            Statement instructionStatement = assemblyStatements[statementIdx];
            Logging.NewlineDebug();
            Logging.LogDebug($"Parsing line: {instructionStatement}");
            // Check for blatantly invalid instruction syntax:
            if (instructionStatement.STLength < 1)
                throw new Exception($"Instruction has zero length: {instructionStatement}");
            if (instructionStatement.Tokens[0].Type != TokenType.MNEMONIC)
                throw new Exception($"Instruction has invalid mnemonic: {instructionStatement}");

            AddressingMode adrMode = DetermineAddressingMode(instructionStatement, header.ImplicitAddressingMode);

            instructionStatement = ResolveReferences(
                instructionStatement,
                globalLoadAddress,
                currentAddress,
                adrMode,
                labelAddressMap,
                dataSection
            );
            instructionStatement = EvaluateConstantExpressions(instructionStatement);

            Logging.LogDebug($"[{currentAddress:x8}] \"{assemblyStatements[statementIdx]}\" -> \"{instructionStatement}\"");

            // Parse assembly line as instruction object
            IInstruction instruction = InstructionLookup.ParseAsInstruction(instructionStatement, adrMode);

            // Note: Since the introduction of relative / absolute jumps, it is no longer necessary
            // to adjust any instructions pointing to memory, since they're either absolute in memory
            // or relative to PC-2.
            
            instructions.Add(instruction);
            currentAddress += 2;
        }

        Logging.NewlineDebug();
        Logging.LogDebug($".text section successfully parsed into a total of {instructions.Count * 2} word(s).");
        return new TextSection(instructions, labelAddressMap, entryPoint);
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
        AddressingMode? implicitAddressingMode = null;
        // By default, the entry point (offset into .text section) is zero
        Either<uint, string> entryPoint = 0x00000000;
        // Parse section header
        if (header.STLength < 2 || header.Tokens[0].Type != TokenType.DIRECTIVE_SECTION || header.Tokens[^2].Type != TokenType.DIRECTIVE_SECTION_TEXT)
            throw new ArgumentException("Cannot parse assembly .text section: Incorrect header");

        if (header.STLength > 2)
        {
            // Header contains attributes
            // Remove first word "#SECTION" and last word ".text"
            header = new Statement([.. header.Tokens[1..^2], Token.CreateEOS()]);
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
                    case ASM_ATTRIBUTE_ADRMODE_IMPLICIT:
                        if (valueToken.Type != TokenType.LITERAL_WORD)
                            throw new Exception($"Invalid implicit addressing mode token (type) {valueToken.Value}");
                        if (valueToken.Value == ASM_ATTRIBUTE_ADRMODE_IMPLICIT_ABSOLUTE)
                            implicitAddressingMode = AddressingMode.Absolute;
                        else if (valueToken.Value == ASM_ATTRIBUTE_ADRMODE_IMPLICIT_RELATIVE)
                            implicitAddressingMode = AddressingMode.PCRelative;
                        else
                            throw new Exception($"Invalid implicit addressing mode {valueToken.Value}");
                        Logging.LogDebug($"Using implicit addressing mode {implicitAddressingMode}");
                        break;
                    default:
                        throw new ArgumentException($"Invalid attribute identifier \"{identifier}\"");
                }

                // Identify possibly illegal attribute combinations
                // Currently none
            }
        }
        Logging.LogDebug("Successfully verified and parsed .text section header.");

        return new TextSectionHeader(entryPoint, implicitAddressingMode);
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
        List<List<Token>> enclosedStatementTokens = [];
        bool isOpen = false;
        foreach (Token token in statement.Tokens)
        {
            if (token == delimiterOpen)
            {
                // Open in-bracket statement
                if (isOpen)
                    Logging.LogWarn($"Warning: Double open delimiter \"{delimiterOpen.Value}\" in enclosed statement.");
                isOpen = true;
                enclosedStatementTokens.Add([]);
                continue;
            }
            else if (token == delimiterClose)
            {
                // Close attribute with EOS
                isOpen = false;
                enclosedStatementTokens[^1].Add(Token.CreateEOS());
                continue;
            }
            if (isOpen)
                // Add attribute content
                enclosedStatementTokens[^1].Add(token);
        }
        if (isOpen)
            throw new Exception($"Unclosed delimiter in enclosed statement: missing \"{delimiterClose.Value}\"");
        return [.. enclosedStatementTokens.Select(tokens => new Statement([.. tokens]))];
    }

    /// <summary>
    /// Determines the addressing mode of this instruction.<br></br>
    /// The returned value will be AddressingMode.Absolute for non-memory instructions.<br></br>
    /// The implicit addressing mode will be used when the instruction does a memory operation, but no explicit mode is specified.
    /// </summary>
    /// <param name="instructionStatement"></param>
    /// <returns></returns>
    private static AddressingMode DetermineAddressingMode(Statement instructionStatement, AddressingMode implicitAddressingMode)
    {
        // The addressing mode specifies how an address is interpreted by the CPU.
        // There are (currently) two addressing modes:
        // 1. A (Absolute): An absolute address in the entire memory space
        // 2. R (PC-relative): The address is treated as an offset relative to the address of the next instruction (PC)
        // The instruction is relative, if if it references a label or pointer.
        // Otherwise, it is implicitly absolute.

        // If the instruction doesn't contain a memory reference, use the default addressing mode (Absolute).
        if (!InstructionLookup.IsJumpInstruction(instructionStatement) && !InstructionLookup.IsLoadStoreInstruction(instructionStatement))
            return AddressingMode.Absolute;
        
        AddressingMode adrMode = implicitAddressingMode;
        List<Token> tokens = [.. instructionStatement.Tokens];
        for (int tokenIdx = 0; tokenIdx < tokens.Count - 4; tokenIdx++)
        {
            Token token = tokens[tokenIdx];
            Token next = tokens[tokenIdx + 1];
            if (token.Value != "[")
                continue;
            
            if (next.Type == TokenType.KEYWORD_ADDRESSING_ABSOLUTE)
            {
                adrMode = AddressingMode.Absolute;
                Token address = tokens[tokenIdx + 2];
                tokens.RemoveAt(tokenIdx + 1); // Remove addressing mode token
                Logging.LogDebug($"Explicit addressing mode override: Absolute; Address: {address.Value}");
            }
            else if (next.Type == TokenType.KEYWORD_ADDRESSING_RELATIVE)
            {
                adrMode = AddressingMode.PCRelative;
                Token sign = tokens[tokenIdx + 2];
                Token offset = tokens[tokenIdx + 3];
                tokens.RemoveAt(tokenIdx + 3); // Remove offset token (replaced with unified token below)
                tokens.RemoveAt(tokenIdx + 2); // Remove sign token
                tokens.RemoveAt(tokenIdx + 1); // Remove addressing mode token
                // Merge sign and offset token (e.g. ["-", "400"] becomes ["-400"])
                if (sign.Value != "-" && sign.Value != "+")
                    throw new Exception("Explicit addressing mode override in relative mode expects sign (e.g. [rel +400] or [rel -400])");
                // "+" "offset" --> "offset"
                // "-" "offset" --> "0" "-" "offset"
                Token[] offsetTokensReformatted = [];
                if (sign.Value == "+")
                    offsetTokensReformatted = [new(TokenType.NUMERIC_VALUE, offset.Value)];
                else if (sign.Value == "-")
                    offsetTokensReformatted = [new(TokenType.NUMERIC_VALUE, "0"), new(TokenType.SYMBOL_ARITHMETIC_OP, "-"), offset];
                tokens.InsertRange(tokenIdx + 1, offsetTokensReformatted);
                Logging.LogDebug($"Explicit addressing mode override: Relative; Offset: {sign.Value}{offset.Value}");
            }
        }
        instructionStatement.Tokens = [.. tokens];

        return adrMode;
    }

    /// <summary>
    /// Parses an instruction statement, replacing references to data with the data itself.<br></br>
    /// This data includes address labels and immediate constants.
    /// </summary>
    /// <param name="instructionStatement"></param>
    /// <param name="globalLoadAddress"></param>
    /// <param name="instructionOffset">The offset in the .text section of this instruction</param>
    /// <param name="adrMode">The preferred addressing mode to use</param>
    /// <param name="textLabelAddressMap"></param>
    /// <param name="dataSection"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static Statement ResolveReferences(
        Statement instructionStatement,
        uint globalLoadAddress,
        uint instructionOffset,
        AddressingMode adrMode,
        Dictionary<string, uint> textLabelAddressMap,
        DataSection dataSection)
    {
        foreach (Token token in instructionStatement.Tokens)
        {
            // Skip brackets, square brackets, mnemonics, numeric values, etc.
            // Only literal words are relevant here for potential replacement
            if (token.Type != TokenType.LITERAL_WORD)
                continue;

            int tokenReferenceCount = 0;
            if (dataSection.ImmediateConstants.Any(constant => constant.Name == token.Value)) tokenReferenceCount++;
            if (dataSection.LabelAddressMap.ContainsKey(token.Value)) tokenReferenceCount++;
            if (textLabelAddressMap.ContainsKey(token.Value)) tokenReferenceCount++;
            // If this token refers to multiple destinations, the reference is ambiguous, since the source is indeterminable.
            if (tokenReferenceCount >= 2)
                throw new Exception($"Address label / .data section reference is ambiguous for \"{token.Value}\".");
            
            // .text label reference
            if (textLabelAddressMap.TryGetValue(token.Value, out uint textLabelOffset))
            {
                if (adrMode == AddressingMode.PCRelative)
                {
                    // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                    uint labelRelAddress = textLabelOffset - instructionOffset - 2;
                    Logging.LogDebug($".text replace .text label (R) \"{token.Value}\" --> {labelRelAddress:x8}h / {labelRelAddress}");
                    token.Type = TokenType.NUMERIC_VALUE;
                    token.Value = labelRelAddress.ToString();
                }
                else if (adrMode == AddressingMode.Absolute)
                {
                    uint labelAbsAddress = globalLoadAddress + dataSection.Size + textLabelOffset;
                    Logging.LogDebug($".text replace .text label (A) \"{token.Value}\" --> {labelAbsAddress:x8}h / {labelAbsAddress}");
                    token.Type = TokenType.NUMERIC_VALUE;
                    token.Value = labelAbsAddress.ToString();
                }
            }

            // .data label reference
            if (dataSection.LabelAddressMap.TryGetValue(token.Value, out uint dataLabelOffset))
            {
                if (adrMode == AddressingMode.PCRelative)
                {
                    // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                    uint labelRelAddress = dataLabelOffset - dataSection.Size - instructionOffset - 2;
                    Logging.LogDebug($".text replace .data label (R) \"{token.Value}\" --> {labelRelAddress:x8}h / {labelRelAddress}");
                    token.Type = TokenType.NUMERIC_VALUE;
                    token.Value = labelRelAddress.ToString();
                }
                else if (adrMode == AddressingMode.Absolute)
                {
                    uint labelAbsAddress = globalLoadAddress + dataLabelOffset;
                    Logging.LogDebug($".text replace .data label (A) \"{token.Value}\" --> {labelAbsAddress:x8}h / {labelAbsAddress}");
                    token.Type = TokenType.NUMERIC_VALUE;
                    token.Value = labelAbsAddress.ToString();
                }
            }

            // .data constant reference
            if (dataSection.ImmediateConstants.Any(constant => constant.Name == token.Value))
            {
                ImmediateConstant constant = dataSection.ImmediateConstants.First(constant => constant.Name == token.Value);
                // Replace instruction operand with immediate value.
                Logging.LogDebug($".text replace single word / constant (R) \"{token.Value}\" --> {constant.Value:x8}h / {constant.Value}");
                token.Type = TokenType.NUMERIC_VALUE;
                token.Value = constant.Value.ToString();
            }

            // At this point, the token type should be a numeric value, so if no replacement was found
            // this indicates the word doesn't map to anything useful for further assembling.
            if (token.Type == TokenType.LITERAL_WORD)
                throw new Exception($"Unresolved reference {token.Value} in {instructionStatement}");
        }
        return instructionStatement;
    }

    /// <summary>
    /// If the instruction contains arithmetic constant expressions (e.g. 5 + 3), evaluate those
    /// to reduce them to their values.
    /// </summary>
    /// <param name="instructionStatement"></param>
    /// <returns></returns>
    private static Statement EvaluateConstantExpressions(Statement instructionStatement)
    {
        List<Statement> constantExpressions = [];
        // Search constant expressions:
        // For every numeric value, search for arithmetic symbols after it, then merge an entire expression consisting of arithmetic and numbers.
        List<List<Token>> expressionTokens = [];
        for (int tokenIdx = 1; tokenIdx < instructionStatement.Tokens.Length - 1; tokenIdx++)
        {
            Token previous = instructionStatement.Tokens[tokenIdx - 1];
            Token current = instructionStatement.Tokens[tokenIdx];
            Token next = instructionStatement.Tokens[tokenIdx + 1];
            if (previous.Type != TokenType.SYMBOL_ARITHMETIC_OP && current.Type == TokenType.NUMERIC_VALUE && next.Type == TokenType.SYMBOL_ARITHMETIC_OP)
                expressionTokens.Add([current]); // Open new expression
            else if (previous.Type == TokenType.NUMERIC_VALUE && current.Type == TokenType.SYMBOL_ARITHMETIC_OP)
                expressionTokens[^1].Add(current);
            else if (previous.Type == TokenType.SYMBOL_ARITHMETIC_OP && current.Type == TokenType.NUMERIC_VALUE)
                expressionTokens[^1].Add(current);
        }
        // Convert token lists to expressions and ensure all numeric values are in "flat" base-10
        foreach (List<Token> expressionTokenList in expressionTokens)
        {
            foreach (Token token in expressionTokenList)
                if (token.Type == TokenType.NUMERIC_VALUE)
                    token.Value = ConvertStringToUInt(token.Value).ToString();
            constantExpressions.Add(new Statement([.. expressionTokenList, Token.CreateEOS()]));
        }
        // Evaluate constant expressions and substitute them
        foreach (Statement expression in constantExpressions)
        {
            // Evaluate constant expression
            Logging.LogDebug($"Evaluating constant expression {expression}");
            ArithmeticExpression<UInt32> arithmeticExpression = new(expression);
            uint expressionValue = arithmeticExpression.Evaluate(throwExceptionOnOverflow: false);
            Logging.LogDebug($"= {expressionValue}");
            if (arithmeticExpression.HasOverflown)
                Logging.LogWarn("Warning: Expression has overflown. This might be intended behavior.");
            // TODO: This could be made more clean?
            int expressionTokenIdx = Array.IndexOf(instructionStatement.Tokens, expression.Tokens[0]);
            List<Token> instructionTokensNew = [.. instructionStatement.Tokens];
            instructionTokensNew.RemoveAll(token => expression.Tokens.Contains(token));
            instructionTokensNew.Insert(expressionTokenIdx, new Token(TokenType.NUMERIC_VALUE, expressionValue.ToString()));
            instructionStatement = new Statement([.. instructionTokensNew]);
        }
        return instructionStatement;
    }
}