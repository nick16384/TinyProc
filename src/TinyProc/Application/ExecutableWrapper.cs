namespace TinyProc.Application;

using TinyProc.Assembling;

// Wrapper for some binary data resembling HLTP32 code.
// Can extract header information, read a binary file and save newly written files.
public class ExecutableWrapper
{
    public uint[] Program;

    public uint AssemblerVersion { get => Program[Assembler.HEADER_INDEX_VERSION]; }
    public uint EntryPoint { get => Program[Assembler.HEADER_INDEX_ENTRY_POINT]; }
    public uint DataSectionLoadAddress { get => Program[Assembler.HEADER_INDEX_SEGMENT_DATA_LOADADDRESS]; }
    public uint DataSectionSize { get => Program[Assembler.HEADER_INDEX_SEGMENT_DATA_SIZE]; }
    public uint TextSectionLoadAddress { get => Program[Assembler.HEADER_INDEX_SEGMENT_TEXT_LOADADDRESS]; }
    public uint TextSectionSize { get => Program[Assembler.HEADER_INDEX_SEGMENT_TEXT_SIZE]; }

    public static ExecutableWrapper LoadProgramFromFile(string executableFilePath)
    {
        Logging.LogInfo($"Attempting to load binary executable {executableFilePath}");
        if (executableFilePath.Trim().EndsWith(".hltp32.bin"))
            Logging.LogWarn("Warning: Binary file name does not end with standard suffix \".hltp32.bin\".");

        Logging.LogDebug("Reading binary file");
        byte[] binFileContent = File.ReadAllBytes(executableFilePath);
        ExecutableWrapper executable = new(ByteArrayToUIntArray(binFileContent));
        Logging.LogDebug($"Decoded binary file into {executable.Program.Length} words.");
        executable.CheckAsmVersion();
        return executable;
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
        Logging.LogDebug("Assembly version check successful!");
    }

    public void WriteExecutableBinaryToFile(string filePath)
    {
        WriteBytesToFile(UIntArrayToByteArray(Program), filePath);
        Logging.LogInfo($"Binary executable file written at {filePath}");
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
        using BinaryWriter binaryWriter = new(outputBinaryFileStream);
        Logging.LogDebug($"Write bytes count {bytes.Length}");
        binaryWriter.Write(bytes);
    }
}