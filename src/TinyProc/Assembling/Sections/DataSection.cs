using static TinyProc.Assembling.Sections.DataSection;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;

namespace TinyProc.Assembling.Sections;

public readonly struct DataSection(ImmediateSequence[] immediateSequences) : IAssemblySection
{
    public uint Size => (uint)immediateSequences.Sum(seq => seq.Data.Length);
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

    public static DataSection CreateEmpty() => new(immediateSequences: []);

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
        List<ImmediateSequence> data = ExtractImmediateSequences(assemblyStatements);

        // Log immediate values and pointers (summary)
        Logging.LogDebug($"Successfully parsed {data.Count} immediate sequences.");

        DataSection resultDataSection = new([.. data]);
        Logging.LogDebug($"Successfully parsed .data section into a total of {resultDataSection.Size} word(s).");

        return resultDataSection;
    }

    private static List<ImmediateSequence> ExtractImmediateSequences(List<Statement> statements)
    {
        List<ImmediateSequence> immediateSequences = [];
        uint currentOffset = 0;

        foreach (Statement statement in statements)
        {
            Logging.LogDebug($"Decoding {statement}");
            // Pointers (word sequences)
            // Usage:
            // dw name* [sequence of values]
            // e.g.
            // dw ptr1 "Hello, world!", 0xA
            // *: Name is optional
            if (statement.Tokens[0].Type == TokenType.KEYWORD_DEFINEWORD)
            {
                Logging.LogDebug($"DEFINEWORD");
                if (statement.STLength < 2)
                    throw new ArgumentException("Number of literal words in word sequence declaration is less than 2.");
                // Look if second word contains data immediately, or an alias comes first
                bool hasAlias = statement.Tokens[1].Type == TokenType.LITERAL_WORD;
                string? alias = hasAlias ? statement.Tokens[1].Value : null;
                Token[] dataTokens = hasAlias ? statement.Tokens[2..^1] : statement.Tokens[1..^1];
                List<uint> data;
                Logging.LogDebug($"Alias: {(hasAlias ? alias : "<none>")}, Data tokens: {dataTokens.Length}");
                if (dataTokens[0].Type == TokenType.KEYWORD_LENGTH)
                {
                    Logging.LogDebug("Length specifier.");
                    // Automatically LE
                    uint length = (uint)immediateSequences.First(seq => seq.Alias == dataTokens[1].Value).Data.Length;
                    Logging.LogDebug($"Target: {dataTokens[1].Value}, Size: {length}");
                    data = [length];
                }
                else
                {
                    Logging.LogDebug("Data.");
                    List<byte> dataBytes = ParseImmediateSequence(dataTokens);
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
                currentOffset += (uint)data.Count * sizeof(uint);
                immediateSequences.Add(new ImmediateSequence(alias, currentOffset, [.. data]));
            }

            // Constants (labeled single words)
            // Usage:
            // dw name* [single value]
            // e.g.
            // equ const1 0xA
            // *: Name is required
            else if (statement.Tokens[0].Type == TokenType.KEYWORD_EQUATE)
            {
                Logging.LogDebug($"EQUATE");
                if (statement.STLength <= 3)
                    throw new ArgumentException("Number of literal words in constant declaration is less than 3.");
                string alias = statement.Tokens[1].Value;
                Token[] dataTokens = statement.Tokens[2..^1];
                List<uint> data;
                Logging.LogDebug($"Alias: {alias}, Data tokens: {dataTokens.Length}");
                if (dataTokens[0].Type == TokenType.KEYWORD_LENGTH)
                {
                    Logging.LogDebug("Length specifier.");
                    // Automatically LE
                    uint length = (uint)immediateSequences.First(seq => seq.Alias == dataTokens[1].Value).Data.Length;
                    Logging.LogDebug($"Target: {dataTokens[1].Value}, Size: {length}");
                    data = [length];
                }
                else
                {
                    Logging.LogDebug("Data.");
                    List<byte> dataBytes = ParseImmediateSequence(dataTokens);
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

            else
            {
                // First token not valid (DW or EQU)
                throw new Exception($"Invalid statement {statement} found in .data section.");
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
        return immediateSequences;
    }

    private static List<byte> ParseImmediateSequence(Token[] dataTokens)
    {
        List<byte> data = [];
        foreach (Token dataToken in dataTokens)
        {
            // Parse as numeric value
            if (dataToken.Type == TokenType.NUMERIC_VALUE)
            {
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