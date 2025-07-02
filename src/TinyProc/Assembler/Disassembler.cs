using System.Text;
using TinyProc.Application;

namespace TinyProc.Assembler;

public partial class Assembler
{
    public static string DisassembleFromProgram(ExecutableWrapper programWrapper)
    {
        Logging.LogDebug("Disassembling program. Extracted header metadata:\n" +
        $"Assembler version: {GetVersionStringFromEncodedValue(programWrapper.AssemblerVersion)}\n" +
        $"RAM Start:   {programWrapper.RAMRegionStart:X8}\n" +
        $"RAM End:     {programWrapper.RAMRegionEnd:X8}\n" +
        $"CON Start:   {programWrapper.CONRegionStart:X8}\n" +
        $"CON End:     {programWrapper.CONRegionEnd:X8}\n" +
        $"Entry Point: {programWrapper.EntryPoint:X8}");

        StringBuilder assemblyCodeBuilder = new();

        foreach ((uint, uint) instruction in ConvertArrayToTuples(programWrapper.ExecutableProgram))
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

        throw new NotImplementedException();
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