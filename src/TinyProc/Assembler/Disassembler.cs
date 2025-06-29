namespace TinyProc.Assembler;

public partial class Assembler
{
    public static string DisassembleFromMachineCode(uint[] assembledProgram)
    {
        uint versionMajor = assembledProgram[0];
        uint versionMinor = assembledProgram[1];

        throw new NotImplementedException();
    }
}