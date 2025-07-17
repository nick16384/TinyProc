using TinyProc.Application;
using TinyProc.Assembling.Sections;

namespace TinyProc.Assembling;

public partial class Assembler()
{
    public static uint[] AssembleToMachineCode(string assemblyCode)
    {
        Logging.LogInfo($"LLTP-x25-32 Assembler v{ASSEMBLER_VERSION}");

        List<string> assemblyLines = [.. assemblyCode.Split("\n")];
        assemblyLines = FilterCommentsAndRemoveExcessWhitespace(assemblyLines);
        assemblyCode = string.Join("\n", assemblyLines);

        Logging.LogInfo("===== Assembly begin =====");
        assemblyLines.ForEach(Logging.LogInfo);
        Logging.LogInfo("=====  Assembly end  =====");

        // Maps labels found in the code to their corresponding addresses
        Dictionary<string, uint> labelAddressMap = [];

        // 1. Pre-parser (labels, string literals)
        // 2. .data section
        // 3. .text section (assemble)
        // 4. Add header and return

        uint currentLineNumber = 1;
        string currentLineStr = "";

        try
        {
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

            DataSection dataSection = DataSection.CreateFromAssemblyCode(dataSectionCode);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error on line {currentLineNumber} \"{currentLineStr}\": ", ex);
        }
        
        return [];
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