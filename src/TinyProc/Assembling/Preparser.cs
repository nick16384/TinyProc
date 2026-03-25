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

        // Stores directive names associated with their values
        Dictionary<string, string> directives = [];
        // Build dictionary
        foreach (string line in assemblyLines)
        {
            if (line.StartsWith(ASM_DIRECTIVE_DEFINE))
            {
                string[] words = line.Split([' '], 3);
                if (words.Length != 3 || words[0] != ASM_DIRECTIVE_DEFINE)
                    throw new Exception($"Incorrect {ASM_DIRECTIVE_DEFINE} directive: {line}");
                
                string name = words[1];
                string value = words[2];
                Logging.LogDebug($"Found define: \"{name}\" = \"{value}\"");
                directives.Add(name, value);
            }
        }
        // Second iteration: Replace names in previously built dictionary with their values in code
        for (int i = 0; i < assemblyLines.Count; i++)
        {
            string line = assemblyLines[i];
            foreach ((string name, string value) in directives)
            {
                // To ensure not any word within another word is replaced to produce garbage,
                // the name is prepended with a "$" sign during use. This is different from e.g. the C pre-parser, but
                // prevents this code section from growing too large to check for tokens instead of occurrences.
                if (line.Contains("$" + name))
                    line = line.Replace("$" + name, value);
            }
            assemblyLines[i] = line;
        }

        return assemblyLines;
    }
}