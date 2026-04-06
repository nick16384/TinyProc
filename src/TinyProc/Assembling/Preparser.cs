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
        Dictionary<string, string> defines = [];
        // Build dictionary
        foreach (Statement statement in assemblyStatements)
        {
            if (statement.Tokens[0].Type == TokenType.DIRECTIVE_DEFINE)
            {
                if (statement.Length < 3)
                    throw new Exception($"Incorrect {ASM_DIRECTIVE_DEFINE} directive: {statement}");
                string name = statement.Tokens[1].Value;
                string value = statement.Tokens[2..^1].Select(t => t.Value).Aggregate((t1, t2) => t1 + t2);
                Logging.LogDebug($"Found define: \"{name}\" = \"{value}\"");
                defines.Add(name, value);
            }
        }
        // Remove all #define statements
        for (int i = 0; i < assemblyStatements.Count; i++)
            if (assemblyStatements[i].Tokens[0].Type == TokenType.DIRECTIVE_DEFINE)
                assemblyStatements.RemoveAt(i--);

        // The define needs to be prepended with a "$" (or anything else, but dollar sign is convention) in code. This is for mere convenience.
        // It no longer serves an actual purpose except being a clear syntactical difference from constants / pointers.
        foreach (Statement statement in assemblyStatements)
            foreach (Token token in statement.Tokens)
            {
                Console.WriteLine($"Check token {token.Value}");
                if (token.Type == TokenType.LITERAL_WORD && defines.Keys.Any(name => name == token.Value[1..]))
                {
                    Console.WriteLine($"Define match: {token.Value}");
                    // StringToToken() redetermines the type.
                    // Since "token" cannot be set directly as a foreach iteration veriable, we modify it's members.
                    Token newToken = StringToToken(defines[token.Value[1..]]);
                    token.Type = newToken.Type;
                    token.Value = newToken.Value;
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