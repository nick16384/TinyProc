namespace TinyProc.Assembling;

public partial class Assembler
{
    public const string ASSEMBLER_VERSION = "3.0";
    public const uint ASSEMBLER_VERSION_ENCODED = (3 << 24) | (0 << 16);
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
    public const int HEADER_INDEX_ENTRY_POINT = 1;
    public const int HEADER_INDEX_SEGMENT_DATA_LOADADDRESS = 2;
    public const int HEADER_INDEX_SEGMENT_DATA_SIZE = 3;
    public const int HEADER_INDEX_SEGMENT_TEXT_LOADADDRESS = 4;
    public const int HEADER_INDEX_SEGMENT_TEXT_SIZE = 5;

    // Assembler directives
    internal const string ASM_DIRECTIVE_VERSION = "#VERSION";
    internal const string ASM_DIRECTIVE_SECTION = "#SECTION";
    internal const string ASM_DIRECTIVE_SECTION_DATA = ".data";
    internal const string ASM_DIRECTIVE_SECTION_TEXT = ".text";
    internal const string ASM_DIRECTIVE_ENTRYPOINT = "#ENTRY";

    // Attributes
    internal const string ASM_ATTRIBUTE = "__attribute__";
    internal const string ASM_ATTRIBUTE_SECTION_LOADADDRESS = "loadaddress";
    internal const string ASM_ATTRIBUTE_INLINE = "inline";
    internal const string ASM_ATTRIBUTE_INLINE_ALL = "all";

    // Keywords
    internal const string KEYWORD_IMMEDIATE = "immediate";
    internal const string KEYWORD_POINTER = "pointer";
    internal const string KEYWORD_BLOCK = "block";
    internal const string KEYWORD_SPECIAL_LENGTH = "len:";
}