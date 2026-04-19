namespace TinyProc.Assembling;

using TinyProc.Application;
using TinyProc.Assembling.Sections;
using static TinyProc.Assembling.Assembler;

/// <summary>
/// Wrapper for a HLTP32 executable.
/// </summary>
public class HLTPExecutable
{
    public readonly struct AssemblyHeader(uint versionEncoded, uint loadAddress, uint entryPoint, uint dataSize, uint textSize)
    {
        public const int SIZE_WORDS = 8;
        public readonly uint VersionEncoded = versionEncoded;
        public readonly uint LoadAddress = loadAddress;
        public readonly uint EntryPoint = entryPoint;
        public readonly uint DataSegmentSize = dataSize;
        public readonly uint TextSegmentSize = textSize;
        // The last 0x0 words are padding reserved for future use:
        public readonly uint[] MachineCodeBinary = [versionEncoded, loadAddress, entryPoint, dataSize, textSize, 0x0, 0x0, 0x0];
    }
    public readonly uint[] MachineCode;
    public readonly uint[] MachineCodeWithHeader;
    public readonly AssemblyHeader Header;
    public readonly DataSection DataSection;
    public readonly TextSection TextSection;
    
    public static HLTPExecutable LoadProgramFromFile(string executableFilePath)
    {
        Logging.LogInfo($"Attempting to load binary executable {executableFilePath}");
        Logging.LogDebug("Reading binary file");
        byte[] binFileContent = File.ReadAllBytes(executableFilePath);
        HLTPExecutable executable = new(ByteArrayToUIntArray(binFileContent));
        Logging.LogDebug($"Decoded binary file into {executable.MachineCodeWithHeader.Length} words.");
        executable.AssertValidAsmVersion();
        return executable;
    }
    public HLTPExecutable(uint loadAddress, DataSection dataSection, TextSection textSection)
    {
        DataSection = dataSection;
        TextSection = textSection;
        Header = new(Assembler.ASSEMBLER_VERSION_ENCODED, loadAddress, TextSection.EntryPoint, DataSection.Size, TextSection.Size);
        MachineCodeWithHeader = [.. Header.MachineCodeBinary, .. DataSection.BinaryRepresentation, .. TextSection.BinaryRepresentation];
        MachineCode = [.. DataSection.BinaryRepresentation, .. TextSection.BinaryRepresentation];
        AssertValidAsmVersion();
    }
    public HLTPExecutable(uint[] program)
    {
        MachineCodeWithHeader = program;
        MachineCode = program[AssemblyHeader.SIZE_WORDS..];
        Header = new(program[0], program[1], program[2], program[3], program[4]);
        AssertValidAsmVersion();
    }
    private void AssertValidAsmVersion()
    {
        if (Header.VersionEncoded != Assembler.ASSEMBLER_VERSION_ENCODED)
        {
            string asmVersionInFile = Assembler.GetVersionStringFromEncodedValue(Header.VersionEncoded);
            string asmVersionRequired = Assembler.GetVersionStringFromEncodedValue(Assembler.ASSEMBLER_VERSION_ENCODED);
            throw new NotSupportedException(
                $"Encoded assembler version mismatch " +
                $"(Found: {asmVersionInFile} != Required: {asmVersionRequired})");
        }
        Logging.LogDebug("Assembly version check successful!");
    }

    public void WriteExecutableBinaryToFile(string filePath, bool includeHeader = true)
    {
        if (includeHeader)
            WriteBytesToFile(UIntArrayToByteArray(MachineCodeWithHeader), filePath);
        else
            WriteBytesToFile(UIntArrayToByteArray(MachineCode), filePath);
        Logging.LogInfo($"Binary executable file written at {filePath}");
    }

    private static void WriteBytesToFile(byte[] bytes, string filePath)
    {
        FileStream outputBinaryFileStream = File.Open(filePath, FileMode.Create);
        using BinaryWriter binaryWriter = new(outputBinaryFileStream);
        Logging.LogDebug($"Write bytes count {bytes.Length}");
        binaryWriter.Write(bytes);
    }
}