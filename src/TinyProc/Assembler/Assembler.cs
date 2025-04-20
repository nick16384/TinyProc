namespace TinyProc.Assembler;

public partial class Assembler()
{
    public const string ASSEMBLER_VERSION = "2.0";
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
        List<uint> assembledMachineCode = [];
        uint currentLine = 0;
        uint currentAddress = 0x0;
        string[] words = [];

        try
        {
            foreach (string line in assemblyLines)
            {
                currentLine++;
                Console.WriteLine($"Attempting to parse assembly line: {line}");
                words = line.Split([" ", ","], StringSplitOptions.RemoveEmptyEntries);

                // Make sure the correct version of assembly code is used
                if (currentLine == 1)
                {
                    if (words[0] != "#VERSION")
                        throw new ArgumentException("Cannot assemble program. No #VERSION directive specified.");
                    if (words[1] != ASSEMBLER_VERSION)
                        throw new NotSupportedException(
                            $"Assembler version {ASSEMBLER_VERSION} does not match #VERSION directive with value {words[1]}");
                    Console.WriteLine("Version check successful.");
                }

                // Resolve memory spaces, Replace relative addresses with absolute counterpart
                // Example: If "#MEMREGION CON 0x10000000 0x20000000" was specified, then
                // "STORE gp1, CON:3" becomes "STORE gp1, 0x10000003"
                if (words[0] == "#MEMREGION")
                {
                    if (words[1] == "RAM")
                    {
                        uint regionStart = ConvertStringToUInt(words[2]);
                        uint regionEnd = ConvertStringToUInt(words[3]);
                        if (regionEnd < regionStart)
                            throw new ArgumentOutOfRangeException(
                                $"Working memory region end {regionEnd:X8} smaller than start {regionStart:X8}");
                        workingMemoryRegion = (regionStart, regionEnd);
                        Console.WriteLine($"Working memory region: {regionStart:X8} - {regionEnd:X8}");
                        continue;
                    }
                    if (words[1] == "CON")
                    {
                        uint regionStart = ConvertStringToUInt(words[2]);
                        uint regionEnd = ConvertStringToUInt(words[3]);
                        if (regionEnd < regionStart)
                            throw new ArgumentOutOfRangeException(
                                $"Working memory region end {regionEnd:X8} smaller than start {regionStart:X8}");
                        consoleMemoryRegion = (regionStart, regionEnd);
                        Console.WriteLine($"Console memory region: {regionStart:X8} - {regionEnd:X8}");
                        continue;
                    }
                    throw new ArgumentException($"Unknown memory region type {words[1]}");
                }
                if (workingMemoryRegion.Item2 < workingMemoryRegion.Item1)
                    throw new ArgumentOutOfRangeException("Working memory region not specified. Directive #MEMREGION RAM missing");
                if (consoleMemoryRegion.Item2 < consoleMemoryRegion.Item1)
                    throw new ArgumentOutOfRangeException("Console memory region not specified. Directive #MEMREGION CON missing");
                
                // Checks current line for occurrences of relative addresses
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].StartsWith("RAM:"))
                    {
                        uint relAddr = ConvertStringToUInt(words[i].Split(":")[1].Trim());
                        uint absAddr = workingMemoryRegion.Item1 + relAddr;
                        words[i] = Convert.ToString(absAddr);
                    }
                    else if (words[i].StartsWith("CON:"))
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
                    Console.WriteLine($"Found label declaration \"{label}\" at address {currentAddress:X8}");
                }
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i] matches one of labelAddressMap keys)
                    {
                        // do something
                    }
                }

                // Parse line as an instruction
                (uint, uint) instructionBinaryTuple = ParseAsInstruction(words);
                assembledMachineCode.Add(instructionBinaryTuple.Item1);
                assembledMachineCode.Add(instructionBinaryTuple.Item2);
                currentAddress += 0x2;
            }
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"Error on line {currentLine} \"{string.Concat(words)}\":");
            throw;
        }

        return [.. assembledMachineCode];
    }

    // Converts a number string from
    // 1. Base 2 (prefix 0b),
    // 2. Base 10 (no prefix) or
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