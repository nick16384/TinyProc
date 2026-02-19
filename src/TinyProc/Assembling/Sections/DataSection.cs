using static TinyProc.Assembling.Sections.DataSection;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;

namespace TinyProc.Assembling.Sections;

public readonly struct DataSection(uint? fixedLoadAddress,
    bool inlineAll,
    Dictionary<string, ImmediateValue> immediateValues,
    Dictionary<string, Pointer> pointers,
    Dictionary<string, ContinuousBlock> blockPointers)
    : IAssemblySection
{
    public uint Size
    {
        get
        {
            uint immediateValuesTotalSize = 0, pointersTotalSize = 0, blocksTotalSize = 0;
            immediateValuesTotalSize = (uint)immediateValues.Count;
            if (pointers.Count > 0)
                pointersTotalSize = (uint)pointers.Sum(ptr => ptr.Value.Data.Length);
            if (blockPointers.Count > 0)
                blocksTotalSize = blockPointers.Select(blk => blk.Value.Size).Aggregate((x, y) => x + y);
            return immediateValuesTotalSize + pointersTotalSize + blocksTotalSize;
        }
    }
    public uint? FixedLoadAddress { get; } = fixedLoadAddress;
    public bool InlineAll { get; } = inlineAll;
    public List<uint> BinaryRepresentation
    {
        get
        {
            if (InlineAll)
            {
                Logging.LogDebug("Specified inline = all attribute. .data section binary representation stays empty.");
                return [];
            }
            else
                return [
                    .. immediateValues.Select(identifierAndImmediate => identifierAndImmediate.Value.Value),
                    .. pointers.Select(identifierAndPointer => identifierAndPointer.Value.Data).SelectMany(x => x),
                    .. blockPointers.Select(identifierAndBlock => identifierAndBlock.Value.BinaryRepresentation).SelectMany(x => x)
                    ];
        }
    }

    public Dictionary<string, ImmediateValue> ImmediateValues { get => immediateValues; }
    public Dictionary<string, Pointer> Pointers { get => pointers; }
    public Dictionary<string, ContinuousBlock> BlockPointers { get => blockPointers; }

    public readonly struct ImmediateValue(uint offset, uint value)
    {
        public readonly uint Offset = offset;
        public readonly uint Value = value;
    }
    public readonly struct Pointer(uint offset, uint[] data)
    {
        public readonly uint Offset = offset;
        public readonly uint[] Data = data;
    }

    /// <summary>
    /// An Either type instance contains either one instance of T1 or an instance of T2.
    /// The either class wraps them up to be used in contexts where both types may appear.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public class Either<T1, T2>(T1? a, T2? b)
    {
        public T1? A { get; } = a;
        public T2? B { get; } = b;
        public Type Type { get; } = a != null ? typeof(T1) : typeof(T2);

        public bool Is<TCompare>() => typeof(TCompare) == Type;

        public static implicit operator Either<T1, T2>(T1 a) => new(a, default);
        public static implicit operator Either<T1, T2>(T2 b) => new(default, b);

        public static implicit operator T1(Either<T1, T2> either) => either.A;
        public static implicit operator T2(Either<T1, T2> either) => either.B;
    }

    // A block of memory, which is (from external view) continuous in its address space.
    public readonly struct ContinuousBlock(uint offset, Dictionary<uint, Either<ImmediateValue, Pointer>> offsetToDataMap)
    {
        public readonly uint Offset = offset;
        readonly Dictionary<uint, Either<ImmediateValue, Pointer>> _offsetToDataMap = offsetToDataMap;

        public uint Size { get => (uint)BinaryRepresentation.Count; }
        public List<uint> BinaryRepresentation { get; } =
            [.. offsetToDataMap.Values.Select(either => {
                if (either.Is<ImmediateValue>()) { return [((ImmediateValue)either).Value]; }
                else { return ((Pointer)either).Data; }
            }).SelectMany(x => x)];

        public Either<ImmediateValue, Pointer> GetValueAt(uint searchOffset)
        {
            foreach (KeyValuePair<uint, Either<ImmediateValue, Pointer>> offsetAndData in _offsetToDataMap)
            {
                uint offset = offsetAndData.Key;
                Either<ImmediateValue, Pointer> data = offsetAndData.Value;
                uint dataSize = 0;
                if (data.Is<ImmediateValue>())
                    dataSize = 1;
                else
                    dataSize = (uint)((Pointer)data).Data.Length;

                // offset <= searchOffset < (offset + dataSize)
                if (offset <= searchOffset && searchOffset < (offset + dataSize))
                    return data;
            }
            throw new IndexOutOfRangeException($"Continuous block has no data at offset {searchOffset:x8}");
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
        uint? fixedLoadAddress;
        bool inlineAll;
        Dictionary<string, ImmediateValue> immediateValues;
        Dictionary<string, Pointer> pointers;
        Dictionary<string, ContinuousBlock> blockPointers = [];

        (uint?, bool) headerAttributes = ParseHeader(lines[0]);
        fixedLoadAddress = headerAttributes.Item1;
        inlineAll = headerAttributes.Item2;

        lines.RemoveAt(0);
        if (!lines.Any(line => !string.IsNullOrEmpty(line)))
        {
            // If no further lines exist or they're all empty, the .data section is empty.
            Logging.LogDebug(".data section is empty.");
            return DataSection.CreateEmpty();
        }

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
                int blockEndIdx = currentDataSectionCode.IndexOf('}') + 1;
                string innerBlock = currentDataSectionCode[blockStartIdx..blockEndIdx].Trim();

                // Extract block contents for later parsing
                string blockName = words[1].Split("{")[0].Trim();
                Logging.LogDebug($"Found block {blocksFound} with name {blockName}:\n{innerBlock}");
                blocks.Add(blockName, innerBlock.Split("{")[1].Split("}")[0]);

                // Remove block from being processed in immediate and pointer parsing
                int fullBlockStartIdx = blockStartIdx - line.Length - 1;
                int fullBlockLength = blockEndIdx - fullBlockStartIdx;
                currentDataSectionCode = currentDataSectionCode.Remove(fullBlockStartIdx, fullBlockLength).Trim();
                lines = [.. currentDataSectionCode.Split("\n")];
                lines = FilterCommentsAndRemoveExcessWhitespace(lines);
                blocksFound++;
                i -= innerBlock.Split("\n").Length;
            }
        }

        // Process immediate values and pointers
        (Dictionary<string, ImmediateValue>, Dictionary<string, Pointer>) immediateValuesAndPointers
            = ExtractImmediateValuesAndPointers(string.Join("\n", lines));
        immediateValues = immediateValuesAndPointers.Item1;
        pointers = immediateValuesAndPointers.Item2;

        // Log immediate values and pointers (summary)
        Logging.LogDebug(
            $"Successfully parsed {immediateValues.Count} immediate value(s) and " +
            $"{pointers.Count} pointer(s) with a total size of " +
            $"{immediateValues.Count + pointers.Sum(ptr => ptr.Value.Data.Length)} " +
            "words.");

        uint currentOffset = 0;
        if (pointers.Count > 0)
            currentOffset = pointers.Last().Value.Offset + (uint)pointers.Last().Value.Data.Length + 1;
        else if (immediateValues.Count > 0)
            currentOffset = (uint)immediateValues.Count;

        // Actually parse blocks, as immediate values and pointers have now been processed
        foreach (KeyValuePair<string, string> innerBlock in blocks)
        {
            Dictionary<uint, Either<ImmediateValue, Pointer>> blockOffsetToDataMap = [];
            lines = [.. innerBlock.Value.Split("\n")];
            lines = FilterCommentsAndRemoveExcessWhitespace(lines);
            uint offset = 0;
            foreach (string line in lines)
            {
                string[] words = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (words[0] == KEYWORD_IMMEDIATE)
                {
                    string name = words[1];
                    if (name == KEYWORD_SPECIAL_LENGTH)
                    {
                        // Store the length of an immediate value / pointer instead of the value itself.
                        blockOffsetToDataMap.Add(offset, new ImmediateValue(currentOffset + offset, 1));
                    }
                    else
                    {
                        if (!immediateValues.ContainsKey(name))
                            throw new Exception($"Immediate value \"{name}\", specified in block \"{innerBlock.Key}\" was never declared.");
                        blockOffsetToDataMap.Add(offset, immediateValues[name]);
                    }
                    offset += 1;
                }
                else if (words[0] == KEYWORD_POINTER)
                {
                    string name = words[1];
                    if (!pointers.ContainsKey(name))
                        throw new Exception($"Pointer \"{name}\", specified in block \"{innerBlock.Key}\" was never declared.");
                    blockOffsetToDataMap.Add(offset, pointers[name]);
                    offset += (uint)pointers[name].Data.Length;
                }
                else { throw new Exception($"Unknown keyword \"{words[0]}\" in block declaration for \"{innerBlock.Key}\"."); }
            }
            Logging.LogDebug($"Block with size {offset + 1:x8} found at .data section offset {currentOffset}");
            blockPointers.Add(innerBlock.Key, new ContinuousBlock(currentOffset, blockOffsetToDataMap));
            currentOffset += offset;
        }

        // Check for illegal attribute combinations (again)
        if ((inlineAll && pointers.Count > 0) || (inlineAll && blockPointers.Count > 0))
            throw new Exception("Cannot inline pointers / blocks.");
        // The inline attribute does not prevent creation of the .data section object.
        // However, its binary representation will be empty.
        DataSection resultDataSection = new(fixedLoadAddress, inlineAll, immediateValues, pointers, blockPointers);
        Logging.LogDebug($"Successfully parsed .data section into a total of {resultDataSection.Size} word(s).");
        return resultDataSection;
    }

    /// <summary>
    /// Processes the header of the .data section
    /// Mostly similar to the header of the .text section, but not identical,
    /// since some attributes are section specific.
    /// </summary>
    /// <param name="lines"></param>
    /// <returns>A possibly fixed load address and whether to inline all or not</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static (uint?, bool) ParseHeader(string headerLine)
    {
        uint? fixedLoadAddress = null;
        bool inlineAll = false;
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
                    case ASM_ATTRIBUTE_INLINE:
                        if (value != ASM_ATTRIBUTE_INLINE_ALL)
                            throw new ArgumentException($"Cannot apply identifier {ASM_ATTRIBUTE_INLINE} on unknown value {value}");
                        inlineAll = true;
                        break;
                    default:
                        throw new ArgumentException($"Invalid attribute identifier \"{identifier}\"");
                }

                // Identify possibly illegal attribute combinations
                if (fixedLoadAddress.HasValue && inlineAll)
                    Logging.LogWarn("Warning: Specified load address, but .data section is inlined anyways.");
            }
        }
        Logging.LogDebug("Successfully verified and parsed .data section header.");

        return (fixedLoadAddress, inlineAll);
    }

    /// <summary>
    /// Extracts immediate values and pointers from a code section, which only has declarations of them.
    /// </summary>
    /// <param name="immediateAndPointerDeclarations"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static (Dictionary<string, ImmediateValue>, Dictionary<string, Pointer>)
        ExtractImmediateValuesAndPointers(string immediateAndPointerDeclarations)
    {
        List<string> lines = [.. immediateAndPointerDeclarations.Split("\n")];
        Dictionary<string, ImmediateValue> immediateValues = [];
        Dictionary<string, Pointer> pointers = [];

        uint currentOffset = 0;

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
                if (assignment != "=" && assignment != KEYWORD_SPECIAL_LENGTH)
                    throw new ArgumentException($"Assignment operator \"=\" or \"{KEYWORD_SPECIAL_LENGTH}\" expected. " +
                        $"Got \"{assignment}\" instead.");

                if (assignment == "=")
                {
                    immediateValues.Add(name, new ImmediateValue(currentOffset, ConvertStringToUInt(value)));
                    Logging.LogDebug($"Immediate value \"{name}\" has value {ConvertStringToUInt(value):x8} at offset {currentOffset:x8}");
                }
                else if (assignment == KEYWORD_SPECIAL_LENGTH)
                {
                    // Store the size of the variable instead of some fixed value.
                    uint valueSize = 0;
                    if (immediateValues.ContainsKey(value))
                        valueSize = 1;
                    else if (pointers.TryGetValue(value, out Pointer pointer))
                        valueSize = (uint)pointer.Data.Length;
                    else
                        throw new Exception($"Cannot determine length of immediate / pointer \"{value}\". It has not been declared.");
                    immediateValues.Add(name, new ImmediateValue(currentOffset, valueSize));
                }
                
                currentOffset++;
            }

            // Pointers
            // Typical usage:
            // pointer name, "hello, world" + 0xA
            if (words[0] == KEYWORD_POINTER)
            {
                if (words.Length < 3)
                    throw new ArgumentException("Number of literal words in pointer declaration is less than 4.");
                string name = new([.. words[1].SkipLast(1)]);
                string assignment = words[1].Last().ToString();
                // A value can consist of multiple concatenated values
                string[] valueAsString = [.. words[2..]
                .Select(value => value.Split("+", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .SelectMany(a => a)];
                if (assignment != ",")
                    throw new ArgumentException($"Assignment operator \",\" expected. Got \"{assignment}\" instead.");

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
                pointers.Add(name, new Pointer(currentOffset, [.. valueAsUIntArray]));
                Logging.LogDebug($"Pointer \"{name}\" points to data: \"{string.Join(", ", valueAsString)}\" at offset {currentOffset:x8}");
                currentOffset += (uint)valueAsUIntArray.Count;
            }
        }

        return (immediateValues, pointers);
    }

    public static DataSection CreateEmpty()
    {
        return new DataSection(
                fixedLoadAddress: 0x00000000u,
                inlineAll: true,
                [], [], []);
    }
}