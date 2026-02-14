using static TinyProc.Processor.Instructions;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;
using System.Data;

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
    /// Parses the data section of assembly code into a TextSection object.
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
                lines.RemoveAt(i);
                i--;
                continue;
            }
            currentAddress += 2;
        }
        currentAddress = 0;

        foreach (string lineUnparsed in lines)
        {
            Logging.NewlineDebug();
            string line = lineUnparsed;
            // Replace occurrences of immediate value / pointer identifiers of the .data section
            // with their corresponding numeric value.

            // Step 1: Replace occurrences of labels / .data section references with their corresponding addresses
            string[] words = SplitLineIntoWords(line);
            bool isRelativeJump =
                words[0].StartsWith("JMP", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("B", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("CALL", StringComparison.OrdinalIgnoreCase);
            bool isRelativeMemOp =
                words[0].StartsWith("LD", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("LDR", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("STR", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("STRR", StringComparison.OrdinalIgnoreCase);
            foreach (string wordWithClutter in words)
            {
                // Remove possible brackets from constant expressions (which always require brackets to be identifiable as such)
                string word = wordWithClutter.Trim().Replace("(", "").Replace(")", "");
                if ((dataSection.ImmediateValues.ContainsKey(word) ? 1 : 0) +
                    (dataSection.Pointers.ContainsKey(word) ? 1 : 0) +
                    (dataSection.BlockPointers.ContainsKey(word) ? 1 : 0) +
                    (labelAddressMap.ContainsKey(word) ? 1 : 0) >= 2)
                    // If at least two of the "dictionaries" contain the same word, the reference is ambiguous, since the source is indeterminable.
                    throw new Exception($"Address label / .data section reference is ambiguous for \"{word}\".");

                if (labelAddressMap.TryGetValue(word, out uint labelAddress))
                {
                    if (!isRelativeJump)
                        throw new Exception("Cannot jump to absolute address via label, since labels do not represent an absolute address.");
                    uint labelRelAddress = labelAddress - currentAddress;
                    line = line.Replace(word, labelRelAddress.ToString());
                    Logging.LogDebug($".text replace label \"{word}\" --> {labelRelAddress:x8}h / {labelRelAddress}");
                }
                else
                {
                    bool isImmediate = dataSection.ImmediateValues.TryGetValue(word, out DataSection.ImmediateValue immediate);
                    bool isPointer = dataSection.Pointers.TryGetValue(word, out DataSection.Pointer pointer);
                    bool isBlockPointer = dataSection.BlockPointers.TryGetValue(word, out DataSection.ContinuousBlock blockPointer);
                    if (!isRelativeMemOp && (isPointer || isBlockPointer))
                        throw new Exception("Cannot load/store from/to .data section reference, since they are relative and do not possess an absolute address.");
                    if (isImmediate)
                    {
                        uint immediateValue = immediate.Value;
                        line = line.Replace(word, immediateValue.ToString());
                        Logging.LogDebug($".text replace immediate \"{word}\" --> {immediateValue:x8}h / {immediateValue}");
                    }
                    else if (isPointer)
                    {
                        uint pointerRelAddress = pointer.Offset - currentAddress - dataSection.Size;
                        line = line.Replace(word, pointerRelAddress.ToString());
                        Logging.LogDebug($".text replace pointer \"{word}\" --> {pointerRelAddress:x8}h / {pointerRelAddress}");
                    }
                    else if (isBlockPointer)
                    {
                        uint blockPointerRelAddress = blockPointer.Offset - currentAddress - dataSection.Size;
                        line = line.Replace(word, blockPointerRelAddress.ToString());
                        Logging.LogDebug($".text replace block pointer \"{word}\" --> {blockPointerRelAddress:x8}h / {blockPointerRelAddress}");
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

            Logging.LogDebug($"[{currentAddress:x8}{(isRelativeJump ? "-RJ" : "")}{(isRelativeMemOp ? "-RM" : "")}] " +
                $"\"{lineUnparsed}\" -> \"{line}\"");
            words = SplitLineIntoWords(line);

            // Parse assembly line as instruction object
            IInstruction instruction = InstructionLookup.ParseAsInstruction(words);

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