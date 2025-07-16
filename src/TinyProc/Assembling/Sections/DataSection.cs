using static TinyProc.Assembling.Sections.DataSection;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;
using System.Net.NetworkInformation;
using System.Net.Http.Headers;
using System.Collections.Frozen;
using System.Diagnostics;

namespace TinyProc.Assembling.Sections;

internal class DataSection(bool isRelocatable, uint? fixedLoadAddress,
    Dictionary<string, uint> immediateValues, Dictionary<string, uint[]> pointers, Dictionary<string, ContinuousBlock> blockPointers)
{
    public uint Size { get; } = (uint)immediateValues.Count + (uint)pointers.Count
                                + blockPointers.Values.Select((block) => block.Size).Aggregate((x, y) => x + y);
    public bool IsRelocatable { get; } = isRelocatable;
    public uint? FixedLoadAddress { get; } = fixedLoadAddress;

    Dictionary<string, uint> ImmediateValues { get; } = immediateValues;
    Dictionary<string, uint[]> Pointers { get; } = pointers;
    Dictionary<string, ContinuousBlock> BlockPointers { get; } = blockPointers;

    internal enum DataType
    {
        Immediate,
        Pointer
    }

    // A block of memory, which is (from external view) continuous in its address space.
    internal readonly struct ContinuousBlock (Dictionary<uint, (DataType, uint[])> offsetToDataMap)
    {
        readonly Dictionary<uint, (DataType, uint[])> _offsetToDataMap = offsetToDataMap;

        public uint Size { get => (uint)_offsetToDataMap.Values.Select(tuple => tuple.Item2.Length).Aggregate((x, y) => x + y); }

        public DataType GetTypeAt(uint dataOffset)
        {
            foreach (KeyValuePair<uint, (DataType, uint[])> offsetAndTypeAndData in _offsetToDataMap)
            {
                uint offset = offsetAndTypeAndData.Key;
                DataType type = offsetAndTypeAndData.Value.Item1;
                uint[] data = offsetAndTypeAndData.Value.Item2;

                // address <= dataIndex < (address + size)
                if (offset <= dataOffset && dataOffset < (offset + data.Length))
                    return type;
            }
            throw new IndexOutOfRangeException($"Continuous block has no data at offset {dataOffset:x8}");
        }

        public object GetValueAt(uint dataOffset)
        {
            foreach (KeyValuePair<uint, (DataType, uint[])> offsetAndTypeAndData in _offsetToDataMap)
            {
                uint offset = offsetAndTypeAndData.Key;
                DataType type = offsetAndTypeAndData.Value.Item1;
                uint[] data = offsetAndTypeAndData.Value.Item2;

                // address <= dataIndex < (address + size)
                if (offset <= dataOffset && dataOffset < (offset + data.Length))
                {
                    return type switch
                    {
                        DataType.Immediate => data[0],
                        DataType.Pointer => data,
                        _ => throw new Exception($"Invalid DataType {type}")
                    };
                }
            }
            throw new IndexOutOfRangeException($"Continuous block has no data at offset {dataOffset:x8}");
        }
    }

    /// <summary>
    /// Parses the data section of assembly code into a DataSection object.
    /// </summary>
    /// <param name="assemblyCodeDataSection"></param>
    /// <returns></returns>
    internal static DataSection CreateFromAssemblyCode(string assemblyCodeDataSection)
    {
        List<string> lines = [.. assemblyCodeDataSection.Split("\n")];
        lines = FilterCommentsAndRemoveExcessWhitespace(lines);

        // These variables need to be determined by this parser
        bool isRelocatable;
        uint? fixedLoadAddress;
        Dictionary<string, uint> immediateValues;
        Dictionary<string, uint[]> pointers;
        Dictionary<string, ContinuousBlock> blockPointers = [];

        (bool, uint?) headerAttributes = ParseHeader(lines);
        isRelocatable = headerAttributes.Item1;
        fixedLoadAddress = headerAttributes.Item2;

        // Main parser for .data section
        // Search for blocks, save their contents for after immediate values and pointers have been parsed.
        int blocksFound = 0;
        Dictionary<string, string> blocks = [];
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            string[] words = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            // Blocks (Guaranteed continuous regions of memory)
            // Typical usage:
            // immediate x = 20
            // pointer stuff, "Stuff"
            // block name
            // {
            //    immediate x
            //    pointer stuff
            // }
            if (words[0] == KEYWORD_BLOCK)
            {
                // Determine block start / end
                string currentDataSectionCode = string.Join("\n", lines);
                int blockStartIdx = currentDataSectionCode.IndexOf('{');
                int blockEndIdx = currentDataSectionCode.IndexOf('}');
                string innerBlock = currentDataSectionCode[blockStartIdx..blockEndIdx];

                // Extract block contents for later parsing
                string blockName = words[1].Split("{")[0].Trim();
                Logging.LogDebug($"Found block {blocksFound} with name {blockName}:\n{innerBlock}");
                blocks.Add(blockName, innerBlock.Split("{")[1].Split("}")[0]);

                // Remove block from being processed in immediate and pointer parsing
                currentDataSectionCode = currentDataSectionCode[..(blockStartIdx + 1)][blockEndIdx..];
                lines = [.. currentDataSectionCode.Split("\n")];
                lines = FilterCommentsAndRemoveExcessWhitespace(lines);
                blocksFound++;
            }
        }

        // Process immediate values and pointers
        (Dictionary<string, uint>, Dictionary<string, uint[]>) immediateValuesAndPointers
            = ExtractImmediateValuesAndPointers(string.Join("\n", lines));
        immediateValues = immediateValuesAndPointers.Item1;
        pointers = immediateValuesAndPointers.Item2;

        // Actually parse blocks, as immediate values and pointers have now been processed
        foreach (KeyValuePair<string, string> innerBlock in blocks)
        {
            Dictionary<uint, (DataType, uint[])> blockOffsetToDataMap = [];
            lines = [.. innerBlock.Value.Split("\n")];
            lines = FilterCommentsAndRemoveExcessWhitespace(lines);
            uint offset = 0;
            foreach (string line in lines)
            {
                string[] words = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (words[0] == KEYWORD_IMMEDIATE)
                {
                    string name = words[1];
                    if (!immediateValues.ContainsKey(name))
                        throw new Exception($"Immediate value \"{name}\", specified in block \"{innerBlock.Key}\" was never declared.");
                    blockOffsetToDataMap.Add(offset, (DataType.Immediate, [immediateValues[name]]));
                    offset += 1;
                }
                else if (words[0] == KEYWORD_POINTER)
                {
                    string name = words[1];
                    if (!pointers.ContainsKey(name))
                        throw new Exception($"Pointer \"{name}\", specified in block \"{innerBlock.Key}\" was never declared.");
                    blockOffsetToDataMap.Add(offset, (DataType.Pointer, pointers[name]));
                    offset += (uint)pointers[name].Length;
                }
                else { throw new Exception($"Unknown keyword \"{words[0]}\" in block declaration for \"{innerBlock.Key}\"."); }
            }
            blockPointers.Add(innerBlock.Key, new ContinuousBlock(blockOffsetToDataMap));
        }

        return new DataSection(isRelocatable, fixedLoadAddress, immediateValues, pointers, blockPointers);
    }

    /// <summary>
    /// Processes the header of the .data section
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static (bool, uint?) ParseHeader(List<string> lines)
    {
        bool isRelocatable = true;
        uint? fixedLoadAddress = null;
        // Parse section header
        string[] headerWords = SplitLineIntoWords(lines[0]);
        if (headerWords[0] != ASM_DIRECTIVE_SECTION || headerWords.Last() != ASM_DIRECTIVE_SECTION_DATA)
            throw new ArgumentException("Cannot parse assembly .data section: Incorrect header");

        if (headerWords.Length > 2)
        {
            // Header contains attributes
            // Remove first word "#SECTION" and last word ".data"
            headerWords = [.. headerWords.Skip(1).SkipLast(1)];
            // Split by brackets
            string[] attributes =
                [.. string.Join("", headerWords)
                .Split(["(", ")"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

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
                    case ASM_ATTRIBUTE_SECTION_RELOCATABLE:
                        isRelocatable = bool.Parse(value);
                        break;
                    case ASM_ATTRIBUTE_SECTION_LOADADDRESS:
                        fixedLoadAddress = ConvertStringToUInt(value);
                        break;
                    default:
                        throw new ArgumentException($"Invalid attribute identifier \"{identifier}\"");
                }

                // Identify possibly illegal attribute combinations
                if (!isRelocatable && !fixedLoadAddress.HasValue)
                    throw new Exception("Data section is not relocatable, but no load address was specified.");
            }
        }
        Logging.LogDebug("Successfully verified and parsed .data section header.");

        return (isRelocatable, fixedLoadAddress);
    }

    /// <summary>
    /// Extracts immediate values and pointers from a code section, which only has declarations of them.
    /// </summary>
    /// <param name="immediateAndPointerDeclarations"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static (Dictionary<string, uint>, Dictionary<string, uint[]>) ExtractImmediateValuesAndPointers(string immediateAndPointerDeclarations)
    {
        List<string> lines = [.. immediateAndPointerDeclarations.Split("\n")];
        Dictionary<string, uint> immediateValues = [];
        Dictionary<string, uint[]> pointers = [];

        foreach (string line in lines)
        {
            string[] words = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            // Immediate values
            // Typical usage:
            // immediate x = 10
            if (words[0] == KEYWORD_IMMEDIATE)
            {
                if (words.Length != 4)
                    throw new ArgumentException("Number of words in immediate declaration does not equal 4.");
                string name = words[1];
                string assignment = words[2];
                string value = words[3];
                if (assignment != "=")
                    throw new ArgumentException($"Assignment operator \"=\" expected. Got \"{assignment}\" instead.");

                immediateValues.Add(name, ConvertStringToUInt(value));
                Logging.LogDebug($"Immediate value \"{name}\" has value {ConvertStringToUInt(value):x8}");
            }

            // Pointers
            // Typical usage:
            // pointer name, "hello, world" + 0xA
            if (words[0] == KEYWORD_POINTER)
            {
                if (words.Length < 3)
                    throw new ArgumentException("Number of words in pointer declaration is less than 4.");
                string name = new([.. words[1].SkipLast(1)]);
                string assignment = words[1].Last().ToString();
                // A value can consist of multiple concatenated values
                string[] valueAsString = [.. words[3..]
                .Select(value => value.Split("+", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .SelectMany(a => a)];
                if (assignment != ",")
                    throw new ArgumentException($"Assignment operator \",\" expected. Got \"{assignment}\" instead.");

                List<uint> valueAsUIntArray = [];
                foreach (string value in valueAsString)
                {
                    if (uint.TryParse(value, out uint n))
                        valueAsUIntArray.Add(n);
                    else
                        throw new Exception($"Unable to parse pointer value \"{value}\" as uint array.");
                }
                pointers.Add(name, [.. valueAsUIntArray]);
                Logging.LogDebug($"Pointer \"{name}\" points to the following data: {string.Join(", ", valueAsString)}");
            }
        }

        return (immediateValues, pointers);
    }
}