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
            string dataSectionCode = "";
            string textSectionCode = "";
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