using TinyProc.Application;
using TinyProc.Assembling.Sections;

namespace TinyProc.Assembling;

public partial class Assembler()
{
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
    public static uint[] AssembleToMachineCode(string assemblyCode)
    {
        Logging.LogInfo($"HLTP-x25-32 Assembler v{ASSEMBLER_VERSION}");

        // Assembling steps:
        // 1. Parse assembly header
        // 2. Pre-parser (labels, string literals)
        // 3. .data section
        // 4. .text section (assemble)
        // 5. Combine header & sections and return

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
            uint entryPoint = 0;
            try { entryPoint = ConvertStringToUInt(entryPointStr); }
            catch (Exception) { throw new Exception($"Entry point {entryPointStr} seems to reference a label. Resolving later."); }
            Logging.LogDebug($"Base entry point: {entryPoint:x8}");

            // ========== Pre-parser (convert labels and string literals to addresses and values) ==========
            foreach (string line in assemblyLines)
            {
                // Convert string literals to uint sequences

                // TODO: Parse labels inside .text section, then extract possible entry point from that
            }
            try { entryPoint = ConvertStringToUInt(entryPointStr); }
            catch (Exception) { throw new Exception($"Failed to convert entry point {entryPointStr} into a valid address."); }
            Logging.LogDebug($"Base entry point: {entryPoint:x8}");

            // ========== Process .data and .text sections ==========

            // Split full assembly code into sections (.data and .text)
            // Check valid section structure
            if (!assemblyCode.Contains($"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_DATA}"))
                throw new Exception("No .data section found.");
            if (!assemblyCode.Contains($"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_TEXT}"))
                throw new Exception("No .text section found.");
            if (assemblyCode.IndexOf($"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_DATA}")
                > assemblyCode.IndexOf($"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_TEXT}"))
                throw new Exception(".data and .text sections in wrong order. Define .data first, then .text");
            if (assemblyCode.Split($"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_DATA}").Length > 2)
                throw new Exception("Multiple .data sections found. Please only specify one.");
            if (assemblyCode.Split($"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_TEXT}").Length > 2)
                throw new Exception("Multiple .text sections found. Please only specify one.");
            // At this point it is guaranteed, that there is exactly one .data and one .text section,
            // and they are in the following order: .data, .text

            // Determine start and end indices of sections
            int dataSectionStartIdx = assemblyCode.IndexOf($"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_DATA}");
            int textSectionStartIdx = assemblyCode.IndexOf($"{ASM_DIRECTIVE_SECTION}*{ASM_DIRECTIVE_SECTION_TEXT}");
            // Get start of .text section and subtract the header length of the .text section
            int dataSectionEndIdx = textSectionStartIdx - assemblyCode[textSectionStartIdx..].Split("\n")[0].Length;
            int textSectionEndIdx = assemblyCode.Length - 1;
            string dataSectionCode = new(assemblyCode.AsSpan()[dataSectionStartIdx..dataSectionEndIdx]);
            string textSectionCode = new(assemblyCode.AsSpan()[textSectionStartIdx..textSectionEndIdx]);

            // TODO: Parse assembly header here (#VERSION, #ENTRY)
            Logging.LogError("Implement assembly header parsing!");

            DataSection dataSection = DataSection.CreateFromAssemblyCode(dataSectionCode);
            TextSection textSection = TextSection.CreateFromAssemblyCode(textSectionCode, dataSection);

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
            Logging.LogDebug($"Actual entry point: {entryPoint + textSection.FixedLoadAddress.GetValueOrDefault(0x0):x8}");
            List<uint> assembledBinary =
            [
                ASSEMBLER_VERSION_ENCODED,
                entryPoint + textSection.FixedLoadAddress.GetValueOrDefault(0x0),
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
        catch (Exception ex)
        {
            throw new Exception($"Error while assembling: ", ex);
        }
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
}