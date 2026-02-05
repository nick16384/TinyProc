using static TinyProc.Processor.Instructions;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;
using System.Data;
using System.Reflection.Emit;

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

        foreach (string lineUnparsed in lines)
        {
            string line = lineUnparsed;
            // Replace occurrences of immediate value / pointer identifiers of the .data section
            // with their corresponding numeric value.
            List<string> constantExpressions = [.. lineUnparsed.Split(["(", ")"], StringSplitOptions.None)];
            // Remove parts not lying between opening and closing brackets (in that order)
            constantExpressions = [.. constantExpressions.Where((str, index) => index % 2 != 0)];
            foreach (string expression in constantExpressions)
            {
                string expressionPreParsed = expression;
                List<string> expressionWords = [.. SplitLineIntoWords(expression)];

                // Replace immediate values / pointers from the .data sections with their value.
                foreach (string word in expressionWords)
                {
                    bool canParseWordAsUInt = true;
                    try { ConvertStringToUInt(word); } catch (Exception) { canParseWordAsUInt = false; }
                    if (canParseWordAsUInt || new List<string> { "+", "-", "*", "/" }.Contains(word))
                        continue;

                    if (dataSection.ImmediateValues.ContainsKey(word))
                        expressionPreParsed = expressionPreParsed.Replace(word, dataSection.ImmediateValues[word].Value.ToString());
                    else if (dataSection.Pointers.ContainsKey(word))
                        expressionPreParsed = expressionPreParsed.Replace(word, dataSection.Pointers[word].Offset.ToString());
                    else if (dataSection.BlockPointers.ContainsKey(word))
                        expressionPreParsed = expressionPreParsed.Replace(word, dataSection.BlockPointers[word].Offset.ToString());
                }

                // Evaluate constant expression
                uint expressionValue = Convert.ToUInt32(new DataTable().Compute(expressionPreParsed, null));
                line = line.Replace("(" + expression + ")", expressionValue.ToString());
            }
            // Evaluate constant expressions consisting of a single immediate / pointer
            string[] words = SplitLineIntoWords(line);
            foreach (string word in words)
            {
                if (dataSection.ImmediateValues.ContainsKey(word))
                    line = line.Replace(word, dataSection.ImmediateValues[word].Value.ToString());
                else if (dataSection.Pointers.ContainsKey(word))
                    line = line.Replace(word, dataSection.Pointers[word].Offset.ToString());
                else if (dataSection.BlockPointers.ContainsKey(word))
                    line = line.Replace(word, dataSection.BlockPointers[word].Offset.ToString());
            }
            // Replace occurrences of labels with their corresponding addresses
            foreach (string word in words)
            {
                foreach ((string label, uint address) in labelAddressMap)
                {
                    if (word == label)
                        line = line.Replace(word, address.ToString());
                }
            }

            Logging.LogDebug($"\"{lineUnparsed}\" -> \"{line}\"");
            words = SplitLineIntoWords(line);

            // Parse assembly line as instruction object
            IInstruction instruction = InstructionLookup.ParseAsInstruction(words);

            // Note: Since the introduction of relative / absolute jumps, it is no longer necessary
            // to adjust any instructions pointing to memory, since they're either absolute in memory
            // or relative to PC-2.
            
            instructions.Add(instruction);
        }

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
                // None
            }
        }
        Logging.LogDebug("Successfully verified and parsed .text section header.");

        return fixedLoadAddress;
    }
}