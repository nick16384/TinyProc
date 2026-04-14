using System.Text.RegularExpressions;
using TinyProc.Application;
using TinyProc.Assembling.Sections;
using TinyProc.Processor;

namespace TinyProc.Assembling;

public partial class Assembler
{
    /// <summary>
    /// Receives full assembly code (without comments) and splits the code into
    /// tokens usable by the assembler.
    /// </summary>
    /// <param name="code"></param>
    /// <returns>A list of tokens in assembly with every statement ending in an EOL token.</returns>
    internal static Token[] Tokenize(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is null or whitespace only. Cannot tokenize.");
        string[] assemblyLines = code.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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
    // Scary regex: Matches some symbols (brackets, square brackets, arithmetic), single words, or words enclosed in quotes,
    // while keeping enquoted strings intact including the quotes themselves.
    // Useful site: https://regex101.com/ (set to .NET flavor)
    private const string pattern = @"\[|\]|\(|\)|\+|\-|\*|""[^""]+""|[^""\s\[\]\(\)""]+";
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
        else if (tokenString == KEYWORD_ADDRESSING_ABSOLUTE)
            return new Token(TokenType.KEYWORD_ADDRESSING_ABSOLUTE, KEYWORD_ADDRESSING_ABSOLUTE);
        else if (tokenString == KEYWORD_ADDRESSING_RELATIVE)
            return new Token(TokenType.KEYWORD_ADDRESSING_RELATIVE, KEYWORD_ADDRESSING_RELATIVE);
        // "Normal" tokens
        // Single letters
        else if (tokenString == "(" || tokenString == ")")
            return new Token(TokenType.BRACKET, tokenString);
        else if (tokenString == "[" || tokenString == "]")
            return new Token(TokenType.SQUARE_BRACKET, tokenString);
        else if (tokenString == "+" || tokenString == "-" || tokenString == "*")
            return new Token(TokenType.SYMBOL_ARITHMETIC_OP, tokenString);
        else if (tokenString == "=")
            return new Token(TokenType.SYMBOL_EQUALS, "=");
        // Check if parseable as mnemonic
        else if (InstructionLookup.IsValidMnemonic(tokenString))
            return new Token(TokenType.MNEMONIC, tokenString.ToUpper());
        // Check if parseable as register
        else if (Instructions.AddressableRegisterCode.IsValidRegisterName(tokenString.ToUpper()))
            return new Token(TokenType.REGISTER, tokenString.ToUpper());
        // Check if parseable as uint
        else if (TryConvertStringToUInt(tokenString, out _))
            return new Token(TokenType.NUMERIC_VALUE, tokenString);
        // Check if word is followed by colon (only exception would be "len:", which would be filtered earlier)
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
        KEYWORD_ADDRESSING_ABSOLUTE,
        KEYWORD_ADDRESSING_RELATIVE,
        BRACKET, // Either open bracket or close bracket
        SQUARE_BRACKET, // Either open square bracket or close square bracket
        SYMBOL_ARITHMETIC_OP, // Arithmetic operator symbol (e.g. +, -, *)
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
            if (statementTokens.Length <= 0)
                throw new ArgumentException("Cannot create statement from empty token array or without finishing in EOS token.");
            if (statementTokens[^1].Type != TokenType.EOS)
            {
                Logging.LogWarn($"Creating statement from possibly unfinished token sequence (without EOS). Appending EOS.");
                statementTokens = [.. statementTokens, Token.CreateEOS()];
            }
            Tokens = statementTokens;
        }
        public override string ToString() => string.Join(" ", Tokens.SkipLast(1).Select(token => token.Value));
    }
}