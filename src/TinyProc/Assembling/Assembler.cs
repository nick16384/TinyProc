using System.Text.RegularExpressions;
using TinyProc.Application;
using TinyProc.Assembling.Sections;
using TinyProc.Processor;

namespace TinyProc.Assembling;

// TODO: Find a better name for this class
/// <summary>
/// A meta class containing machine code from an assembled program, both the .data and .text section, and assembly header information.
/// </summary>
public class AssemblerOutput
{
    public readonly struct AssemblyHeader(uint versionEncoded, uint loadAddress, uint entryPoint, uint dataSize, uint textSize)
    {
        public readonly uint VersionEncoded = versionEncoded;
        public readonly uint LoadAddress = loadAddress;
        public readonly uint EntryPoint = entryPoint;
        public readonly uint DataSegmentSize = dataSize;
        public readonly uint TextSegmentSize = textSize;
        // The last 0x0 words are padding reserved for future use:
        public readonly uint[] MachineCodeBinary = [versionEncoded, loadAddress, entryPoint, dataSize, textSize, 0x0, 0x0, 0x0];
    }
    public readonly AssemblyHeader Header;
    public readonly DataSection DataSection;
    public readonly TextSection TextSection;
    public readonly uint[] MachineCodeBinary;
    public AssemblerOutput(uint loadAddress, DataSection dataSection, TextSection textSection)
    {
        DataSection = dataSection;
        TextSection = textSection;
        Header = new(Assembler.ASSEMBLER_VERSION_ENCODED, loadAddress, TextSection.EntryPoint, DataSection.Size, TextSection.Size);
        MachineCodeBinary = [.. Header.MachineCodeBinary, .. DataSection.BinaryRepresentation, .. TextSection.BinaryRepresentation];
    }
}

/// <summary>
/// The assembler reads assembly program code (as a string) and assembles (converts) it into a machine code.
/// It is crucial to note that this is the only step this class is supposed to do.
/// </summary>
public partial class Assembler
{
    /// <summary>
    /// Takes in HLTP32 assembly code (e.g. read from a source file) and converts it into machine code.
    /// The machine code contains a .data section for data and a .text section for executable instructions in sequence.
    /// The .text section follows the .data section.
    /// </summary>
    /// <remarks>
    /// Note: The code of this function is <b>not</b> meant to be performant in any way (preferring code readability),
    /// i.e. it may take some ages to assemble a lot of assembly code.
    /// </remarks>
    /// <param name="assemblyCode"></param>
    /// <returns>A meta class containing machine code, both sections and the load address.</returns>
    /// <exception cref="Exception">If the assembler encounters an error.</exception>
    public static AssemblerOutput Assemble(string assemblyCode)
    {
        // Assembling steps:
        // 1. Cleanup: Remove comments and excess whitespace
        // 2. Check assembly version (first line)
        // 3. Tokenize: Parse code as a list of tokens for the assembler to work with
        // 4. Pre-parse: Expand #DEFINE and "times N"
        // 5. Extract load address (#ORG)
        // 6. .data section parser
        // 7. .text section parser

        assemblyCode = FilterCommentsAndRemoveExcessWhitespace(assemblyCode);
        Logging.LogInfo(
            "\n===== Assembly begin =====\n" +
            assemblyCode +
            "\n=====  Assembly end  =====");
        if (!CheckAssemblyVersion(assemblyCode))
            throw new Exception("Invalid assembly version.");
        Token[] assemblyTokens = TokenizeAssembly(assemblyCode);
        Logging.LogDebug(
            "\n===== Tokenized begin =====");
        assemblyTokens.ToList().ForEach(t => {
            if (t.Type == TokenType.EOS) { Logging.NewlineDebug(); return; }
            Logging.PrintDebug($"{t.Type}({t.Value}) ");
        });
        Logging.PrintDebug(
            "===== Tokenized end =====\n");
        List<Statement> assemblyStatements = TokensToStatements(assemblyTokens);
        assemblyStatements = PreParse(assemblyStatements);

        uint loadAddress = FindLoadAddress(assemblyStatements);

        // ========== Process .data and .text sections ==========

        // Search for the .data section:
        int dataSectionStart = assemblyStatements.TakeWhile(statement =>
            statement.STLength < 2 ||
            statement.Tokens[0].Type != TokenType.DIRECTIVE_SECTION ||
            statement.Tokens[^2].Type != TokenType.DIRECTIVE_SECTION_DATA 
        ).Count();
        if (dataSectionStart >= assemblyStatements.Count)
            throw new Exception("No .data section found.");
        // Search for the .text section:
        int textSectionStart = assemblyStatements.TakeWhile(statement =>
            statement.STLength < 2 ||
            statement.Tokens[0].Type != TokenType.DIRECTIVE_SECTION ||
            statement.Tokens[^2].Type != TokenType.DIRECTIVE_SECTION_TEXT 
        ).Count();
        if (textSectionStart >= assemblyStatements.Count)
            throw new Exception("No .text section found.");
        
        int dataSectionEndExclusive = textSectionStart;
        int textSectionEndExclusive = assemblyStatements.Count;
        
        // TODO: Add support for multiple .data and .text sections in arbitrary order

        DataSection dataSection = DataSection.CreateFromAssemblyCode(assemblyStatements[dataSectionStart .. dataSectionEndExclusive]);
        TextSection textSection = TextSection.CreateFromAssemblyCode(assemblyStatements[textSectionStart .. textSectionEndExclusive], dataSection);

        // Assembly header metadata words:
        // 1. Assembler version (byte 1: Major; byte 2: Minor, bytes 3 & 4: zero)
        // 2. Global load address
        // 3. Entry point (offset in .text section)
        // 4. .data segment size in words
        // 5. .text segment size in words
        // 6. 0x0
        // 7. 0x0
        // 8. 0x0
        Logging.LogDebug(
            "Assembly header:\n" +
            $"Version:.................{ASSEMBLER_VERSION_ENCODED:x8}\n" +
            $"Global load address:.....{loadAddress:x8}\n" +
            $"Entry point (rel):.......{textSection.EntryPoint:x8}\n" +
            $".data size (dec, words):.{dataSection.Size:x8}\n" +
            $".text size (dec, words):.{textSection.Size:x8}\n" +
            $"Padding (1):.............{0:x8}\n" +
            $"Padding (2):.............{0:x8}\n" +
            $"Padding (3):.............{0:x8}");
        
        return new AssemblerOutput(loadAddress, dataSection, textSection);
    }

    /// <summary>
    /// Converts a number string from<br></br>
    /// 1. Base 2 (prefix 0b)<br></br>
    /// 2. Base 10 (no prefix)<br></br>
    /// 3. Base 16 (prefix 0x or postfix h)<br></br>
    /// to a uint
    /// </summary>
    /// <param name="numStr">The number string</param>
    /// <param name="numValue">The numeric value. Null if the conversion was unsuccessful.</param>
    /// <returns>Whether the conversion was successful or not.</returns>
    internal static bool TryConvertStringToUInt(string numStr, out uint? numValue)
    {
        try { numValue = ConvertStringToUInt(numStr); }
        catch (Exception) { numValue = null; return false; }
        return true;
    }
    internal static uint ConvertStringToUInt(string numStr)
    {
        // Base-2 (binary)
        if (numStr.StartsWith("0b"))
            return Convert.ToUInt32(numStr[2..], 2);
        else if (numStr.EndsWith('b'))
            return Convert.ToUInt32(numStr[..^1], 2);
        // Base-16 (hexadecimal)
        else if (numStr.StartsWith("0x"))
            return Convert.ToUInt32(numStr[2..], 16);
        else if (numStr.EndsWith('h'))
            return Convert.ToUInt32(numStr[..^1], 16);
        // Base-10 (decimal) or unknown
        try { return Convert.ToUInt32(numStr); }
        catch (Exception) { throw new Exception($"Unable to parse \"{numStr}\" as uint."); }
    }

    /// <summary>
    /// Removes assembly comments (starting with a semicolon) and excess whitespace
    /// (this includes leading / trailing whitespace in lines and replacing e.g. multi-spaces or tabs with single spaces)
    /// </summary>
    /// <param name="assemblyCode"></param>
    /// <returns></returns>
    private static string FilterCommentsAndRemoveExcessWhitespace(string assemblyCode)
    {
        // FIXME: Edge case: When a comment is enclosed in quotes, it will still be discarded as a comment,
        // which is obviously not intended behaviour.
        string[] lines = assemblyCode.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        lines = [.. lines
            .Select(line => line.Split(";")[0]) // Discard everything after comment symbol
            .Select(line => Regex.Replace(line, @"\s+", " ")) // Replace multi-space or tab with single space
            .Select(line => line.Trim()) // Remove leading or trailing whitespace
            .Where(line => !string.IsNullOrWhiteSpace(line)) // Only keep strings with remaining content
            ];
        return string.Join("\n", lines);
    }

    private static bool CheckAssemblyVersion(string assemblyCode)
    {
        string firstLine = assemblyCode.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        bool isVersionValid = firstLine == $"{ASM_DIRECTIVE_VERSION} {ASSEMBLER_VERSION}";
        if (!isVersionValid)
            Logging.LogError($"Invalid assembly version. Missing {ASM_DIRECTIVE_VERSION} directive or wrong version.");
        return isVersionValid;
    }

    private static uint FindLoadAddress(List<Statement> assemblyStatements)
    {
        foreach (Statement statement in assemblyStatements)
        {
            foreach (Token token in statement.Tokens)
            {
                if (token.Type == TokenType.DIRECTIVE_ORG)
                {
                    if (statement.STLength < 2 || statement.Tokens[1].Type != TokenType.NUMERIC_VALUE)
                        throw new Exception($"Invalid load address specifier {ASM_DIRECTIVE_LOADADDRESS}: {statement}");
                    return ConvertStringToUInt(statement.Tokens[1].Value);
                }
            }
        }
        throw new Exception($"Unable to find load address specifier {ASM_DIRECTIVE_LOADADDRESS}");
    }

    #region Token shit

    /// <summary>
    /// Receives full assembly code (without comments) and splits the code into
    /// tokens usable by the assembler.
    /// </summary>
    /// <param name="assemblyCode"></param>
    /// <returns>A list of tokens in assembly with every statement ending in an EOL token.</returns>
    private static Token[] TokenizeAssembly(string assemblyCode)
    {
        string[] assemblyLines = assemblyCode.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        List<Token> tokens = [];
        foreach (string line in assemblyLines)
        {
            string[] words = SplitLineIntoWords(line);
            foreach (string word in words)
            {
                tokens.Add(StringToToken(word));
            }
            tokens.Add(Token.CreateEOS());
        }
        return [.. tokens];
    }
    // Scary regex: Matches single words, or words enclosed in quotes, brackets or square brackets,
    // while keeping brackets and square brackets separated, and enquoted strings intact including the quotes themselves.
    // Useful site: https://regex101.com/ (set to .NET flavor)
    private const string pattern = @"\[|\]|\(|\)|""[^""]+""|[^""\s\[\]\(\)""]+";
    private static readonly string[] prefilterSymbols = [","];
    /// <summary>
    /// Splits a line (expecting zero line breaks) into an array of words (splitting at whitespace and commas), but
    /// words enclosed in quotation marks (") and square brackets ([]) remain as one word.
    /// The function preserves both symbols in the resulting list of strings, but removes commas and spaces.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private static string[] SplitLineIntoWords(string line)
    {
        // Split into words
        var matches = Regex.Matches(line, pattern);
        string[] words = [.. matches.Select(m => m.Value.Trim())];
        // Remove separator strings (",") but not if they are enquoted
        foreach (string excludeSymbol in prefilterSymbols)
            words = [.. words.Select(word =>
            {
                // Don't remove characters in enquoted strings
                if (!word.StartsWith('\"') && !word.EndsWith('\"'))
                    return word.Replace(excludeSymbol, "");
                return word;
            })];
        words = [.. words.Where(word => !string.IsNullOrWhiteSpace(word))];
        return words;
    }
    private static Token StringToToken(string tokenString)
    {
        // Assembly directives
        if      (tokenString == ASM_DIRECTIVE_VERSION)
            return new Token(TokenType.DIRECTIVE_VERSION, ASM_DIRECTIVE_VERSION);
        else if (tokenString == ASM_DIRECTIVE_LOADADDRESS)
            return new Token(TokenType.DIRECTIVE_ORG, ASM_DIRECTIVE_LOADADDRESS);
        else if (tokenString == ASM_DIRECTIVE_DEFINE)
            return new Token(TokenType.DIRECTIVE_DEFINE, ASM_DIRECTIVE_DEFINE);
        else if (tokenString == ASM_DIRECTIVE_SECTION)
            return new Token(TokenType.DIRECTIVE_SECTION, ASM_DIRECTIVE_SECTION);
        else if (tokenString == ASM_DIRECTIVE_SECTION_DATA)
            return new Token(TokenType.DIRECTIVE_SECTION_DATA, ASM_DIRECTIVE_SECTION_DATA);
        else if (tokenString == ASM_DIRECTIVE_SECTION_TEXT)
            return new Token(TokenType.DIRECTIVE_SECTION_TEXT, ASM_DIRECTIVE_SECTION_TEXT);
        // Special keywords
        else if (tokenString == KEYWORD_DEFINEWORD)
            return new Token(TokenType.KEYWORD_DEFINEWORD, KEYWORD_DEFINEWORD);
        else if (tokenString == KEYWORD_EQUATE)
            return new Token(TokenType.KEYWORD_EQUATE, KEYWORD_EQUATE);
        else if (tokenString == KEYWORD_LENGTH)
            return new Token(TokenType.KEYWORD_LENGTH, KEYWORD_LENGTH);
        else if (tokenString == KEYWORD_TIMES)
            return new Token(TokenType.KEYWORD_TIMES, KEYWORD_TIMES);
        // "Normal" tokens
        // Single letters
        else if (tokenString == "[")
            return new Token(TokenType.SQUARE_BRACKET_OPEN, "[");
        else if (tokenString == "]")
            return new Token(TokenType.SQUARE_BRACKET_CLOSE, "]");
        else if (tokenString == "(")
            return new Token(TokenType.BRACKET_OPEN, "(");
        else if (tokenString == ")")
            return new Token(TokenType.BRACKET_CLOSE, ")");
        else if (tokenString == "+")
            return new Token(TokenType.SYMBOL_PLUS, "+");
        else if (tokenString == "-")
            return new Token(TokenType.SYMBOL_MINUS, "-");
        else if (tokenString == "*")
            return new Token(TokenType.SYMBOL_MULTIPLY, "*");
        else if (tokenString == "=")
            return new Token(TokenType.SYMBOL_EQUALS, "=");
        // Check if parseable as mnemonic
        // FIXME: Check for conditional codes!!!
        else if (InstructionLookup.MnemonicOpcodeMap.Keys.Any(mnemonicAndType => mnemonicAndType.Item1 == tokenString.ToUpper()))
            return new Token(TokenType.MNEMONIC, tokenString.ToUpper());
        // Check if parseable as register
        else if (Instructions.AddressableRegisterCode.IsValidRegisterName(tokenString.ToUpper()))
            return new Token(TokenType.REGISTER, tokenString.ToUpper());
        // Check if parseable as uint
        else if (TryConvertStringToUInt(tokenString, out _))
            return new Token(TokenType.NUMERIC_VALUE, tokenString);
        // Check if word is followed by colon, and implicitly assumes it's the only word in the line
        else if (tokenString.EndsWith(':'))
            return new Token(TokenType.LABEL, tokenString[..^1]);
        // Check if the word is enclosed in quotes
        else if (tokenString.StartsWith('"') && tokenString.EndsWith('"'))
            return new Token(TokenType.STRING, tokenString[1..^1]);
        else
            return new Token(TokenType.LITERAL_WORD, tokenString);
    }

    /// <summary>
    /// Convert a list of tokens to a list of statements. Every statement must end with an EOS token to be recognized as such.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    private static List<Statement> TokensToStatements(Token[] tokens)
    {
        List<Statement> statements = [];
        List<Token> currentStatementTokens = [];
        foreach (Token token in tokens)
        {
            currentStatementTokens.Add(token);
            if (token.Type == TokenType.EOS)
            {
                statements.Add(new Statement([.. currentStatementTokens]));
                currentStatementTokens = [];
            }
        }
        return [.. statements];
    }

    /// <summary>
    /// Checks if a string contains a string specified in the wildcard argument.
    /// This method is mostly similar to string.Contains(otherString), except it also
    /// accepts the wildcard symbol * (asterisk), which means any number of arbitrary
    /// characters in between.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="wildcard"></param>
    /// <returns></returns>
    internal static bool StringContainsWithWildcard(string data, string wildcard)
    {
        string pattern = Regex.Escape(wildcard).Replace(@"\#", "#").Replace(@"\*", ".*");
        return Regex.IsMatch(data, pattern);
    }

    internal static int StringIndexOfWithWildcard(string data, string wildcard)
    {
        string pattern = Regex.Escape(wildcard).Replace(@"\#", "#").Replace(@"\*", ".*");
        Match regexMatch = Regex.Match(data, pattern);
        return regexMatch.Success ? regexMatch.Index : throw new Exception($"String {pattern} not found in data.");
    }

    internal static int StringWildcardMatchCount(string data, string wildcard)
    {
        string pattern = Regex.Escape(wildcard).Replace(@"\#", "#").Replace(@"\*", ".*");
        MatchCollection matches = Regex.Matches(data, pattern);
        return matches.Count;
    }

    internal enum TokenType
    {
        DIRECTIVE_VERSION, // Technically unnecessary, since version checking is done before tokenization, but still here for completeness.
        DIRECTIVE_ORG, // Load address directive
        DIRECTIVE_DEFINE, // Define (similar to C's #define)
        DIRECTIVE_SECTION,
        DIRECTIVE_SECTION_DATA,
        DIRECTIVE_SECTION_TEXT,
        KEYWORD_DEFINEWORD,
        KEYWORD_EQUATE,
        KEYWORD_LENGTH,
        KEYWORD_TIMES,
        SQUARE_BRACKET_OPEN,
        SQUARE_BRACKET_CLOSE,
        BRACKET_OPEN,
        BRACKET_CLOSE,
        SYMBOL_PLUS,
        SYMBOL_MINUS,
        SYMBOL_MULTIPLY,
        SYMBOL_EQUALS,
        MNEMONIC, // Instruction mnemonic
        REGISTER, // Instruction register
        NUMERIC_VALUE,
        LABEL, // Address label
        LITERAL_WORD, // A literal word
        STRING, // A literal word enclosed in quotes
        EOS // End-of-statement (special token)
    }
    /// <summary>
    /// A token is an abstract type for representing symbols, strings, words or numbers as an alternative, more
    /// useful representation of code. Usually, tokens appear in a sequence, e.g. in lists or arrays representing the entire code.
    /// </summary>
    /// <param name="type">The token type, e.g. an opening bracket, string or numeric value.</param>
    /// <param name="value">The token value for non-fixed token types. (e.g. the number itself for numeric tokens)</param>
    internal class Token(TokenType type, string value)
    {
        public TokenType Type = type;
        public string Value = value;
        public uint AsUInt() => ConvertStringToUInt(Value);
        public Instructions.AddressableRegisterCode AsRegisterCode() => (Instructions.AddressableRegisterCode)Value;
        public byte[] AsByteArray()
        {
            List<byte> bytesList = [];
            foreach (char c in Value)
                bytesList.Add((byte)c);
            return [.. bytesList];
        }
        /// <summary>
        /// Copies the information of this token into another token.
        /// </summary>
        /// <param name="target"></param>
        public void CopyTo(Token target)
        {
            target.Type = Type;
            target.Value = Value;
        }
        /// <summary>
        /// Creates a new EOS (end-of-statement) token.
        /// </summary>
        /// <returns></returns>
        public static Token CreateEOS() => new(TokenType.EOS, "\n");
        /// <summary>
        /// Adds the values of the tokens together to a string and puts a space between them.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static string operator +(Token t1, Token t2) => t1.Value + " " + t2.Value;
        public static bool operator ==(Token t1, Token t2) => t1.Type == t2.Type && t1.Value == t2.Value;
        public static bool operator !=(Token t1, Token t2) => !(t1 == t2);
        public override bool Equals(object? obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }
    /// <summary>
    /// A statement is a logical group of tokens, usually representing a "single thing to be done".
    /// Most statements basically represent one line of code.
    /// </summary>
    /// <param name="statementTokens"></param>
    internal class Statement
    {
        public Token[] Tokens;
        /// <summary>
        /// The number of tokens in this statement excluding EOS.
        /// The name STLength helps to make clear this is different from any normal List.Length or string.Length.
        /// </summary>
        public int STLength { get => Tokens.Length - 1; }
        internal Statement(params Token[] statementTokens)
        {
            if (statementTokens.Length <= 0 || statementTokens[^1].Type != TokenType.EOS)
                throw new ArgumentException("Cannot create statement from empty token array or without finishing in EOS token.");
            Tokens = statementTokens;
        }
        public override string ToString() => string.Join(" ", Tokens.SkipLast(1).Select(token => token.Value));
    }

    #endregion Token shit
}