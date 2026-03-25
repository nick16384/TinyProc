using TinyProc.Application;

namespace TinyProc.Assembling;

public partial class Assembler
{
    /// <summary>
    /// Receives full assembly code (e.g. read directly from a source file),
    /// splits it into code lines, and does some work (see below for more details) to make the code usable for
    /// the rest of the assembler.<br></br>
    /// Currently, the pre-parser does the following:<br></br>
    /// 1. Splits the code into lines, removes comments and excess whitespace<br></br>
    /// 2. Checks for the correct assembly version.<br></br>
    /// 3. Parses #define directives and replaces all occurrences of their labels.
    /// </summary>
    /// <param name="assemblyCode">The raw assembly source code</param>
    /// <returns>The lines of the assembly code after pre-parsing</returns>
    private static List<string> PreParse(string assemblyCode)
    {
        // FIXME: Edge case: When a comment is enclosed in quotes, it will still be discarded as a comment,
        // which is obviously not intended behaviour.

        // TODO: Work with StringBuilder instead of rewriting strings over and over.
        // use separate indices array to mark newlines, words, etc.
        // If the StringBuilder content needs to be updated (e.g. labels replaced), also update the index arrays.
        List<string> assemblyLines = [.. assemblyCode.Split("\n")];
        assemblyLines = FilterCommentsAndRemoveExcessWhitespace(assemblyLines);
        assemblyCode = string.Join("\n", assemblyLines);

        Logging.LogInfo("===== Assembly begin =====");
        assemblyLines.ForEach(Logging.LogInfo);
        Logging.LogInfo("=====  Assembly end  =====");

        if (!assemblyLines[0].StartsWith(ASM_DIRECTIVE_VERSION))
            throw new Exception($"Missing {ASM_DIRECTIVE_VERSION} directive at the start. Cannot determine assembly version.");
        if (SplitLineIntoWords(assemblyLines[0]).Length < 2)
            throw new Exception($"Missing version number after {ASM_DIRECTIVE_VERSION} directive.");
        string versionInAssemblyStr = SplitLineIntoWords(assemblyLines[0])[1];
        if (versionInAssemblyStr != ASSEMBLER_VERSION)
            throw new Exception($"Incompatible assembler version: Expected {ASSEMBLER_VERSION}, got {versionInAssemblyStr} instead.");
        Logging.LogDebug("Assembly version check successful!");

        // Convert labels and string literals to addresses and values
        foreach (string line in assemblyLines)
        {
            // Convert string literals to uint sequences
            string[] words = SplitLineIntoWords(line);
            foreach (string word in words)
            {
                if (word.StartsWith('\"') && word.EndsWith('\"'))
                {
                    string wordWithoutQuotes = new([.. word.Skip(1).SkipLast(1)]);
                    List<string> wordUInts = [];
                    for (int i = 0; i < wordWithoutQuotes.Length; i += 4)
                    {
                        // Each block of 4 letters can be represented as a uint
                        uint char1 = (i + 0) < wordWithoutQuotes.Length ? wordWithoutQuotes[i + 0] : 0u;
                        uint char2 = (i + 1) < wordWithoutQuotes.Length ? wordWithoutQuotes[i + 1] : 0u;
                        uint char3 = (i + 2) < wordWithoutQuotes.Length ? wordWithoutQuotes[i + 2] : 0u;
                        uint char4 = (i + 3) < wordWithoutQuotes.Length ? wordWithoutQuotes[i + 3] : 0u;
                        uint textBlockAsUInt =
                                char1 << 24
                            | char2 << 16
                            | char3 << 8
                            | char4 << 0;
                        wordUInts.Add(textBlockAsUInt.ToString("x8") + "h");
                    }
                    string wordUIntRepresentation = string.Join(" + ", wordUInts);
                    assemblyCode = assemblyCode.Replace(word, wordUIntRepresentation);
                    assemblyLines = [.. assemblyCode.Split("\n")];
                    assemblyLines = FilterCommentsAndRemoveExcessWhitespace(assemblyLines);
                    Logging.LogDebug($"Replaced string literal {word} with uint sequence {wordUIntRepresentation}");
                }
            }
        }

        Logging.LogWarn("Pre-parser missing parsing #define directives!");

        return assemblyLines;
    }
}