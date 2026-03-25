using static TinyProc.Assembling.Sections.DataSection;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;
using System.Drawing;

namespace TinyProc.Assembling.Sections;

public readonly struct DataSection(uint? fixedLoadAddress, ImmediateSequence[] immediateSequences) : IAssemblySection
{
    public uint Size => (uint)immediateSequences.Sum(seq => seq.Data.Length);
    public uint? FixedLoadAddress { get; } = fixedLoadAddress;
    public List<uint> BinaryRepresentation
        => [..
            immediateSequences
            .Where(seq => seq.Offset.HasValue)
            .Select(seq => seq.Data)
            .SelectMany(x => x)
            ];

    public readonly struct ImmediateSequence
    {
        public readonly string? Alias;
        public readonly uint? Offset;
        public readonly uint[] Data;
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
    }

    public static DataSection CreateEmpty() => new(fixedLoadAddress: null, immediateSequences: []);

    /// <summary>
    /// Parses the data section of assembly code into a DataSection object.
    /// </summary>
    /// <param name="assemblyCodeDataSection"></param>
    /// <returns></returns>
    internal static DataSection CreateFromAssemblyCode(string assemblyCodeDataSection)
    {
        List<string> lines = [.. assemblyCodeDataSection.Split("\n")];
        lines = FilterCommentsAndRemoveExcessWhitespace(lines);

        // These variables need to be determined by this parser (currently just one)
        uint? fixedLoadAddress;

        // Syntax does look a bit whacky, but for future attribute additions, .Item1 .Item2 etc. syntax might be used.
        uint? headerAttributes = ParseHeader(lines[0]);
        fixedLoadAddress = headerAttributes;
        if (fixedLoadAddress.HasValue)
            Logging.LogDebug($".data section is fixed at address {fixedLoadAddress:x8}");
        else
            Logging.LogDebug($".data section is relocatable");

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

        DataSection resultDataSection = new(fixedLoadAddress, [.. data]);
        Logging.LogDebug($"Successfully parsed .data section into a total of {resultDataSection.Size} word(s).");
        return resultDataSection;
    }

    /// <summary>
    /// Processes the header of the .data section
    /// Mostly similar to the header of the .text section, but not identical,
    /// since some attributes are section specific.
    /// </summary>
    /// <param name="lines"></param>
    /// <returns>A possibly fixed load address.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static uint? ParseHeader(string headerLine)
    {
        uint? fixedLoadAddress = null;
        // Parse section header
        string[] headerWords = SplitLineIntoWords(headerLine);
        if (headerWords[0] != ASM_DIRECTIVE_SECTION || headerWords.Last() != ASM_DIRECTIVE_SECTION_DATA)
            throw new ArgumentException("Cannot parse assembly .data section: Incorrect header");

        if (headerWords.Length > 2)
        {
            // Header contains attributes
            // Remove first word "#SECTION" and last word ".data"
            headerWords = [.. headerWords.Skip(1).SkipLast(1)];
            // Split by brackets
            string[] attributes =
                [.. string.Join(" ", headerWords)
                .Split(["(", ")"], StringSplitOptions.RemoveEmptyEntries)];

            foreach (string attribute in attributes)
            {
                string[] attributeWords = attribute.Split([" ", "="], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                // Check if the attribute is even in the correct format
                if (attributeWords.Length != 3)
                    throw new Exception($"Unable to parse attribute \"{attribute}\". Too many or too few words.");
                string attributeKeyword = attributeWords[0];
                string identifier = attributeWords[1];
                string value = attributeWords[2];
                if (attributeKeyword != ASM_ATTRIBUTE)
                    throw new Exception($"Unable to parse attribute \"{attribute}\". Missing attribute keyword \"{ASM_ATTRIBUTE}\".");

                // Extract attribute values
                // TODO: Throw exception when encountering same attribute multiple times
                switch (identifier)
                {
                    case ASM_ATTRIBUTE_SECTION_LOADADDRESS:
                        fixedLoadAddress = ConvertStringToUInt(value);
                        break;
                    default:
                        throw new ArgumentException($"Invalid attribute identifier \"{identifier}\"");
                }

                // Identify possibly illegal attribute combinations
                // None
            }
        }
        Logging.LogDebug("Successfully verified and parsed .data section header.");

        return fixedLoadAddress;
    }

    private static List<ImmediateSequence> ExtractImmediateSequences(string[] lines)
    {
        List<ImmediateSequence> immediateSequences = [];
        uint currentOffset = 0;

        for (int lineCount = 0; lineCount < lines.Length; lineCount++)
        {
            string line = lines[lineCount];
            string[] words = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            // Pointers
            // Usage:
            // dw name* [sequence of values] // equ name [single value]
            // e.g.
            // dw ptr1 "Hello, world!", 0xA
            // or
            // equ ref1 0xAA
            // TODO: Implement proper parsing for dw / equ
            throw new NotImplementedException();
            if (words[0] == KEYWORD_DEFINEWORD)
            {
                if (words.Length < 3)
                    throw new ArgumentException("Number of literal words in word sequence declaration is less than 4.");
                string name = new([.. words[1].SkipLast(1)]);
                string assignment = words[1].Last().ToString();
                // A value can consist of multiple concatenated values
                string[] valueAsString = [.. words[2..]
                .Select(value => value.Split("+", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .SelectMany(a => a)];
                if (assignment != ",")
                    throw new ArgumentException($"Assignment operator \",\" expected. Got \"{assignment}\" instead.");
                
                // Convert string literals to uint sequences
                foreach (string word in words)
                {
                    if (word.StartsWith('\"') && word.EndsWith('\"'))
                    {
                        string wordWithoutQuotes = new([.. word.Skip(1).SkipLast(1)]);
                        List<string> wordUInts = [];
                        for (int i = 0; i < wordWithoutQuotes.Length; i += 4)
                        {
                            // Each block of 4 letters can be represented as a uint
                            uint char1 = (i + 0) < wordWithoutQuotes.Length ? wordWithoutQuotes[i + 0] : 0u;
                            uint char2 = (i + 1) < wordWithoutQuotes.Length ? wordWithoutQuotes[i + 1] : 0u;
                            uint char3 = (i + 2) < wordWithoutQuotes.Length ? wordWithoutQuotes[i + 2] : 0u;
                            uint char4 = (i + 3) < wordWithoutQuotes.Length ? wordWithoutQuotes[i + 3] : 0u;
                            uint textBlockAsUInt =
                                    char1 << 24
                                | char2 << 16
                                | char3 << 8
                                | char4 << 0;
                            wordUInts.Add(textBlockAsUInt.ToString("x8") + "h");
                        }
                        string wordUIntRepresentation = string.Join(" + ", wordUInts);
                        line = line.Replace(word, wordUIntRepresentation);
                        Logging.LogDebug($"Replaced string literal {word} with uint sequence {wordUIntRepresentation}");
                    }
                }

                List<uint> valueAsUIntArray = [];
                foreach (string value in valueAsString)
                {
                    try
                    {
                        uint valueAsUInt = ConvertStringToUInt(value);
                        valueAsUIntArray.Add(valueAsUInt);
                    }
                    catch (Exception)
                    {
                        throw new Exception($"Unable to parse pointer value \"{value}\" as uint.");
                    }
                }
                pointers.Add(name, new ImmediateSequence(currentOffset, [.. valueAsUIntArray]));
                Logging.LogDebug($"Pointer \"{name}\" points to data: \"{string.Join(", ", valueAsString)}\" at offset {currentOffset:x8}");
                currentOffset += (uint)valueAsUIntArray.Count;
            }
        }
    }
}