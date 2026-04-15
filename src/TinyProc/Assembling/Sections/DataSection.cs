using static TinyProc.Assembling.Sections.DataSection;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;

namespace TinyProc.Assembling.Sections;

public readonly struct DataSection(ImmediateSequence[] immediateSequences, ImmediateConstant[] immediateConstants, Dictionary<string, uint> labelAddressMap) : IAssemblySection
{
    public uint Size => (uint)immediateSequences.Sum(seq => seq.Data.Length);
    public Dictionary<string, uint> LabelAddressMap { get; } = labelAddressMap;
    public List<uint> BinaryRepresentation
        => [..
            immediateSequences
            .Select(seq => seq.Data)
            .SelectMany(x => x)
            ];
    public List<ImmediateConstant> ImmediateConstants { get => [.. immediateConstants]; }
    public List<ImmediateSequence> ImmediateSequences { get => [.. immediateSequences]; }

    public readonly struct ImmediateConstant(string name, uint value)
    {
        public readonly string Name = name;
        public readonly uint Value = value;
        public static bool operator ==(ImmediateConstant const1, ImmediateConstant const2)
        {
            return
                const1.Name == const2.Name &&
                const1.Value == const2.Value;
        }
        public static bool operator !=(ImmediateConstant const1, ImmediateConstant const2) => !(const1 == const2);
        public override bool Equals(object? obj) => object.Equals(obj, this);
        public override int GetHashCode() => base.GetHashCode();
    }
    public readonly struct ImmediateSequence(string? label, uint offset, uint[] data)
    {
        public readonly string? Label = label;
        public readonly uint Offset = offset;
        public readonly uint[] Data = data;
        public bool HasLabel { get => Label != null; }

        public static bool operator ==(ImmediateSequence seq1, ImmediateSequence seq2)
        {
            return
                seq1.Label == seq2.Label &&
                seq1.Offset == seq2.Offset &&
                seq1.Data == seq2.Data;
        }
        public static bool operator !=(ImmediateSequence seq1, ImmediateSequence seq2) => !(seq1 == seq2);
        public override bool Equals(object? obj) => object.Equals(obj, this);
        public override int GetHashCode() => base.GetHashCode();
    }

    public static DataSection CreateEmpty() => new(immediateSequences: [], immediateConstants: [], labelAddressMap: []);

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
        (List<ImmediateSequence> ImmediateSequences, List<ImmediateConstant> Constants, Dictionary<string, uint> LabelAddressMap) sectionDataAll = ExtractData(assemblyStatements);
        List<ImmediateSequence> data = sectionDataAll.ImmediateSequences;
        List<ImmediateConstant> constants = sectionDataAll.Constants;
        Dictionary<string, uint> labelAddressMap = sectionDataAll.LabelAddressMap;

        DataSection resultDataSection = new([.. data], [.. constants], labelAddressMap);
        Logging.LogDebug("\n" +
            $"Successfully parsed .data section.\n" +
            $"Total size:..........{resultDataSection.Size} words\n" +
            $"Immediate sequences:.{data.Count}\n" +
            $"Constants:...........{constants.Count}\n" +
            $"Labels:..............{labelAddressMap.Count}"
        );
        Logging.NewlineDebug();

        return resultDataSection;
    }

    /// <summary>
    /// Takes in a list of assembly statements and extracts immediate sequences, constants and address labels.
    /// </summary>
    /// <param name="statements"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static (List<ImmediateSequence>, List<ImmediateConstant>, Dictionary<string, uint>) ExtractData(List<Statement> statements)
    {
        List<ImmediateSequence> immediateSequences = [];
        List<ImmediateConstant> constants = [];
        Dictionary<string, uint> labelAddressMap = [];
        uint currentOffset = 0;

        foreach (Statement statement in statements)
        {
            Logging.NewlineDebug();
            Logging.LogDebug($"Decoding {statement}");
            Token[] statementTokens = new Token[statement.STLength];
            // Create a copy to prevent modification of the original statement.
            // Note the EOS token is missing, since it isn't required for parsing immediate sequences.
            Array.Copy(statement.Tokens, statementTokens, statement.STLength);

            string? label = null;
            if (statementTokens[0].Type == TokenType.LABEL)
            {
                label = statementTokens[0].Value;
                Logging.LogDebug($"Label: {label}, Offset: {currentOffset:x8}");
                labelAddressMap.Add(label, currentOffset);
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

            // Word sequences
            // Usage:
            // label*: dw [sequence of values]
            // e.g.
            // dw ptr1 "Hello, world!", 0xA
            // *: Label is optional
            if (statementTokens[0].Type == TokenType.KEYWORD_DEFINEWORD)
            {
                Logging.LogDebug($"DEFINEWORD");
                if (statementTokens.Length < 2)
                    throw new ArgumentException("Number of tokens in word sequence declaration is less than 2.");

                Token[] dataTokens = statementTokens[1..];
                Logging.LogDebug($"Label: {label ?? "<none>"}, Data tokens: {dataTokens.Length}");
                List<uint> data;
                if (dataTokens[0].Type == TokenType.KEYWORD_LENGTH)
                {
                    Logging.LogDebug("Length specifier.");
                    Token reference = dataTokens[1];
                    ImmediateSequence referenceSequence = immediateSequences.First(seq => seq.Label == reference.Value);
                    Logging.LogDebug($"Reference: {reference.Value}, Size: {referenceSequence.Data.Length}");
                    // Automatically LE
                    data = [(uint)referenceSequence.Data.Length];
                }
                else
                {
                    Logging.LogDebug("Data.");
                    data = ParseDataFromTokens(dataTokens);
                    Logging.LogDebug($"Data as uints: {string.Join(", ", data.Select(word => word.ToString()))}");
                }
                immediateSequences.Add(new ImmediateSequence(label, currentOffset, [.. data]));
                currentOffset += (uint)data.Count;
            }

            // Constants (labeled single words)
            // Usage:
            // equ name [single value]
            // e.g.
            // equ const1 0xA
            else if (statementTokens[0].Type == TokenType.KEYWORD_EQUATE)
            {
                Logging.LogDebug($"EQUATE");
                if (statementTokens.Length < 3)
                    throw new ArgumentException("Number of tokens in constant declaration is less than 3.");
                if (label != null)
                    throw new Exception("Equate cannot have a label: Constants are operand-injected and don't reside within .data");
                
                string name = statementTokens[1].Value;
                Token[] dataTokens = statementTokens[2..];
                uint constantValue;
                Logging.LogDebug($"Name: {name}, Data tokens: {dataTokens.Length}");
                if (dataTokens[0].Type == TokenType.KEYWORD_LENGTH)
                {
                    Logging.LogDebug("Length specifier.");
                    Token reference = dataTokens[1];
                    ImmediateSequence referenceSequence = immediateSequences.First(seq => seq.Label == reference.Value);
                    Logging.LogDebug($"Reference: {reference.Value}, Size: {referenceSequence.Data.Length}");
                    // Automatically LE
                    constantValue = (uint)referenceSequence.Data.Length;
                }
                else
                {
                    Logging.LogDebug("Data.");
                    constantValue = ParseDataFromTokens(dataTokens)[0];
                    Logging.LogDebug($"As uint: {constantValue:x8}");
                    if (dataTokens.Length > 1)
                        throw new Exception("Constants cannot have more than one word of data.");
                }
                constants.Add(new ImmediateConstant(name, constantValue));
            }
        }
        // Check if multiple constants / sequences have the same name / label
        List<string> allNames = [];
        allNames.AddRange(immediateSequences.Where(seq => seq.HasLabel).Select(seq => seq.Label!));
        allNames.AddRange(constants.Select(constant => constant.Name));
        List<string> distinctNames = [];
        foreach (string name in allNames)
        {
            if (distinctNames.Contains(name))
                throw new Exception($"Duplicate name for sequence / constant {name}");
            distinctNames.Add(name);
        }
        Logging.NewlineDebug();
        return (immediateSequences, constants, labelAddressMap);
    }

    /// <summary>
    /// Parses the data of a list of data tokens and returns a list of data bytes.
    /// </summary>
    /// <param name="dataTokens"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static List<uint> ParseDataFromTokens(Token[] dataTokens)
    {
        List<byte> dataBytes = [];
        foreach (Token dataToken in dataTokens)
        {
            // Parse as numeric value
            if (dataToken.Type == TokenType.NUMERIC_VALUE)
            {
                uint num = ConvertStringToUInt(dataToken.Value);
                // This logic is to keep track of how many bytes the number string will be stored as given the assembly code.
                // Some examples:
                // 0x4040: 2 bytes
                // 0b10101010: 1 byte
                // 0x005555: 3 bytes
                // 0x00000000: 4 bytes
                // 68: 4 bytes (always 4 bytes for decimals)
                int keepBytesCount = 0;
                if (dataToken.Value.StartsWith("0x")) // Hex
                    keepBytesCount = dataToken.Value[2..].Length / 2;
                else if (dataToken.Value.EndsWith('h')) // Hex
                    keepBytesCount = dataToken.Value[..^1].Length / 2;
                else if (dataToken.Value.StartsWith("0b")) // Binary
                    keepBytesCount = dataToken.Value[2..].Length / 8;
                else // Decimal (keep 4 bytes regardless)
                    keepBytesCount = 4;
                
                Logging.LogDebug($"Numeric data: keeping {keepBytesCount} byte(s) from value {dataToken.Value}");
                if (keepBytesCount >= 4) dataBytes.Add((byte)((num & 0xFF000000) >> 24));
                if (keepBytesCount >= 3) dataBytes.Add((byte)((num & 0x00FF0000) >> 16));
                if (keepBytesCount >= 2) dataBytes.Add((byte)((num & 0x0000FF00) >> 8));
                if (keepBytesCount >= 1) dataBytes.Add((byte)((num & 0x000000FF) >> 0));
                
                continue;
            }
            // Parse as string
            if (dataToken.Type == TokenType.STRING)
            {
                // Convert string literals to uint sequences
                dataBytes.AddRange(dataToken.AsByteArray());
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
        // Data parsed successfully
        // FIXME: Okay so now a little bit of magic must happen for LE / BE data:
        // Strings are stored LTR
        // Bytes following strings are also stored LTR
        // Numbers are stored RTL
        // Welp
        throw new NotImplementedException("welp (see fixme above)");
        return ByteSequenceToUIntSequence(dataBytes);
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
        Logging.LogDebug($"Data is {byteSequence.Count} byte(s) long, adding {uintSequence.Length * sizeof(uint) - byteSequence.Count} padding byte(s)");
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