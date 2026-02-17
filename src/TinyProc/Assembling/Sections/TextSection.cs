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
            string line = lineUnparsed;
            // Replace occurrences of immediate value / pointer identifiers of the .data section
            // with their corresponding numeric value.

            // Step 1: Replace occurrences of labels / .data section references with their corresponding addresses
            string[] words = SplitLineIntoWords(line);
            bool isJump =
                words[0].StartsWith("JMP", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("B", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("CALL", StringComparison.OrdinalIgnoreCase);
            bool isLoadStore =
                words[0].StartsWith("LD", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("LDR", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("ST", StringComparison.OrdinalIgnoreCase) ||
                words[0].StartsWith("STR", StringComparison.OrdinalIgnoreCase);
            bool isAbsolute = words[0].EndsWith(".A", StringComparison.OrdinalIgnoreCase);

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
                    if (isJump && isAbsolute)
                        throw new Exception("Cannot jump to absolute address via label, since labels do not represent an absolute address.");
                    // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                    uint labelRelAddress = labelAddress - currentAddress - 2;
                    line = line.Replace(word, labelRelAddress.ToString());
                    Logging.LogDebug($".text replace label \"{word}\" --> {labelRelAddress:x8}h / {labelRelAddress}");
                }

                if (!isLoadStore)
                    continue;

                // The instruction is a load / store instruction possibly using relative addressing modes
                if (dataSection.FixedLoadAddress.HasValue && fixedLoadAddress.HasValue)
                {
                    // Location in memory is static for both .data and .text section
                    Logging.LogDebug("Both .data and .text section are static in memory.");
                    bool isImmediate = dataSection.ImmediateValues.TryGetValue(word, out DataSection.ImmediateValue immediate);
                    bool isPointer = dataSection.Pointers.TryGetValue(word, out DataSection.Pointer pointer);
                    bool isBlockPointer = dataSection.BlockPointers.TryGetValue(word, out DataSection.ContinuousBlock blockPointer);
                    if (isImmediate)
                    {
                        uint immediateValue = immediate.Value;
                        line = line.Replace(word, immediateValue.ToString());
                        Logging.LogDebug($".text replace immediate \"{word}\" --> {immediateValue:x8}h / {immediateValue}");
                    }
                    else if (isPointer && isAbsolute)
                    {
                        uint pointerAbsAddress = dataSection.FixedLoadAddress.Value + pointer.Offset;
                        line = line.Replace(word, pointerAbsAddress.ToString());
                        Logging.LogDebug($".text replace pointer (A) \"{word}\" --> {pointerAbsAddress:x8}h / {pointerAbsAddress}");
                    }
                    else if (isPointer && !isAbsolute)
                    {
                        // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                        uint pointerRelAddress = pointer.Offset - currentAddress - dataSection.Size - 2;
                        line = line.Replace(word, pointerRelAddress.ToString());
                        Logging.LogDebug($".text replace pointer (R) \"{word}\" --> {pointerRelAddress:x8}h / {pointerRelAddress}");
                    }
                    else if (isBlockPointer && isAbsolute)
                    {
                        uint blockPointerAbsAddress = dataSection.FixedLoadAddress.Value + blockPointer.Offset;
                        line = line.Replace(word, blockPointerAbsAddress.ToString());
                        Logging.LogDebug($".text replace block pointer (A) \"{word}\" --> {blockPointerAbsAddress:x8}h / {blockPointerAbsAddress}");
                    }
                    else if (isBlockPointer && !isAbsolute)
                    {
                        // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                        uint blockPointerRelAddress = blockPointer.Offset - currentAddress - dataSection.Size - 2;
                        line = line.Replace(word, blockPointerRelAddress.ToString());
                        Logging.LogDebug($".text replace block pointer (R) \"{word}\" --> {blockPointerRelAddress:x8}h / {blockPointerRelAddress}");
                    }
                }
                else
                {
                    // Location in memory is dynamic for either the .data or the .text section.
                    // This means that absolute load / stores won't work.
                    Logging.LogDebug("Either .text or .data section are dynamic.");
                    bool isImmediate = dataSection.ImmediateValues.TryGetValue(word, out DataSection.ImmediateValue immediate);
                    bool isPointer = dataSection.Pointers.TryGetValue(word, out DataSection.Pointer pointer);
                    bool isBlockPointer = dataSection.BlockPointers.TryGetValue(word, out DataSection.ContinuousBlock blockPointer);
                    if (isAbsolute && (isPointer || isBlockPointer))
                        throw new Exception("Cannot infer absolute address of .data section reference, since either or both sections are relocatable.");
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
                        Logging.LogDebug($".text replace pointer (R) \"{word}\" --> {pointerRelAddress:x8}h / {pointerRelAddress}");
                    }
                    else if (isBlockPointer)
                    {
                        // Subtract two to account for PC-relative instructions always using the address from the next instruction.
                        uint blockPointerRelAddress = blockPointer.Offset - currentAddress - dataSection.Size - 2;
                        line = line.Replace(word, blockPointerRelAddress.ToString());
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

            Logging.LogDebug($"[{currentAddress:x8}{(isAbsolute ? ".A" : ".R")}] " +
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