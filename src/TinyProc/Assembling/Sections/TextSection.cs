using static TinyProc.Processor.Instructions;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;
using System.Data;
using System.Text.RegularExpressions;

namespace TinyProc.Assembling.Sections;

public readonly struct TextSection : IAssemblySection
{
    public uint Size { get; }
    public uint? FixedLoadAddress { get; }

    public List<IInstruction> Instructions { get; }
    public Dictionary<string, uint> LabelAddressMap { get; }
    public List<uint> BinaryRepresentation { get; }

    public TextSection(uint? fixedLoadAddress,
        List<IInstruction> instructions,
        Dictionary<string, uint> labelAddressMap)
    {
        Size = (uint)instructions.Count * 2;
        FixedLoadAddress = fixedLoadAddress;
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
    internal static TextSection CreateFromAssemblyCode(string assemblyCodeTextSection, DataSection dataSection)
    {
        List<string> lines = [.. assemblyCodeTextSection.Split("\n")];
        lines = FilterCommentsAndRemoveExcessWhitespace(lines);

        uint? fixedLoadAddress;
        List<IInstruction> instructions = [];
        Dictionary<string, uint> labelAddressMap = [];

        uint? headerAttributes = ParseHeader(lines[0]);
        fixedLoadAddress = headerAttributes;
        lines.RemoveAt(0);

        // .text section pre-parser:
        // Replace label encounters with their corresponding address.
        uint currentAddress = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (line.EndsWith(':'))
            {
                string labelName = new([.. line.SkipLast(1)]);
                labelAddressMap.Add(labelName, currentAddress);
                Logging.LogDebug($"Found label declaration \"{labelName}\" at address {currentAddress:x8}");
                lines.RemoveAt(i--);
                continue;
            }
            currentAddress += 2;
        }
        currentAddress = 0;

        foreach (string lineUnparsed in lines)
        {
            Logging.NewlineDebug();
            Logging.LogDebug($"Parsing line: {lineUnparsed}");
            string line = lineUnparsed;
            // Replace occurrences of immediate value / pointer identifiers of the .data section
            // with their corresponding numeric value.

            // Step 1: Replace occurrences of labels / .data section references with their corresponding addresses
            string[] words = SplitLineIntoWords(line);
            // The addressing mode specifies how an address is interpreted by the CPU.
            // There are (currently) two addressing modes:
            // 1. A (Absolute): An absolute address in the entire memory space
            // 2. R (PC-relative): The address is treated as an offset relative to the address of the next instruction (PC)
            // The instruction is relative, if if it references a label, pointer or block pointer.
            // Otherwise, it is implicitly absolute.
            AddressingMode? adrMode = null;
            if (InstructionLookup.IsJumpInstruction(words) || InstructionLookup.IsLoadStoreInstruction(words))
                adrMode = AddressingMode.Absolute;

            foreach (string wordWithClutter in words.SelectMany(word => word.Split(" ")))
            {
                // Remove possible brackets from constant expressions (which always require brackets to be identifiable as such)
                string word = wordWithClutter;
                foreach (string symbolStrip in (IEnumerable<string>)["(", ")", "[", "]"])
                    word = word.Replace(symbolStrip, "");
                if ((dataSection.ImmediateValues.ContainsKey(word) ? 1 : 0) +
                    (dataSection.Pointers.ContainsKey(word) ? 1 : 0) +
                    (dataSection.BlockPointers.ContainsKey(word) ? 1 : 0) +
                    (labelAddressMap.ContainsKey(word) ? 1 : 0) >= 2)
                    // If at least two of the "dictionaries" contain the same word, the reference is ambiguous, since the source is indeterminable.
                    throw new Exception($"Address label / .data section reference is ambiguous for \"{word}\".");
                
                if (labelAddressMap.TryGetValue(word, out uint labelAddress))
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
                    bool isImmediate = dataSection.ImmediateValues.TryGetValue(word, out DataSection.ImmediateValue immediate);
                    bool isPointer = dataSection.Pointers.TryGetValue(word, out DataSection.Pointer pointer);
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
                    bool isPointer = dataSection.Pointers.TryGetValue(word, out DataSection.Pointer pointer);
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
                        uint pointerRelAddress = pointer.Offset - currentAddress - dataSection.Size - 2;
                        line = line.Replace(word, pointerRelAddress.ToString());
                        adrMode = AddressingMode.PCRelative;
                        Logging.LogDebug($".text replace pointer (R) \"{word}\" --> {pointerRelAddress:x8}h / {pointerRelAddress}");
                    }
                    else if (isBlockPointer)
                    {
                        // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                        uint blockPointerRelAddress = blockPointer.Offset - currentAddress - dataSection.Size - 2;
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
    /// Processes the header of the .text section
    /// Mostly similar to the header of the .data section, but not identical,
    /// since some attributes are section specific.
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static uint? ParseHeader(string headerLine)
    {
        uint? fixedLoadAddress = null;
        // Parse section header
        string[] headerWords = SplitLineIntoWords(headerLine);
        if (headerWords[0] != ASM_DIRECTIVE_SECTION || headerWords.Last() != ASM_DIRECTIVE_SECTION_TEXT)
            throw new ArgumentException("Cannot parse assembly .text section: Incorrect header");

        if (headerWords.Length > 2)
        {
            // Header contains attributes
            // Remove first word "#SECTION" and last word ".text"
            headerWords = [.. headerWords.Skip(1).SkipLast(1)];
            // Split by brackets
            string[] attributes =
                [.. string.Join(" ", headerWords)
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
                    case ASM_ATTRIBUTE_SECTION_LOADADDRESS:
                        fixedLoadAddress = ConvertStringToUInt(value);
                        break;
                    default:
                        throw new ArgumentException($"Invalid attribute identifier \"{identifier}\"");
                }

                // Identify possibly illegal attribute combinations
                // Currently none
            }
        }
        Logging.LogDebug("Successfully verified and parsed .text section header.");

        return fixedLoadAddress;
    }
}