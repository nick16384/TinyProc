using System.Text;

namespace TinyProc.Assembling;

/// <summary>
/// A static class for the sole purpose of creating nicely formatted hexdumps
/// of binary data in text.
/// </summary>
public class Hexdump()
{
    // TODO: Implement these:
    private const bool BigEndian = true;
    private const int GroupSize = sizeof(uint);

    private const uint BytesPerLine = 8;
    private const uint UInt32sPerLine = BytesPerLine / sizeof(uint);

    public static string CreateTextHexdump(uint[] data)
    {
        StringBuilder printStringBuilder = new();
        int strIdxAddressBegin = 0;
        int strIdxDataBegin = sizeof(uint) * 2 + 3;
        int strIdxASCIIBegin = strIdxDataBegin + (int)UInt32sPerLine * (sizeof(uint) * 2 + 1) + 1;
        printStringBuilder.Append("ADDRESS");
        printStringBuilder.Append(new string(' ', strIdxDataBegin - strIdxAddressBegin - "ADDRESS".Length));
        printStringBuilder.Append("DATA");
        printStringBuilder.Append(new string(' ', strIdxASCIIBegin - strIdxDataBegin - "DATA".Length));
        printStringBuilder.Append("ASCII");
        printStringBuilder.AppendLine();
        for (uint baseAddr = 0; baseAddr < data.Length; baseAddr += UInt32sPerLine)
        {
            printStringBuilder.Append($"{baseAddr:x8}   ");
            for (uint addr = baseAddr; addr - baseAddr < UInt32sPerLine && addr < data.Length; addr++)
            {
                printStringBuilder.Append($"{data[addr]:x8} ");
            }
            while (printStringBuilder.ToString().Split('\n')[^1].Length < strIdxASCIIBegin)
                printStringBuilder.Append(' ');
            for (uint addr = baseAddr; addr - baseAddr < UInt32sPerLine && addr < data.Length; addr++)
            {
                uint word = data[addr];
                char c1 = (char)((word & 0xFF000000) >> 24);
                char c2 = (char)((word & 0x00FF0000) >> 16);
                char c3 = (char)((word & 0x0000FF00) >> 8);
                char c4 = (char)((word & 0x000000FF) >> 0);
                printStringBuilder.Append(IsPrintable(c1) ? c1 : '.');
                printStringBuilder.Append(IsPrintable(c2) ? c2 : '.');
                printStringBuilder.Append(IsPrintable(c3) ? c3 : '.');
                printStringBuilder.Append(IsPrintable(c4) ? c4 : '.');
                printStringBuilder.Append(' ');
            }
            printStringBuilder.AppendLine();
        }
        if (data.Length == 0)
            printStringBuilder.Append("<empty>\n");
        return printStringBuilder.ToString();
    }

    public static string CreateTextHexdump(byte[] data)
    {
        // FIXME: Check for possible problems with hexdumping from LE-stored uints!
        throw new NotImplementedException("see fixme in hexdump class");
        StringBuilder printStringBuilder = new("");
        for (uint byteIdx = 0; byteIdx < data.Length; byteIdx += BytesPerLine)
        {
            printStringBuilder.Append($"{byteIdx:x8}   ");
            for (uint idx = byteIdx; idx - byteIdx < BytesPerLine && idx < data.Length; idx++)
            {
                printStringBuilder.Append($"{data[idx]:x2} ");
            }
            printStringBuilder.Append(' ');
            for (uint idx = byteIdx; idx - byteIdx < BytesPerLine && idx < data.Length; idx++)
            {
                printStringBuilder.Append(IsPrintable((char)data[byteIdx]) ? (char)data[byteIdx] : '.');
            }
            printStringBuilder.AppendLine();
        }
        if (data.Length == 0)
            printStringBuilder.Append("<empty>\n");
        return printStringBuilder.ToString();
    }

    private static bool IsPrintable(char input)
    {
        return input >= 0x20 && input <= 0x7E;
    }
}