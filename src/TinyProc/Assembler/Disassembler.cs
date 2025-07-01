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

        foreach ((uint, uint) instruction in programWrapper.ExecutableProgram.Select((word1, word2) => (word1, word2)))
        {
            // Loop through program tuples
            assemblyCodeBuilder.Append(StringFromInstructionWords(instruction));  

            // Append newline at the end of every instruction
            assemblyCodeBuilder.Append('\n');
        }

        // Convert to string and remove last unnecessary newline
        return assemblyCodeBuilder.ToString().Trim();

        throw new NotImplementedException();
    }
}