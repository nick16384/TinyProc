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
    internal static DataSection CreateFromAssemblyCode(string assemblyCodeDataSection)
    {
        List<string> lines = [.. assemblyCodeDataSection.Split("\n")];
        lines = FilterCommentsAndRemoveExcessWhitespace(lines);

        string[] headerWords = SplitLineIntoWords(lines[0]);
        if (headerWords[0] != ASM_DIRECTIVE_SECTION || headerWords.Last() != ASM_DIRECTIVE_SECTION_DATA)
            throw new ArgumentException("Cannot parse assembly .data section: Incorrect header");
        // No need for attribute parsing - will maybe be necessary in future
        Logging.LogDebug("Successfully verified and parsed .data section header.");

        lines.RemoveAt(0);
        if (!lines.Any(line => !string.IsNullOrEmpty(line)))
        {
            // If no further lines exist or they're all empty, the .data section is empty.
            Logging.LogDebug(".data section is empty.");
            return DataSection.CreateEmpty();
        }

        // Main parser for .data section
        List<ImmediateSequence> data = ExtractImmediateSequences([.. lines]);

        // Log immediate values and pointers (summary)
        Logging.LogDebug($"Successfully parsed {data.Count} immediate sequences.");

        DataSection resultDataSection = new([.. data]);
        Logging.LogDebug($"Successfully parsed .data section into a total of {resultDataSection.Size} word(s).");
        return resultDataSection;
    }

    private static List<ImmediateSequence> ExtractImmediateSequences(string[] lines)
    {
        List<ImmediateSequence> immediateSequences = [];
        uint currentOffset = 0;

        for (int lineCount = 0; lineCount < lines.Length; lineCount++)
        {
            string line = lines[lineCount];
            // Split into words, but preserve values in quotation marks, and remove commas
            string[] words = SplitLineIntoWords(line);

            // Pointers (word sequences)
            // Usage:
            // dw name* [sequence of values]
            // e.g.
            // dw ptr1 "Hello, world!", 0xA
            // *: Name is optional
            if (words[0] == KEYWORD_DEFINEWORD)
            {
                if (words.Length < 2)
                    throw new ArgumentException("Number of literal words in word sequence declaration is less than 2.");
                // Look if second word contains data immediately, or an alias comes first
                bool hasAlias = !TryParseImmediateSequence(words[1..], out _);
                string? alias = hasAlias ? words[1] : null;
                string[] dataStrings = hasAlias ? words[2..] : words[1..];
                List<uint> data;
                if (dataStrings[0] == KEYWORD_SPECIAL_LENGTH)
                    data = [(uint)immediateSequences.First(seq => seq.Alias == dataStrings[1]).Data.Length];
                else
                {
                    bool parseSuccess = TryParseImmediateSequence(dataStrings, out List<byte> dataBytes);
                    if (!parseSuccess)
                        throw new Exception($"Part of word sequence is neither a string in quotation marks nor a numeric value: {string.Join(", ", dataStrings)}");
                    data = ByteSequenceToUIntSequence(dataBytes);
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
            else if (words[0] == KEYWORD_EQUATE)
            {
                if (words.Length < 3)
                    throw new ArgumentException("Number of literal words in constant declaration is less than 3.");
                string alias = words[1];
                string[] dataStrings = words[2..];
                List<uint> data;
                if (dataStrings[0] == KEYWORD_SPECIAL_LENGTH)
                    data = [(uint)immediateSequences.First(seq => seq.Alias == dataStrings[1]).Data.Length];
                else
                {
                    bool parseSuccess = TryParseImmediateSequence(dataStrings, out List<byte> dataBytes);
                    if (!parseSuccess)
                        throw new Exception($"Part of constant is neither a string in quotation marks nor a numeric value: {string.Join(", ", dataStrings)}");
                    data = ByteSequenceToUIntSequence(dataBytes);
                }
                immediateSequences.Add(new ImmediateSequence(alias, null, [.. data]));
            }

            var sequencesWithSameNameButDifferentValues =
                immediateSequences.Where(seq1 => immediateSequences.Any(seq2 => seq1.Alias == seq2.Alias && seq1 != seq2));
            if (sequencesWithSameNameButDifferentValues.Count() >= 2)
                throw new Exception($"Two immediate sequences with the same alias: {sequencesWithSameNameButDifferentValues.First().Alias}");
        }
        return immediateSequences;
    }

    private static bool TryParseImmediateSequence(string[] dataStrings, out List<byte> data)
    {
        data = [];
        foreach (string dataStr in dataStrings)
        {
            // Parse as numeric value
            if (TryConvertStringToUInt(dataStr, out uint? num))
            {
                data.Add((byte)((num! & 0xFF000000) >> 24));
                data.Add((byte)((num! & 0x00FF0000) >> 16));
                data.Add((byte)((num! & 0x0000FF00) >> 8));
                data.Add((byte)((num! & 0x000000FF) >> 0));
                continue;
            }
            // Parse as string
            if (dataStr.StartsWith('"') && dataStr.EndsWith('"'))
            {
                // Convert string literals to uint sequences
                string dataStrWithoutQuotes = new([.. dataStr.Skip(1).SkipLast(1)]);
                for (int i = 0; i < dataStr.Length; i++)
                    data.Add((byte)dataStrWithoutQuotes[i]);
                continue;
            }
            // Unable to parse
            return false;
        }
        // Everything parsed successfully
        return true;
    }

    private static List<uint> ByteSequenceToUIntSequence(List<byte> byteSequence)
    {
        int paddingBytesRequired = byteSequence.Count % sizeof(uint);
        for (int i = 0; i < paddingBytesRequired; i++)
            byteSequence.Add(0x00b);
        List<uint> uintSequence = new(byteSequence.Count / 4);
        for (int i = 0; i < uintSequence.Count; i++)
        {
            uint bytesAsUInt =
                ((uint)byteSequence[i*sizeof(uint) + 0] << 24) |
                ((uint)byteSequence[i*sizeof(uint) + 1] << 16) |
                ((uint)byteSequence[i*sizeof(uint) + 2] << 8)  |
                ((uint)byteSequence[i*sizeof(uint) + 3] << 0);
            uintSequence.Add(bytesAsUInt);
        }
        return uintSequence;
    }
}