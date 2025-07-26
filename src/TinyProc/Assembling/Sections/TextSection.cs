using static TinyProc.Processor.Instructions;
using static TinyProc.Assembling.Assembler;
using TinyProc.Application;
using System.Data;

namespace TinyProc.Assembling.Sections;

internal readonly struct TextSection : IAssemblySection
{
    public uint Size { get; }
    public bool IsRelocatable { get; }
    public uint? FixedLoadAddress { get; }

    public List<IInstruction> Instructions { get; }
    public List<uint> BinaryRepresentation { get; }

    public TextSection(bool isRelocatable, uint? fixedLoadAddress, List<IInstruction> instructions)
    {
        Size = (uint)instructions.Count * 2;
        IsRelocatable = isRelocatable;
        FixedLoadAddress = fixedLoadAddress;
        Instructions = instructions;

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

        bool isRelocatable;
        uint? fixedLoadAddress;
        List<IInstruction> instructions = [];

        (bool, uint?) headerAttributes = ParseHeader(lines[0]);
        isRelocatable = headerAttributes.Item1;
        fixedLoadAddress = headerAttributes.Item2;
        lines.RemoveAt(0);

        foreach (string lineUnparsed in lines)
        {
            string line = lineUnparsed;
            // Replace occurrences of immediate value / pointer identifiers of the .data section
            // with their corresponding numeric value.
            List<string> constantExpressions = [.. lineUnparsed.Split(["(", ")"], StringSplitOptions.None)];
            // Remove parts not lying between opening and closing brackets (in that order)
            constantExpressions = [.. constantExpressions.Where((str, index) => index % 2 != 0 )];
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
                        expressionPreParsed.Replace(word, dataSection.ImmediateValues[word].ToString());
                    else if (dataSection.Pointers.ContainsKey(word))
                        expressionPreParsed.Replace(word, dataSection.Pointers[word].Offset.ToString());
                    else if (dataSection.BlockPointers.ContainsKey(word))
                        expressionPreParsed.Replace(word, dataSection.BlockPointers[word].Offset.ToString());
                }

                // Evaluate constant expression
                uint expressionValue = Convert.ToUInt32(new DataTable().Compute(expressionPreParsed, null));
                line = line.Replace("(" + expression + ")", expressionValue.ToString());
            }

            string[] words = SplitLineIntoWords(line);

            // Parse assembly line as instruction object
            IInstruction instruction = InstructionLookup.ParseAsInstruction(words);
            
            // Replace occurrences of jump addresses with their fixed offset applied
            if (!isRelocatable && instruction.InstructionType == InstructionType.Jump)
            {
                uint baseAddress = fixedLoadAddress ?? throw new Exception(
                    ".text section relocatable but no load address specified. Cannot determine jump base address.");
                instruction = new JumpInstruction(instruction.Opcode, instruction.Conditional, baseAddress + instruction.J_JumpTargetAddress);
            }
            instructions.Add(instruction);
        }

        return new TextSection(isRelocatable, fixedLoadAddress, instructions);
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
    private static (bool, uint?) ParseHeader(string headerLine)
    {
        bool isRelocatable = true;
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
                    throw new Exception("Text section is not relocatable, but no load address was specified.");
            }
        }
        Logging.LogDebug("Successfully verified and parsed .text section header.");

        return (isRelocatable, fixedLoadAddress);
    }
}