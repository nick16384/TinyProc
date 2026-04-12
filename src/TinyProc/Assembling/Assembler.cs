using System.Text.RegularExpressions;
using TinyProc.Application;
using TinyProc.Assembling.Sections;

namespace TinyProc.Assembling;

// TODO: Find a better name for this class
/// <summary>
/// A meta class containing machine code from an assembled program, both the .data and .text section, and assembly header information.
/// </summary>
public class AssemblerOutput
{
    public readonly struct AssemblyHeader(uint versionEncoded, uint loadAddress, uint entryPoint, uint dataSize, uint textSize)
    {
        public readonly uint VersionEncoded = versionEncoded;
        public readonly uint LoadAddress = loadAddress;
        public readonly uint EntryPoint = entryPoint;
        public readonly uint DataSegmentSize = dataSize;
        public readonly uint TextSegmentSize = textSize;
        // The last 0x0 words are padding reserved for future use:
        public readonly uint[] MachineCodeBinary = [versionEncoded, loadAddress, entryPoint, dataSize, textSize, 0x0, 0x0, 0x0];
    }
    public readonly AssemblyHeader Header;
    public readonly DataSection DataSection;
    public readonly TextSection TextSection;
    public readonly uint[] MachineCodeBinary;
    public AssemblerOutput(uint loadAddress, DataSection dataSection, TextSection textSection)
    {
        DataSection = dataSection;
        TextSection = textSection;
        Header = new(Assembler.ASSEMBLER_VERSION_ENCODED, loadAddress, TextSection.EntryPoint, DataSection.Size, TextSection.Size);
        MachineCodeBinary = [.. Header.MachineCodeBinary, .. DataSection.BinaryRepresentation, .. TextSection.BinaryRepresentation];
    }
}

/// <summary>
/// The assembler reads assembly program code (as a string) and assembles (converts) it into a machine code.
/// It is crucial to note that this is the only step this class is supposed to do.
/// </summary>
public partial class Assembler
{
    /// <summary>
    /// Takes in HLTP32 assembly code (e.g. read from a source file) and converts it into machine code.
    /// The machine code contains a .data section for data and a .text section for executable instructions in sequence.
    /// The .text section follows the .data section.
    /// </summary>
    /// <remarks>
    /// Note: The code of this function is <b>not</b> meant to be performant in any way (preferring code readability),
    /// i.e. it may take some ages to assemble a lot of assembly code.
    /// </remarks>
    /// <param name="assemblyCode"></param>
    /// <returns>A meta class containing machine code, both sections and the load address.</returns>
    /// <exception cref="Exception">If the assembler encounters an error.</exception>
    public static AssemblerOutput Assemble(string assemblyCode)
    {
        // Assembling steps:
        // 1. Cleanup: Remove comments and excess whitespace
        // 2. Check assembly version (first line)
        // 3. Tokenize: Parse code as a list of tokens for the assembler to work with
        // 4. Pre-parse: Expand #DEFINE and "times N"
        // 5. Extract load address (#ORG)
        // 6. .data section parser
        // 7. .text section parser

        assemblyCode = FilterCommentsAndRemoveExcessWhitespace(assemblyCode);
        Logging.LogInfo(
            "\n===== Assembly begin =====\n" +
            assemblyCode +
            "\n=====  Assembly end  =====");
        if (!CheckAssemblyVersion(assemblyCode))
            throw new Exception("Invalid assembly version.");
        Token[] assemblyTokens = TokenizeAssembly(assemblyCode);
        Logging.LogDebug(
            "\n===== Tokenized begin =====");
        assemblyTokens.ToList().ForEach(t => {
            if (t.Type == TokenType.EOS) { Logging.NewlineDebug(); return; }
            Logging.PrintDebug($"{t.Type}({t.Value}) ");
        });
        Logging.PrintDebug(
            "===== Tokenized end =====\n");
        List<Statement> assemblyStatements = TokensToStatements(assemblyTokens);
        assemblyStatements = PreParse(assemblyStatements);

        Logging.LogDebug(
            "\n===== Tokenized begin after pre-parser =====");
        assemblyStatements.ForEach(stmt => Logging.LogDebug($"{stmt}"));
        Logging.PrintDebug(
            "===== Tokenized end after pre-parser =====\n");

        uint loadAddress = FindLoadAddress(assemblyStatements);

        // ========== Process .data and .text sections ==========

        // Search for the .data section:
        int dataSectionStart = assemblyStatements.TakeWhile(statement =>
            statement.STLength < 2 ||
            statement.Tokens[0].Type != TokenType.DIRECTIVE_SECTION ||
            statement.Tokens[^2].Type != TokenType.DIRECTIVE_SECTION_DATA 
        ).Count();
        if (dataSectionStart >= assemblyStatements.Count)
            throw new Exception("No .data section found.");
        // Search for the .text section:
        int textSectionStart = assemblyStatements.TakeWhile(statement =>
            statement.STLength < 2 ||
            statement.Tokens[0].Type != TokenType.DIRECTIVE_SECTION ||
            statement.Tokens[^2].Type != TokenType.DIRECTIVE_SECTION_TEXT 
        ).Count();
        if (textSectionStart >= assemblyStatements.Count)
            throw new Exception("No .text section found.");
        
        int dataSectionEndExclusive = textSectionStart;
        int textSectionEndExclusive = assemblyStatements.Count;
        
        // TODO: Add support for multiple .data and .text sections in arbitrary order

        DataSection dataSection = DataSection.CreateFromAssemblyCode(assemblyStatements[dataSectionStart .. dataSectionEndExclusive]);
        TextSection textSection = TextSection.CreateFromAssemblyCode(assemblyStatements[textSectionStart .. textSectionEndExclusive], dataSection);

        // Assembly header metadata words:
        // 1. Assembler version (byte 1: Major; byte 2: Minor, bytes 3 & 4: zero)
        // 2. Global load address
        // 3. Entry point (offset in .text section)
        // 4. .data segment size in words
        // 5. .text segment size in words
        // 6. 0x0
        // 7. 0x0
        // 8. 0x0
        Logging.LogDebug(
            "Assembly header:\n" +
            $"Version:.................{ASSEMBLER_VERSION_ENCODED:x8}\n" +
            $"Global load address:.....{loadAddress:x8}\n" +
            $"Entry point (rel):.......{textSection.EntryPoint:x8}\n" +
            $".data size (dec, words):.{dataSection.Size:x8}\n" +
            $".text size (dec, words):.{textSection.Size:x8}\n" +
            $"Padding (1):.............{0:x8}\n" +
            $"Padding (2):.............{0:x8}\n" +
            $"Padding (3):.............{0:x8}");
        
        return new AssemblerOutput(loadAddress, dataSection, textSection);
    }

    /// <summary>
    /// Converts a number string from<br></br>
    /// 1. Base 2 (prefix 0b)<br></br>
    /// 2. Base 10 (no prefix)<br></br>
    /// 3. Base 16 (prefix 0x or postfix h)<br></br>
    /// to a uint
    /// </summary>
    /// <param name="numStr">The number string</param>
    /// <param name="numValue">The numeric value. Null if the conversion was unsuccessful.</param>
    /// <returns>Whether the conversion was successful or not.</returns>
    internal static bool TryConvertStringToUInt(string numStr, out uint? numValue)
    {
        // Using exceptions for control flow is not good for performance, but the assembler doesn't rely
        // on performance anyway, so it doesn't matter.
        try { numValue = ConvertStringToUInt(numStr); }
        catch (Exception) { numValue = null; return false; }
        return true;
    }
    internal static uint ConvertStringToUInt(string numStr)
    {
        // For easier reading, assembly may use digit group separators (thousands separators in standard English).
        // However, as Convert.ToUInt32 cannot process these separators, they must be removed beforehand.
        numStr = numStr.Replace("_", "");
        // Base-2 (binary)
        if (numStr.StartsWith("0b"))
            return Convert.ToUInt32(numStr[2..], 2);
        else if (numStr.EndsWith('b'))
            return Convert.ToUInt32(numStr[..^1], 2);
        // Base-16 (hexadecimal)
        else if (numStr.StartsWith("0x"))
            return Convert.ToUInt32(numStr[2..], 16);
        else if (numStr.EndsWith('h'))
            return Convert.ToUInt32(numStr[..^1], 16);
        // Base-10 (decimal) or unknown
        try { return Convert.ToUInt32(numStr); }
        catch (Exception) { throw new Exception($"Unable to parse \"{numStr}\" as uint."); }
    }

    /// <summary>
    /// Removes assembly comments (starting with a semicolon) and excess whitespace
    /// (this includes leading / trailing whitespace in lines and replacing e.g. multi-spaces or tabs with single spaces)
    /// </summary>
    /// <param name="assemblyCode"></param>
    /// <returns></returns>
    private static string FilterCommentsAndRemoveExcessWhitespace(string assemblyCode)
    {
        // FIXME: Edge case: When a comment is enclosed in quotes, it will still be discarded as a comment,
        // which is obviously not intended behaviour.
        string[] lines = assemblyCode.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        lines = [.. lines
            .Select(line => line.Split(";")[0]) // Discard everything after comment symbol
            .Select(line => Regex.Replace(line, @"\s+", " ")) // Replace multi-space or tab with single space
            .Select(line => line.Trim()) // Remove leading or trailing whitespace
            .Where(line => !string.IsNullOrWhiteSpace(line)) // Only keep strings with remaining content
            ];
        return string.Join("\n", lines);
    }

    private static bool CheckAssemblyVersion(string assemblyCode)
    {
        string firstLine = assemblyCode.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        bool isVersionValid = firstLine == $"{ASM_DIRECTIVE_VERSION} {ASSEMBLER_VERSION}";
        if (!isVersionValid)
            Logging.LogError($"Invalid assembly version. Missing {ASM_DIRECTIVE_VERSION} directive or wrong version.");
        return isVersionValid;
    }

    private static uint FindLoadAddress(List<Statement> assemblyStatements)
    {
        foreach (Statement statement in assemblyStatements)
        {
            foreach (Token token in statement.Tokens)
            {
                if (token.Type == TokenType.DIRECTIVE_ORG)
                {
                    if (statement.STLength < 2 || statement.Tokens[1].Type != TokenType.NUMERIC_VALUE)
                        throw new Exception($"Invalid load address specifier {ASM_DIRECTIVE_LOADADDRESS}: {statement}");
                    return ConvertStringToUInt(statement.Tokens[1].Value);
                }
            }
        }
        throw new Exception($"Unable to find load address specifier {ASM_DIRECTIVE_LOADADDRESS}");
    }
}