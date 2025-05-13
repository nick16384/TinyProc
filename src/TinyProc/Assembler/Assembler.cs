namespace TinyProc.Assembler;

public partial class Assembler()
{
    private const string ASM_DIRECTIVE_VERSION = "#VERSION";
    public const string ASSEMBLER_VERSION = "2.0";
    public const uint ASSEMBLER_VERSION_ENCODED = (2 << 24) | (0 << 16);
    private const uint ENCODED_VERSION_BITMASK_MAJOR = 0b11111111_00000000_00000000_00000000;
    private const uint ENCODED_VERSION_BITMASK_MINOR = 0b00000000_11111111_00000000_00000000;
    public static string GetVersionStringFromEncodedValue(uint encodedVersion)
    {
        return $"{(encodedVersion & ENCODED_VERSION_BITMASK_MAJOR) >> 24}.{(encodedVersion & ENCODED_VERSION_BITMASK_MINOR) >> 16}";
    }
    public static uint GetEncodedVersionValueFromString(string versionString)
    {
        uint versionMajor = Convert.ToUInt32(versionString.Split(".")[0]);
        uint versionMinor = Convert.ToUInt32(versionString.Split(".")[1]);
        return (versionMajor << 24) | (versionMinor << 16);
    }
    public const int ASSEMBLER_HEADER_SIZE_WORDS = 8;
    public const int HEADER_INDEX_VERSION = 0;
    public const int HEADER_INDEX_RAM_REGION_START = 1;
    public const int HEADER_INDEX_RAM_REGION_END = 2;
    public const int HEADER_INDEX_CON_REGION_START = 3;
    public const int HEADER_INDEX_CON_REGION_END = 4;
    public const int HEADER_INDEX_ENTRY_POINT = 5;

    private const string ASM_DIRECTIVE_MEMREGION = "#MEMREGION";
    private const string ASM_DIRECTIVE_MEMREGION_RAM = "RAM";
    private const string ASM_DIRECTIVE_MEMREGION_CON = "CON";

    private const uint DEFAULT_PROGRAM_ENTRY_POINT = 0x0u;

    public static uint[] AssembleToMachineCode(string assemblyCode)
    {
        Console.WriteLine($"LLTP-x25-32 Assembler v{ASSEMBLER_VERSION}");
        List<string> assemblyLines = [.. assemblyCode.Split("\n")];
        // Filter out comments & Remove empty lines
        assemblyLines = [.. assemblyLines
            .ConvertAll(line => line.Split(";")[0].Trim())
            .Where(line => !string.IsNullOrEmpty(line))];

        Console.WriteLine("===== Assembly begin =====");
        assemblyLines.ForEach(Console.WriteLine);
        Console.WriteLine("=====  Assembly end  =====");

        (uint, uint) workingMemoryRegion = (uint.MaxValue, uint.MaxValue - 1);
        (uint, uint) consoleMemoryRegion = (uint.MaxValue, uint.MaxValue - 1);
        Dictionary<string, uint> labelAddressMap = [];
        List<uint> assembledMachineCode =
            [ASSEMBLER_VERSION_ENCODED, 0x0, 0x0, 0xFFFFFFFF, 0xFFFFFFFE, DEFAULT_PROGRAM_ENTRY_POINT, 0x0u, 0x0u];
        uint currentLine = 0;
        uint currentAddress = 0x0;
        string currentLineStr = "";

        try
        {
            foreach (string line in assemblyLines)
            {
                currentLine++;
                currentLineStr = line;
                Console.WriteLine($"Attempting to parse assembly line: {line}");
                // Split line at spaces and commas, except when enclosed in double quotes
                // https://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes
                string[] words = [.. line.Split('"')
                     .Select((element, index) => index % 2 == 0  // If even index
                                           ? element.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                           : ["\"" + element + "\""])  // Keep the entire item
                     .SelectMany(element => element).ToList()];

                // Make sure the correct version of assembly code is used
                if (currentLine == 1)
                {
                    if (words[0] != ASM_DIRECTIVE_VERSION)
                        throw new ArgumentException($"Cannot assemble program. No {ASM_DIRECTIVE_VERSION} directive specified.");
                    if (words[1] != ASSEMBLER_VERSION)
                        throw new NotSupportedException(
                            $"Assembler version {ASSEMBLER_VERSION}" +
                            $"does not match {ASM_DIRECTIVE_VERSION} directive with value {words[1]}");
                    Console.WriteLine("Version check successful.");
                    continue;
                }

                // Resolve memory spaces, Replace relative addresses with absolute counterpart
                // Example: If "#MEMREGION CON 0x10000000 0x20000000" was specified, then
                // "STORE gp1, CON:3" becomes "STORE gp1, 0x10000003"
                if (words[0] == ASM_DIRECTIVE_MEMREGION)
                {
                    if (words[1] == ASM_DIRECTIVE_MEMREGION_RAM)
                    {
                        uint regionStart = ConvertStringToUInt(words[2]);
                        uint regionEnd = ConvertStringToUInt(words[3]);
                        if (regionEnd < regionStart)
                            throw new ArgumentOutOfRangeException(
                                $"Working memory region end {regionEnd:x8} smaller than start {regionStart:x8}");
                        workingMemoryRegion = (regionStart, regionEnd);
                        assembledMachineCode[HEADER_INDEX_RAM_REGION_START] = regionStart;
                        assembledMachineCode[HEADER_INDEX_RAM_REGION_END] = regionEnd;
                        Console.WriteLine($"Working memory region: {regionStart:x8} - {regionEnd:x8}");
                        continue;
                    }
                    if (words[1] == ASM_DIRECTIVE_MEMREGION_CON)
                    {
                        uint regionStart = ConvertStringToUInt(words[2]);
                        uint regionEnd = ConvertStringToUInt(words[3]);
                        if (regionEnd < regionStart)
                            throw new ArgumentOutOfRangeException(
                                $"Working memory region end {regionEnd:x8} smaller than start {regionStart:x8}");
                        consoleMemoryRegion = (regionStart, regionEnd);
                        assembledMachineCode[HEADER_INDEX_CON_REGION_START] = regionStart;
                        assembledMachineCode[HEADER_INDEX_CON_REGION_END] = regionEnd;
                        Console.WriteLine($"Console memory region: {regionStart:x8} - {regionEnd:x8}");
                        continue;
                    }
                    throw new ArgumentException($"Unknown memory region type {words[1]}");
                }
                
                // Checks current line for occurrences of relative addresses
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].StartsWith(ASM_DIRECTIVE_MEMREGION_RAM + ":"))
                    {
                        uint relAddr = ConvertStringToUInt(words[i].Split(":")[1].Trim());
                        uint absAddr = workingMemoryRegion.Item1 + relAddr;
                        words[i] = Convert.ToString(absAddr);
                    }
                    else if (words[i].StartsWith(ASM_DIRECTIVE_MEMREGION_CON + ":"))
                    {
                        uint relAddr = ConvertStringToUInt(words[i].Split(":")[1].Trim());
                        uint absAddr = consoleMemoryRegion.Item1 + relAddr;
                        words[i] = Convert.ToString(absAddr);
                    }
                }

                // Resolve labels, Replace label encounters with their corresponding absolute addresses
                if (words[0].EndsWith(':'))
                {
                    string label = words[0][..^1];
                    labelAddressMap.Add(label, currentAddress);
                    Console.WriteLine($"Found label declaration \"{label}\" at address {currentAddress:x8}");
                    continue;
                }
                for (int i = 0; i < words.Length; i++)
                {
                    if (labelAddressMap.ContainsKey(words[i]))
                    {
                        uint labelAddress = labelAddressMap[words[i]];
                        words[i] = Convert.ToString(labelAddress);
                    }
                }

                // Replace string literals with corresponding ASCII numeric values
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].StartsWith('\"'))
                    {
                        if (!words[i].EndsWith('\"'))
                            throw new ArgumentException($"Unescaped string literal: {words[i]}");
                        
                        // Trim quotes of string literal
                        string stringLiteral = words[i][1..^1];
                        char[] stringLiteralChars = stringLiteral.ToCharArray();
                        int charCount = stringLiteralChars.Length;

                        uint stringLiteralAsUInt = 0x0;
                        for (int cIdx = 0; cIdx < charCount; cIdx++)
                        {
                            char c = stringLiteralChars[cIdx];
                            stringLiteralAsUInt |= ((uint)c) << ((3 - cIdx) * sizeof(byte)*8);
                        }

                        words[i] = Convert.ToString(stringLiteralAsUInt);
                    }
                }
                Console.WriteLine($"Line parsed into tokens: \"{currentLineStr}\" --> {string.Join(" ", words)}");

                // Parse line as an instruction
                (uint, uint) instructionBinaryTuple = ParseAsInstruction(words);
                assembledMachineCode.Add(instructionBinaryTuple.Item1);
                assembledMachineCode.Add(instructionBinaryTuple.Item2);
                currentAddress += 0x2;
            }
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"Error on line {currentLine} \"{currentLineStr}\":");
            throw;
        }

        // Check valid memory regions
        uint RAMRegionStart = workingMemoryRegion.Item1;
        uint RAMRegionEnd   = workingMemoryRegion.Item2;
        uint CONRegionStart = consoleMemoryRegion.Item1;
        uint CONRegionEnd   = consoleMemoryRegion.Item2;
        if (RAMRegionEnd < RAMRegionStart)
            throw new ArgumentOutOfRangeException("Working memory region not specified. Directive #MEMREGION RAM missing");
        if (CONRegionEnd < CONRegionStart)
            // Console region is optional, therefore no error is thrown if it doesn't exist.
            Console.Error.WriteLine("Warning: No console memory region specified. (#MEMREGION CON directive missing)");
        
        // Check for overlapping regions
        // Check if CON region starts inside of RAM region
        if (RAMRegionStart <= CONRegionStart && CONRegionStart <= RAMRegionEnd)
            throw new ArgumentOutOfRangeException("Memory region overlap: CON region starts inside of RAM region.");
        // Check if CON region ends inside of RAM region
        if (RAMRegionStart <= CONRegionEnd && CONRegionEnd <= RAMRegionEnd)
            throw new ArgumentOutOfRangeException("Memory region overlap: CON region ends inside of RAM region.");
        // Check if RAM region starts inside of CON region
        if (CONRegionStart <= RAMRegionStart && RAMRegionStart <= CONRegionEnd)
            throw new ArgumentOutOfRangeException("Memory region overlap: RAM region starts inside of CON region.");
        // Check if RAM region ends inside of CON region
        if (CONRegionStart <= RAMRegionEnd && RAMRegionEnd <= CONRegionEnd)
            throw new ArgumentOutOfRangeException("Memory region overlap: RAM region ends inside of CON region.");
        
        // Check for unused void space between memory regions
        uint RAMRegionSize = RAMRegionEnd - RAMRegionStart + 1; // + 1, because both start & end addresses are inclusive
        uint CONRegionSize = CONRegionEnd - CONRegionStart + 1;
        if (RAMRegionSize + CONRegionSize < uint.Max(RAMRegionEnd + 1, CONRegionEnd + 1))
            throw new ArgumentOutOfRangeException(
                "Unused void space between memory regions." +
                $"Expected {RAMRegionSize + CONRegionSize}, got {uint.Max(RAMRegionEnd + 1, CONRegionEnd + 1)}");

        return [.. assembledMachineCode];
    }

    // Converts a number string from
    // 1. Base 2 (prefix 0b)
    // 2. Base 10 (no prefix)
    // 3. Base 16 (prefix 0x)
    // to a uint
    private static uint ConvertStringToUInt(string numStr)
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
}