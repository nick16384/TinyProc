namespace TinyProc.Assembling;

public partial class Assembler
{
    public static string DisassembleFromProgram(HLTPExecutable programWrapper)
    {
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