using static TinyProc.Assembling.Sections.DataSection;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;

namespace TinyProc.Assembling.Sections;

public readonly struct DataSection(ImmediateSequence[] immediateSequences, Dictionary<string, uint> labelAddressMap) : IAssemblySection
{
    public uint Size => (uint)immediateSequences.Sum(seq => seq.Data.Length);
    public Dictionary<string, uint> LabelAddressMap { get; } = labelAddressMap;
    public List<uint> BinaryRepresentation
        => [..
            immediateSequences
            .Where(seq => seq.Offset.HasValue)
            .Select(seq => seq.Data)
            .SelectMany(x => x)
            ];
    public List<ImmediateSequence> ImmediateSequences { get => [.. immediateSequences]; }

    public readonly struct ImmediateSequence
    {
        public readonly string? Alias = null;
        public readonly uint? Offset = null;
        public readonly uint[] Data = [];
        public bool HasAlias { get => Alias != null; }
        public bool HasOffset { get => Offset.HasValue; }
        /// <summary>
        /// Declares a sequence of immediate values / words.
        /// </summary>
        /// <param name="alias">The name this sequence is referred to as in code. If null, there is no name.</param>
        /// <param name="offset">The offset in the .data section, at which this sequence starts.
        /// If null, data is 1 word long and this therefore a pure alias for the actual value (similar to #define).</param>
        /// <param name="data">The data this sequence holds.</param>
        public ImmediateSequence(string? alias, uint? offset, uint[] data)
        {
            Alias = alias;
            if (offset == null && data.Length > 1)
                throw new Exception("Cannot initialize word sequence as an alias with multiple words.");
            Offset = offset;
            Data = data;
        }

        public static bool operator ==(ImmediateSequence seq1, ImmediateSequence seq2)
        {
            return
                seq1.Alias == seq2.Alias &&
                seq1.Offset == seq2.Offset &&
                seq1.Data == seq2.Data;
        }
        public static bool operator !=(ImmediateSequence seq1, ImmediateSequence seq2) => !(seq1 == seq2);
        public override bool Equals(object? obj) => object.Equals(obj, this);
        public override int GetHashCode() => base.GetHashCode();
    }

    public static DataSection CreateEmpty() => new(immediateSequences: [], labelAddressMap: []);

    /// <summary>
    /// Parses the data section of assembly code into a DataSection object.
    /// </summary>
    /// <param name="assemblyCodeDataSection"></param>
    /// <returns></returns>
    internal static DataSection CreateFromAssemblyCode(List<Statement> assemblyStatements)
    {
        Statement header = assemblyStatements[0];
        if (header.STLength < 2 || header.Tokens[0].Type != TokenType.DIRECTIVE_SECTION || header.Tokens[^2].Type != TokenType.DIRECTIVE_SECTION_DATA)
            throw new ArgumentException("Cannot parse assembly .data section: Incorrect header");
        // No need for attribute parsing - will maybe be necessary in future
        Logging.LogDebug("Successfully verified and parsed .data section header.");
        assemblyStatements.Remove(header);
        if (assemblyStatements.Count <= 0)
        {
            // If no further statements exist, the .data section is empty.
            Logging.LogDebug(".data section is empty.");
            return DataSection.CreateEmpty();
        }

        // Main parser for .data section
        (List<ImmediateSequence>, Dictionary<string, uint>) dataAndLabels = ExtractImmediateSequencesAndLabels(assemblyStatements);
        List<ImmediateSequence> data = dataAndLabels.Item1;
        Dictionary<string, uint> labelAddressMap = dataAndLabels.Item2;

        // Log immediate values and pointers (summary)
        Logging.LogDebug($"Successfully parsed {data.Count} immediate sequences.");

        DataSection resultDataSection = new([.. data], labelAddressMap);
        Logging.LogDebug($"Successfully parsed .data section into a total of {resultDataSection.Size} word(s).");

        return resultDataSection;
    }

    /// <summary>
    /// Takes in a list of assembly statements and extracts immediate sequences and address labels.
    /// </summary>
    /// <param name="statements"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static (List<ImmediateSequence> ImmediateSequences, Dictionary<string, uint> LabelAddressMap) ExtractImmediateSequencesAndLabels(List<Statement> statements)
    {
        List<ImmediateSequence> immediateSequences = [];
        Dictionary<string, uint> labelAddressMap = [];
        uint currentOffset = 0;

        foreach (Statement statement in statements)
        {
            Logging.LogDebug($"Decoding {statement}");
            Token[] statementTokens = new Token[statement.STLength];
            // Create a copy to prevent modification of the original statement.
            // Note the EOS token is missing, since it isn't required for parsing immediate sequences.
            Array.Copy(statement.Tokens, statementTokens, statement.STLength);

            if (statementTokens[0].Type == TokenType.LABEL)
            {
                Logging.LogDebug($"Label: {statementTokens[0].Value}, Offset: {currentOffset:x8}");
                labelAddressMap.Add(statementTokens[0].Value, currentOffset);
                statementTokens = statementTokens[1..];
            }

            if (statementTokens.Length <= 1)
            {
                throw new Exception($"Invalid statement {statement} ended prematurely.");
            }
            if (statementTokens[0].Type != TokenType.KEYWORD_DEFINEWORD && statementTokens[0].Type != TokenType.KEYWORD_EQUATE)
            {
                // First token not valid (DW or EQU)
                throw new Exception($"Invalid statement {statement} found in .data section. Starting with wrong token.");
            }

            // Pointers (word sequences)
            // Usage:
            // dw name* [sequence of values]
            // e.g.
            // dw ptr1 "Hello, world!", 0xA
            // *: Name is optional
            if (statementTokens[0].Type == TokenType.KEYWORD_DEFINEWORD)
            {
                Logging.LogDebug($"DEFINEWORD");
                if (statementTokens.Length < 2)
                    throw new ArgumentException("Number of tokens in word sequence declaration is less than 2.");
                // From here, there are two possible syntax cases:
                // 1. The immediate sequence is a single word. The syntax looks as follows: <label> dw <alias> value (label & alias are optional)
                // 2. The immediate sequence is a multi-word. The syntax looks as follows: <label> dw values... (label is optional)
                // Note that the label has been removed beforehand.

                // Look if second word contains data immediately, or an alias comes first
                bool hasAlias = statementTokens[1].Type == TokenType.LITERAL_WORD;
                string? alias = hasAlias ? statementTokens[1].Value : null;
                Token[] dataTokens = hasAlias ? statementTokens[2..] : statementTokens[1..];
                Logging.LogDebug($"Alias: {(hasAlias ? alias : "<none>")}, Data tokens: {dataTokens.Length}");
                List<uint> data;
                if (dataTokens[0].Type == TokenType.KEYWORD_LENGTH)
                {
                    Logging.LogDebug("Length specifier.");
                    Token reference = dataTokens[1];
                    uint referenceOffset = labelAddressMap[reference.Value];
                    ImmediateSequence referenceSequence = immediateSequences.First(seq => seq.Offset == referenceOffset);
                    Logging.LogDebug($"Reference: {reference.Value}, Offset: {referenceOffset:x8}, Size: {referenceSequence.Data.Length}");
                    // Automatically LE
                    data = [(uint)referenceSequence.Data.Length];
                }
                else
                {
                    Logging.LogDebug("Data.");
                    List<byte> dataBytes = ParseImmediateSequenceData(dataTokens);
                    // TLDR: To prevent confusion, multi-word immediate sequences may not be named (i.e. have an alias) and must be referenced to by a label.
                    // Long explanation:
                    // In the previous iteration of ASMv3.1, multi-word sequences (i.e. immediate sequences with more than one word)
                    // were allowed to be named / aliased. This meant that e.g.
                    // "dw name1 0xffffffff" would produce a singular word with name "name1", while
                    // "dw name2 0xffffffff, 0xffffffff" would produce a multi-word with name "name2".
                    // The problem arises in references to "name2" in the .text section: If name2 were to be a single word, its value
                    // would be substituted into the instruction operand, whereas if name2 as a multi-word now only inserts its address into
                    // the instruction operand.
                    // Editing and debugging this assembly gimmick would have caused some nightmares, so the decision was made that multi-words
                    // must not have an alias and instead the usage of explicit labels (which all are addresses / offsets) was enforced to clearly differentiate
                    // between immediate values and pointers.
                    // The len: keyword must, consequently, also reference the multi-word as a pointer instead of the name itself.
                    if (hasAlias && dataBytes.Count > sizeof(uint))
                        throw new Exception($"ASMv3.1 standard forbids named multi-word sequences: {statement}");
                    if (dataBytes.Count < sizeof(uint) && !dataTokens.Any(token => token.Type == TokenType.STRING))
                    {
                        data = ByteSequenceToUIntSequence(dataBytes, encodeAsLittleEndian: true);
                        Logging.LogDebug($"Encoded as LE, Size (words): {data.Count}");
                    }
                    else
                    {
                        data = ByteSequenceToUIntSequence(dataBytes);
                        Logging.LogDebug($"Encoded as BE, Size (words): {data.Count}");
                    }
                }
                immediateSequences.Add(new ImmediateSequence(alias, currentOffset, [.. data]));
                currentOffset += (uint)data.Count;
            }

            // Constants (labeled single words)
            // Usage:
            // dw name* [single value]
            // e.g.
            // equ const1 0xA
            // *: Name is required
            else if (statementTokens[0].Type == TokenType.KEYWORD_EQUATE)
            {
                Logging.LogDebug($"EQUATE");
                if (statementTokens.Length < 3)
                    throw new ArgumentException("Number of tokens in constant declaration is less than 3.");
                if (labelAddressMap.Last().Value == currentOffset)
                    throw new Exception("Equate cannot have a label: Constants are operand-injected and don't reside within .data");
                string alias = statementTokens[1].Value;
                Token[] dataTokens = statementTokens[2..];
                List<uint> data;
                Logging.LogDebug($"Alias: {alias}, Data tokens: {dataTokens.Length}");
                if (dataTokens[0].Type == TokenType.KEYWORD_LENGTH)
                {
                    Logging.LogDebug("Length specifier.");
                    Token reference = dataTokens[1];
                    uint referenceOffset = labelAddressMap[reference.Value];
                    ImmediateSequence referenceSequence = immediateSequences.First(seq => seq.Offset == referenceOffset);
                    Logging.LogDebug($"Reference: {reference.Value}, Offset: {referenceOffset:x8}, Size: {referenceSequence.Data.Length}");
                    // Automatically LE
                    data = [(uint)referenceSequence.Data.Length];
                }
                else
                {
                    Logging.LogDebug("Data.");
                    List<byte> dataBytes = ParseImmediateSequenceData(dataTokens);
                    if (dataBytes.Count < sizeof(uint) && !dataTokens.Any(token => token.Type == TokenType.STRING))
                    {
                        data = ByteSequenceToUIntSequence(dataBytes, encodeAsLittleEndian: true);
                        Logging.LogDebug($"Encoded as LE, Size (words): {data.Count}");
                    }
                    else
                    {
                        data = ByteSequenceToUIntSequence(dataBytes);
                        Logging.LogDebug($"Encoded as BE, Size (words): {data.Count}");
                    }
                }
                immediateSequences.Add(new ImmediateSequence(alias, null, [.. data]));
            }

            var sequencesWithSameNameButDifferentValues = immediateSequences
                .Where(seq1 => immediateSequences.Any(seq2 =>
                    seq1.HasAlias &&
                    seq2.HasAlias &&
                    seq1.Alias == seq2.Alias &&
                    seq1 != seq2
                ));
            if (sequencesWithSameNameButDifferentValues.Count() >= 2)
                throw new Exception($"Multiple immediate sequences with the same alias: {sequencesWithSameNameButDifferentValues.First().Alias}");
            Logging.NewlineDebug();
        }
        return (immediateSequences, labelAddressMap);
    }

    /// <summary>
    /// Parses the data of a single immediate sequence and returns a list of data bytes.
    /// </summary>
    /// <param name="dataTokens"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static List<byte> ParseImmediateSequenceData(Token[] dataTokens)
    {
        List<byte> data = [];
        foreach (Token dataToken in dataTokens)
        {
            // Parse as numeric value
            if (dataToken.Type == TokenType.NUMERIC_VALUE)
            {
                // FIXME: When the code contains something like "0x00000000", we want all those bytes to appear in the final binary.
                // The current solution only appends a single byte of those four specified.
                uint num = ConvertStringToUInt(dataToken.Value);
                // Only add bytes after the first non-zero byte.
                // I see this singular use of goto as justified. Suggest something better to convince me otherwise.
                if      ((num & 0xFF000000) != 0u) goto label_byte4;
                else if ((num & 0x00FF0000) != 0u) goto label_byte3;
                else if ((num & 0x0000FF00) != 0u) goto label_byte2;
                else if (true)                     goto label_byte1; // First byte is seen as data nonetheless

                label_byte4: data.Add((byte)((num & 0xFF000000) >> 24));
                label_byte3: data.Add((byte)((num & 0x00FF0000) >> 16));
                label_byte2: data.Add((byte)((num & 0x0000FF00) >> 8));
                label_byte1: data.Add((byte)((num & 0x000000FF) >> 0));
                
                continue;
            }
            // Parse as string
            if (dataToken.Type == TokenType.STRING)
            {
                // Convert string literals to uint sequences
                data.AddRange(dataToken.AsByteArray());
                continue;
            }
            if (dataToken.Type == TokenType.EOS)
            {
                Logging.LogWarn("Internal warning: Immediate data contains EOS token! Aborting possibly early.");
                break;
            }
            // Unable to parse
            throw new Exception($"Invalid token type {dataToken.Type} for immediate sequence data.");
        }
        // Everything parsed successfully
        return data;
    }

    /// <summary>
    /// Converts a list of bytes to a list of uints in big endian.
    /// If the number of bytes doesn't fit evenly into the uints, padding bytes (0x00) are added.
    /// </summary>
    /// <param name="byteSequence"></param>
    /// <param name="encodeAsLittleEndian">Specifies whether to fill uints in LE or BE order. Default is BE.</param>
    /// <returns></returns>
    private static List<uint> ByteSequenceToUIntSequence(List<byte> byteSequence, bool encodeAsLittleEndian = false)
    {
        uint[] uintSequence = new uint[(int)Math.Ceiling((double)byteSequence.Count / 4)];
        Array.Fill(uintSequence, 0u); // Just to be sure, fill with zeroes (incl. padding bytes)
        for (int i = 0; i < byteSequence.Count; i++)
        {
            int uintArrayIdx = i / 4;
            int byteIdx = i % 4;
            // The reason the "useLE" parameter is given comes from encoding issues:
            // Strings are encoded sequentially (in BE order), raw data bytes from right to left (LE): This creates a conflict!
            // This function cannot differentiate these two, since it treats them all the same.
            if (encodeAsLittleEndian)
                uintSequence[uintArrayIdx] |= (uint)byteSequence[i] << (8 * byteIdx);
            else
                uintSequence[uintArrayIdx] |= (uint)byteSequence[i] << (24 - (8 * byteIdx));
        }
        return [.. uintSequence];
    }
}