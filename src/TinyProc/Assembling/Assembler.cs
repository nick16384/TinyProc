using System.Text.RegularExpressions;
using TinyProc.Application;
using TinyProc.Assembling.Sections;

namespace TinyProc.Assembling;

public partial class Assembler()
{
    /// <summary>
    /// This method is (mostly) used internally. It extracts and parses the .data and .text section
    /// of a program, but returns them directly instead of merging them together with a header.
    /// </summary>
    /// <param name="assemblyCode"></param>
    /// <returns>The entry point, the .data section and the .text section</returns>
    public static (uint, DataSection, TextSection) AssembleToDirectMachineCode(string assemblyCode)
    {
        // FIXME: Edge case: When a comment is enclosed in quotes, it will still be discarded as a comment,
        // which is obviously not intended behaviour.

        // TODO: Work with StringBuilder instead of rewriting strings over and over.
        // use separate indices array to mark newlines, words, etc.
        // If the StringBuilder content needs to be updated (e.g. labels replaced), also update the index arrays.
        List<string> assemblyLines = [.. assemblyCode.Split("\n")];
        assemblyLines = FilterCommentsAndRemoveExcessWhitespace(assemblyLines);
        assemblyCode = string.Join("\n", assemblyLines);

        Logging.LogInfo("===== Assembly begin =====");
        assemblyLines.ForEach(Logging.LogInfo);
        Logging.LogInfo("=====  Assembly end  =====");

        // Maps labels found in the code to their corresponding addresses
        Dictionary<string, uint> labelAddressMap = [];

        try
        {
            // ========== Parse assembly header (version & entry point) ==========
            if (!assemblyLines[0].StartsWith(ASM_DIRECTIVE_VERSION))
                throw new Exception($"Missing {ASM_DIRECTIVE_VERSION} directive at the start. Cannot determine assembly version.");
            if (SplitLineIntoWords(assemblyLines[0]).Length < 2)
                throw new Exception($"Missing version number after {ASM_DIRECTIVE_VERSION} directive.");
            if (!assemblyLines[1].StartsWith(ASM_DIRECTIVE_ENTRYPOINT))
                throw new Exception($"Missing {ASM_DIRECTIVE_ENTRYPOINT} directive after version directive. Cannot determine program entry point.");
            if (SplitLineIntoWords(assemblyLines[1]).Length < 2)
                throw new Exception($"Missing entry point after {ASM_DIRECTIVE_ENTRYPOINT} directive.");

            string versionInAssemblyStr = SplitLineIntoWords(assemblyLines[0])[1];
            if (versionInAssemblyStr != ASSEMBLER_VERSION)
                throw new Exception($"Incompatible assembler version: Expected {ASSEMBLER_VERSION}, got {versionInAssemblyStr} instead.");
            Logging.LogDebug("Assembly version check successful!");

            string entryPointStr = SplitLineIntoWords(assemblyLines[1])[1];
            uint? entryPoint = null;
            try
            {
                entryPoint = ConvertStringToUInt(entryPointStr);
                Logging.LogDebug($"Base entry point: {entryPoint:x8}");
            }
            catch (Exception) { Logging.LogDebug($"Entry point {entryPointStr} seems to reference a label. Resolving later."); }

            // ========== Pre-parser (convert labels and string literals to addresses and values) ==========
            foreach (string line in assemblyLines)
            {
                // Convert string literals to uint sequences
                string[] words = SplitLineIntoWords(line);
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
                            wordUInts.Add(textBlockAsUInt.ToString());
                        }
                        string wordUIntRepresentation = string.Join(" + ", wordUInts);
                        assemblyCode = assemblyCode.Replace(word, wordUIntRepresentation);
                        assemblyLines = [.. assemblyCode.Split("\n")];
                        assemblyLines = FilterCommentsAndRemoveExcessWhitespace(assemblyLines);
                        Logging.LogDebug($"Replaced string literal {word} with uint sequence {wordUIntRepresentation}");
                    }
                }
            }

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
        Logging.LogDebug($"Actual entry point: {entryPoint:x8}");
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

    // Converts a number string from
    // 1. Base 2 (prefix 0b)
    // 2. Base 10 (no prefix)
    // 3. Base 16 (prefix 0x)
    // to a uint
    internal static uint ConvertStringToUInt(string numStr)
    {
        if (numStr.StartsWith("0b"))
        {
            // Base 2
            return Convert.ToUInt32(numStr, 2);
        }
        else if (numStr.StartsWith("0x"))
        {
            // Base 16
            return Convert.ToUInt32(numStr, 16);
        }
        else
        {
            // Base 10 or unknown
            return Convert.ToUInt32(numStr);
        }
    }

    internal static List<string> FilterCommentsAndRemoveExcessWhitespace(List<string> textLines)
        => [.. textLines
            .ConvertAll(line => line.Split(";")[0].Trim())
            .Where(line => !string.IsNullOrEmpty(line))];

    internal static string[] SplitLineIntoWords(string line)
    {
        // Split line at spaces and commas, except when enclosed in double quotes
        // https://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes
        return [.. line.Split('"')
            .Select((element, index) => index % 2 == 0  // If even index
                                ? element.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                : ["\"" + element + "\""])  // Keep the entire item
            .SelectMany(element => element).ToList()];
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
}