using TinyProc.Application;

namespace TinyProc.Assembling;

public partial class Assembler
{
    /// <summary>
    /// Receives full assembly code (e.g. read directly from a source file),
    /// splits it into code lines, and does some work (see below for more details) to make the code usable for
    /// the rest of the assembler.<br></br>
    /// Currently, the pre-parser does the following:<br></br>
    /// 1. Parses "#define" directives and replaces all occurrences of their labels.<br></br>
    /// 2. Parses "times N": Duplicates the following statement N times
    /// </summary>
    /// <param name="assemblyCode">The raw assembly source code</param>
    /// <returns>The lines of the assembly code after pre-parsing</returns>
    private static List<Statement> PreParse(List<Statement> assemblyStatements)
    {
        assemblyStatements = PreParseDefines(assemblyStatements);
        assemblyStatements = PreParseTimes(assemblyStatements);
        return assemblyStatements;
    }

    private static List<Statement> PreParseDefines(List<Statement> assemblyStatements)
    {
        // Stores define names associated with their values
        Dictionary<string, Token[]> defines = [];
        // Build dictionary
        foreach (Statement statement in assemblyStatements)
        {
            if (statement.Tokens[0].Type == TokenType.DIRECTIVE_DEFINE)
            {
                if (statement.Length < 3)
                    throw new Exception($"Incorrect {ASM_DIRECTIVE_DEFINE} directive: {statement}");
                string name = statement.Tokens[1].Value;
                Token[] values = statement.Tokens[2..^1];
                Logging.LogDebug($"Found define: \"${name}\" = \"{new Statement(values)}\"");
                // The define needs to be prepended with a "$" in code. This is for mere convenience.
                // It no longer serves an actual purpose except being a clear syntactical difference from constants / pointers.
                defines.Add("$" + name, values);
            }
        }
        // Remove all #define statements
        for (int i = 0; i < assemblyStatements.Count; i++)
            if (assemblyStatements[i].Tokens[0].Type == TokenType.DIRECTIVE_DEFINE)
                assemblyStatements.RemoveAt(i--);

        foreach (Statement statement in assemblyStatements)
        {
            for (int tokenIdx = 0; tokenIdx < statement.Tokens.Length; tokenIdx++)
            {
                Token token = statement.Tokens[tokenIdx];
                Console.WriteLine($"Check token {token.Value}");
                if (token.Type == TokenType.LITERAL_WORD && defines.Keys.Any(name => name == token.Value))
                {
                    Console.WriteLine($"Define match: {token.Value}");
                    Token[] replacement = defines[token.Value];
                    List<Token> newTokens = [.. statement.Tokens];
                    newTokens.RemoveAt(tokenIdx);
                    newTokens.InsertRange(tokenIdx, replacement);
                    statement.Tokens = [.. newTokens];
                    tokenIdx += replacement.Length;
                }
            }
        }
        return assemblyStatements;
    }

    private static List<Statement> PreParseTimes(List<Statement> assemblyStatements)
    {
        for (int i = 0; i < assemblyStatements.Count; i++)
        {
            Statement statement = assemblyStatements[i];
            if (statement.Tokens[0].Type == TokenType.KEYWORD_TIMES)
            {
                if (statement.Length < 3 || statement.Tokens[1].Type != TokenType.NUMERIC_VALUE)
                    throw new Exception($"Invalid {KEYWORD_TIMES} N: {statement}");
                List<Token> duplicateTokens = [.. statement.Tokens[2..^1]];
                int repeatTimes = (int)ConvertStringToUInt(statement.Tokens[1].Value);

                assemblyStatements.RemoveAt(i--);
                for (int j = 0; j < repeatTimes; j++)
                    assemblyStatements.Insert(i++, new Statement([.. duplicateTokens, new Token(TokenType.EOS, "\n")]));
            }
        }
        return assemblyStatements;
    }
}