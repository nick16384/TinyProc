using System.Text.RegularExpressions;
using TinyProc.Application;
using TinyProc.Assembling.Sections;
using TinyProc.Processor;

namespace TinyProc.Assembling;

public partial class Assembler
{
    /// <summary>
    /// This method is (mostly) used internally. It extracts and parses the .data and .text section
    /// of a program, but returns them directly instead of merging them together with a header.
    /// </summary>
    /// <param name="assemblyCode"></param>
    /// <returns>The load address, the .data section and the .text section</returns>
    public static (uint, DataSection, TextSection) AssembleToDirectMachineCode(string assemblyCode)
    {
        List<string> assemblyLines = PreParse(assemblyCode);
        try
        {
            // ========== Parse assembly header entry point ==========
            // TODO: Implement #ORG directive
            if (!assemblyLines[1].StartsWith(ASM_DIRECTIVE_ENTRYPOINT))
                throw new Exception($"Missing {ASM_DIRECTIVE_ENTRYPOINT} directive after version directive. Cannot determine program entry point.");
            if (SplitLineIntoWords(assemblyLines[1]).Length < 2)
                throw new Exception($"Missing entry point after {ASM_DIRECTIVE_ENTRYPOINT} directive.");

            string entryPointStr = SplitLineIntoWords(assemblyLines[1])[1];
            uint? entryPoint = null;
            try
            {
                TryConvertStringToUInt(entryPointStr, out entryPoint);
                Logging.LogDebug($"Base entry point: {entryPoint:x8}");
            }
            catch (Exception) { Logging.LogDebug($"Entry point {entryPointStr} seems to reference a label. Resolving later."); }

            // ========== Process .data and .text sections ==========

            // Split full assembly code into sections (.data and .text)
            // Check valid section structure
            string dataSectionWildcard = $"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_DATA}";
            string textSectionWildcard = $"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_TEXT}";
            if (!StringContainsWithWildcard(assemblyCode, dataSectionWildcard))
                throw new Exception("No .data section found.");
            if (!StringContainsWithWildcard(assemblyCode, textSectionWildcard))
                throw new Exception("No .text section found.");
            if (StringIndexOfWithWildcard(assemblyCode, dataSectionWildcard)
                > StringIndexOfWithWildcard(assemblyCode, textSectionWildcard))
                throw new Exception(".data and .text sections in wrong order. Define .data first, then .text");
            if (StringWildcardMatchCount(assemblyCode, dataSectionWildcard) > 2)
                throw new Exception("Multiple .data sections found. Please only specify one.");
            if (StringWildcardMatchCount(assemblyCode, textSectionWildcard) > 2)
                throw new Exception("Multiple .text sections found. Please only specify one.");
            // At this point it is guaranteed, that there is exactly one .data and one .text section,
            // and they are in the following order: .data, .text

            // Determine start and end indices of sections
            int dataSectionStartIdx = StringIndexOfWithWildcard(assemblyCode, dataSectionWildcard);
            int textSectionStartIdx = StringIndexOfWithWildcard(assemblyCode, textSectionWildcard);
            int dataSectionEndIdx = textSectionStartIdx - 1;
            int textSectionEndIdx = assemblyCode.Length - 1;
            string dataSectionCode = new(assemblyCode.AsSpan()[dataSectionStartIdx..(dataSectionEndIdx + 1)].Trim());
            string textSectionCode = new(assemblyCode.AsSpan()[textSectionStartIdx..(textSectionEndIdx + 1)].Trim());

            DataSection dataSection = DataSection.CreateFromAssemblyCode(dataSectionCode);
            TextSection textSection = TextSection.CreateFromAssemblyCode(textSectionCode, dataSection);

            // Replace entry point, if it was a label instead of an address
            if (!entryPoint.HasValue)
            {
                try { entryPoint = textSection.LabelAddressMap[entryPointStr]; }
                catch (Exception) { throw new Exception($"Failed to convert entry point {entryPointStr} into a valid address."); }
                Logging.LogDebug($"Base entry point: {entryPoint:x8}");
            }
            entryPoint += textSection.FixedLoadAddress.GetValueOrDefault(0x0);

            return (entryPoint.Value, dataSection, textSection);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while assembling: ", ex);
        }
    }

    /// <summary>
    /// The assembler reads assembly program code (as a string) and assembles (converts) it into a binary file.
    /// It is crucial to note that this is the only step this class is supposed to do. The following happens
    /// after the program has been assembled by this method:
    /// The resulting binary file can be read by the CPU loader program, which copies the binary data
    /// into the space it finds the most appropriate and start execution of this machine code.
    /// </summary>
    /// <param name="assemblyCode"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static uint[] AssembleToLoadableProgram(string assemblyCode)
    {
        Logging.LogInfo($"HLTP-x25-32 Assembler v{ASSEMBLER_VERSION}");

        // Assembling steps:
        // 1. Parse assembly header
        // 2. Pre-parser (labels, string literals)
        // 3. .data section
        // 4. .text section (assemble)
        // 5. Combine header & sections and return

        (uint, DataSection, TextSection) assembledDataRaw = AssembleToDirectMachineCode(assemblyCode);
        uint entryPoint = assembledDataRaw.Item1;
        DataSection dataSection = assembledDataRaw.Item2;
        TextSection textSection = assembledDataRaw.Item3;

        // ========== Merge .data & .text sections and add header ==========

        // Assembly header metadata words:
        // 1. Assembler version (byte 1: Major; byte 2: Minor, bytes 3 & 4: zero)
        // 2. Entry point (offset in .text section)
        // 3. .data section fixed load address (or 0x0 if relocatable)
        // 4. .data segment size in words
        // 5. .text section fixed load address (ox 0x0 if relocatable)
        // 6. .text segment size in words
        // 7. 0x0
        // 8. 0x0
        Logging.LogDebug(
            "Assembly header:\n" +
            $"Version:.................{ASSEMBLER_VERSION_ENCODED:x8}\n" +
            $"Entry point:.............{entryPoint:x8}\n" +
            $".data load address:......{(dataSection.FixedLoadAddress.HasValue ? dataSection.FixedLoadAddress.Value : "RELOC"):x8}\n" +
            $".data size (dec, words):.{dataSection.Size}\n" +
            $".text load address:......{(textSection.FixedLoadAddress.HasValue ? textSection.FixedLoadAddress.Value : "RELOC"):x8}\n" +
            $".text size (dec, words):.{textSection.Size}\n" +
            $"Padding (1):.............{0:x8}\n" +
            $"Padding (2):.............{0:x8}");
        List<uint> assembledBinary =
        [
            ASSEMBLER_VERSION_ENCODED,
            entryPoint,
            dataSection.FixedLoadAddress.GetValueOrDefault(0x0),
            dataSection.Size,
            textSection.FixedLoadAddress.GetValueOrDefault(0x0),
            textSection.Size,
            0x0,
            0x0
        ];
        assembledBinary.AddRange(dataSection.BinaryRepresentation);
        assembledBinary.AddRange(textSection.BinaryRepresentation);

        return [.. assembledBinary];
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
        try { numValue = ConvertStringToUInt(numStr); }
        catch (Exception) { numValue = null; return false; }
        return true;
    }
    internal static uint ConvertStringToUInt(string numStr)
    {
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

    internal static List<string> FilterCommentsAndRemoveExcessWhitespace(List<string> textLines)
        => [.. textLines
            .ConvertAll(line => line.Split(";")[0].Trim())
            .Where(line => !string.IsNullOrEmpty(line))];

    
    /// <summary>
    /// Receives full assembly code (without comments) and splits the code into
    /// tokens usable by the assembler.
    /// </summary>
    /// <param name="assemblyCode"></param>
    /// <returns>A list of tokens in assembly with every statement ending in an EOL token.</returns>
    internal static Token[] TokenizeAssembly(string assemblyCode)
    {
        string[] assemblyLines = assemblyCode.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        throw new NotImplementedException();
    }
    private const string pattern = @"\[[^\]]*\]|""[^""]*""|[,]|\S+";
    private static readonly string[] prefilterSymbols = [","];
    /// <summary>
    /// Splits a line (expecting zero line breaks) into an array of words (splitting at whitespace and commas), but
    /// words enclosed in quotation marks (") and square brackets ([]) remain as one word.
    /// The function preserves both symbols in the resulting list of strings, but removes commas and spaces.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    internal static string[] SplitLineIntoWords(string line)
    {
        // Split into words
        var matches = Regex.Matches(line, pattern);
        string[] words = [.. matches.Select(m => m.Value.Trim())];
        // Remove separator strings (",") but not if they are enquoted
        foreach (string excludeSymbol in prefilterSymbols)
            words = [.. words.Select(word =>
            {
                // Don't remove characters in enquoted strings
                if (!word.StartsWith('\"') && !word.EndsWith('\"'))
                    return word.Replace(excludeSymbol, "");
                return word;
            })];
        return words;
    }

    /// <summary>
    /// Checks if a string contains a string specified in the wildcard argument.
    /// This method is mostly similar to string.Contains(otherString), except it also
    /// accepts the wildcard symbol * (asterisk), which means any number of arbitrary
    /// characters in between.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="wildcard"></param>
    /// <returns></returns>
    internal static bool StringContainsWithWildcard(string data, string wildcard)
    {
        string pattern = Regex.Escape(wildcard).Replace(@"\#", "#").Replace(@"\*", ".*");
        return Regex.IsMatch(data, pattern);
    }

    internal static int StringIndexOfWithWildcard(string data, string wildcard)
    {
        string pattern = Regex.Escape(wildcard).Replace(@"\#", "#").Replace(@"\*", ".*");
        Match regexMatch = Regex.Match(data, pattern);
        return regexMatch.Success ? regexMatch.Index : throw new Exception($"String {pattern} not found in data.");
    }

    internal static int StringWildcardMatchCount(string data, string wildcard)
    {
        string pattern = Regex.Escape(wildcard).Replace(@"\#", "#").Replace(@"\*", ".*");
        MatchCollection matches = Regex.Matches(data, pattern);
        return matches.Count;
    }

    private enum TokenType
    {
        // Load address directive
        DIRECTIVE_ORG,
        // Entry point directive
        DIRECTIVE_ENTRYPOINT,
        // Define word directive
        DIRECTIVE_DEFINEWORD,
        // Equate directive
        DIRECTIVE_EQUATE,
        // Instruction mnemonic
        MNEMONIC,
        // Instruction register
        REGISTER,
        // Numeric value
        NUMERIC_VALUE,
        // Address label
        LABEL,
        // String value
        STRING,
        // End-of-line (special token)
        EOL
    }
    private class Token(TokenType type, string value)
    {
        public readonly TokenType Type = type;
        public readonly string Value = value;
        public uint AsUInt() => ConvertStringToUInt(Value);
        public Instructions.AddressableRegisterCode AsRegisterCode() => (Instructions.AddressableRegisterCode)Value;
        public byte[] AsByteArray()
        {
            List<byte> bytesList = [];
            foreach (char c in Value)
                bytesList.Add((byte)c);
            return [.. bytesList];
        }
    }
}