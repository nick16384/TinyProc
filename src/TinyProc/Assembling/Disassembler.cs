using System.Text;
using TinyProc.Application;

namespace TinyProc.Assembling;

public partial class Assembler
{
    public static string DisassembleFromProgram(ExecutableWrapper programWrapper)
    {
        Logging.LogDebug("Disassembling program. Extracted header metadata:\n" +
        $"Assembler version: {GetVersionStringFromEncodedValue(programWrapper.AssemblerVersion)}\n" +
        $"Entry Point: {programWrapper.EntryPoint:X8}\n" +
        $".data addr:  {programWrapper.DataSectionLoadAddress:X8}\n" +
        $".data size:  {programWrapper.DataSectionSize:X8}\n" +
        $".text addr:  {programWrapper.TextSectionLoadAddress:X8}\n" +
        $".text size:  {programWrapper.TextSectionSize:X8}");

        return DisassembleFromProgram(programWrapper.Program);
    }

    public static string DisassembleFromProgram(uint[] executableProgram)
    {
        StringBuilder assemblyCodeBuilder = new();

        foreach ((uint, uint) instruction in ConvertArrayToTuples(executableProgram))
        {
            // Loop through program instruction word tuples
            string disassembledInstructionAssembly = StringFromSingleInstruction(instruction);
            Logging.LogDebug($"Decoded {instruction.Item1:X8}, {instruction.Item2:X8} into \"{disassembledInstructionAssembly}\"");
            assemblyCodeBuilder.Append(disassembledInstructionAssembly);

            // Append newline at the end of every instruction
            assemblyCodeBuilder.Append('\n');
        }

        // Convert to string and remove last unnecessary newline
        return assemblyCodeBuilder.ToString().Trim();
    }

    public static string DisassembleFromProgramWithHeader(ExecutableWrapper programWrapper)
    {
        string header =
            $"[Imagine header here (To be implemented)]";
        return header + "\n\n" + DisassembleFromProgram(programWrapper);
    }

    public static string DisassembleFromProgramWithHeader(uint[] executableProgram,
        string? version = null, uint? ramStart = null, uint? ramEnd = null, uint? conStart = null, uint? conEnd = null)
    {
        version ??= "2.0";
        ramStart ??= 0x00000000u;
        ramEnd ??= (uint)executableProgram.Length;
        conStart ??= ramEnd + 1;
        conEnd ??= conStart + 1;
        string header =
            $"#VERSION {version}\n" +
            $"#MEMREGION RAM 0x{ramStart:X8} 0x{ramEnd:X8}\n" +
            $"#MEMREGION CON 0x{conStart:X8} 0x{conEnd:X8}";
        return header + "\n\n" + DisassembleFromProgram(executableProgram);
    }

    private static (T, T)[] ConvertArrayToTuples<T>(T[] array)
    {
        if (array.Length % 2 != 0)
            throw new ArgumentException("Cannot convert array to tuples: Number of elements is odd.");

        return
            [..
                Enumerable
                .Range(0, array.Length / 2)
                .Select(i => (array[2 * i], array[2 * i + 1]))
            ];
    }
}