namespace TinyProc.Application;

using TinyProc.Assembler;

// Wrapper for some binary data resembling HLTP32 code.
// Can extract header information, read a binary file and save newly written files.
public class ExecutableWrapper
{
    public uint[] Program;
    public uint[] ExecutableProgram { get => [.. Program.Skip(Assembler.ASSEMBLER_HEADER_SIZE_WORDS)]; }

    public uint AssemblerVersion { get => Program[Assembler.HEADER_INDEX_VERSION]; }
    public uint RAMRegionStart   { get => Program[Assembler.HEADER_INDEX_RAM_REGION_START]; }
    public uint RAMRegionEnd     { get => Program[Assembler.HEADER_INDEX_RAM_REGION_END]; }
    public uint CONRegionStart   { get => Program[Assembler.HEADER_INDEX_CON_REGION_START]; }
    public uint CONRegionEnd     { get => Program[Assembler.HEADER_INDEX_CON_REGION_END]; }
    public uint EntryPoint       { get => Program[Assembler.HEADER_INDEX_ENTRY_POINT]; }

    public ExecutableWrapper(string executableFilePath) => LoadProgramFromFile(executableFilePath);
    public void LoadProgramFromFile(string executableFilePath)
    {
        Console.WriteLine($"Attempting to load binary executable {executableFilePath}");
        if (executableFilePath.Trim().EndsWith(".lltp32.bin"))
            Console.Error.WriteLine("Warning: Binary file name does not end with standard suffix \".lltp32.bin\".");

        Console.WriteLine("Reading binary file");
        byte[] binFileContent = File.ReadAllBytes(executableFilePath);
        Program = ByteArrayToUIntArray(binFileContent);
        Console.WriteLine($"Decoded binary file into {Program.Length} words.");
        CheckAsmVersion();
    }
    public ExecutableWrapper(uint[] program)
    {
        Program = program;
        CheckAsmVersion();
    }
    private void CheckAsmVersion()
    {
        if (AssemblerVersion != Assembler.ASSEMBLER_VERSION_ENCODED)
        {
            string asmVersionInFile = Assembler.GetVersionStringFromEncodedValue(AssemblerVersion);
            string asmVersionRequired = Assembler.GetVersionStringFromEncodedValue(Assembler.ASSEMBLER_VERSION_ENCODED);
            throw new NotSupportedException(
                $"Encoded assembler version mismatch " +
                $"(Found: {asmVersionInFile} != Required: {asmVersionRequired})");
        }
        Console.WriteLine("Assembly version check successful!");
    }

    public void WriteExecutableBinaryToFile(string filePath)
    {
        WriteBytesToFile(UIntArrayToByteArray(Program), filePath);
        Console.WriteLine($"Binary executable file written at {filePath}");
    }

    private static uint[] ByteArrayToUIntArray(byte[] byteArray)
    {
        if (byteArray.Length % 4 != 0)
            throw new ArgumentException("Byte array length not divisible by 4.");

        uint[] uintArray = new uint[byteArray.Length / 4];
        for (int uintIdx = 0; uintIdx < uintArray.Length; uintIdx++)
        {
            uint currentUInt = 0;
            int byteIdx = uintIdx * 4;
            currentUInt |= ((uint)byteArray[byteIdx + 0]) << 24;
            currentUInt |= ((uint)byteArray[byteIdx + 1]) << 16;
            currentUInt |= ((uint)byteArray[byteIdx + 2]) << 8;
            currentUInt |= ((uint)byteArray[byteIdx + 3]) << 0;
            uintArray[uintIdx] = currentUInt;
        }
        return uintArray;
    }
    private static byte[] UIntArrayToByteArray(uint[] uintArray)
    {
        // TODO: Fix potential errors with very large programs exceeding C# array size limits.
        byte[] byteArray = new byte[uintArray.Length * 4];
        for (int uintIdx = 0; uintIdx < uintArray.Length; uintIdx++)
        {
            int byteIdx = uintIdx * 4;
            byteArray[byteIdx + 0] = (byte)((uintArray[uintIdx] & 0xFF000000) >> 24);
            byteArray[byteIdx + 1] = (byte)((uintArray[uintIdx] & 0x00FF0000) >> 16);
            byteArray[byteIdx + 2] = (byte)((uintArray[uintIdx] & 0x0000FF00) >> 8);
            byteArray[byteIdx + 3] = (byte)((uintArray[uintIdx] & 0x000000FF) >> 0);
        }
        return byteArray;
    }

    private static void WriteBytesToFile(byte[] bytes, string filePath)
    {
        FileStream outputBinaryFileStream = File.Open(filePath, FileMode.Create);
        using (BinaryWriter binaryWriter = new(outputBinaryFileStream))
        {
            Console.WriteLine($"Write bytes count {bytes.Length}");
            binaryWriter.Write(bytes);
        }
    }
}