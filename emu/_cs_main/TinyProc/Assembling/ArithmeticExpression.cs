using System.Numerics;
using TinyProc.Application;

namespace TinyProc.Assembling;

public partial class Assembler
{
    /// <summary>
    /// Describes an arithmetic expression, which is an abstract representation of a sequence
    /// of mathematical operations done on numeric values.<br></br>
    /// An illustrative example would be<br></br>
    /// "400 * 2 + 99"
    /// </summary>
    /// <typeparam name="TOperand">The type of the numbers to operate on (e.g. Int32)</typeparam>
    public class ArithmeticExpression<TOperand> where TOperand : IBinaryInteger<TOperand>
    {
        private readonly Token[] expressionTokens;
        /// <summary>
        /// true if the evaluation had an overflow, false otherwise.
        /// Must run Evaluate() at least once to set.
        /// </summary>
        public bool HasOverflown { get; private set; } = false;

        /// <summary>
        /// Creates a new arithmetic expression from the specified string.
        /// </summary>
        /// <param name="expression"></param>
        public ArithmeticExpression(string expression) : this(Tokenize(expression)[..^1])
        { /* Everything is handled already by the other constructor. */ }
        /// <summary>
        /// Creates a new arithmetic expression from a statement.
        /// </summary>
        /// <param name="expressionStatement"></param>
        internal ArithmeticExpression(Statement expressionStatement) : this(expressionStatement.Tokens[..^1])
        { /* Everything is handled already by the other constructor. */ }
        /// <summary>
        /// Creates a new arithmetic expression from the specified token array.
        /// </summary>
        /// <param name="expressionTokens"></param>
        /// <exception cref="ArgumentException"></exception>
        internal ArithmeticExpression(Token[] expressionTokens)
        {
            if (expressionTokens.Any(token => token.Type == TokenType.BRACKET))
                Logging.LogWarn($"Warning: Will ignore brackets in arithmetic expression evaluation.");
            expressionTokens = [.. expressionTokens.Where(token => token.Type != TokenType.BRACKET)];
            if (expressionTokens.Any(token => token.Type != TokenType.NUMERIC_VALUE && token.Type != TokenType.SYMBOL_ARITHMETIC_OP))
                throw new ArgumentException($"Arithmetic expression {new Statement(expressionTokens)} has invalid symbols.");
            this.expressionTokens = [.. expressionTokens];
        }

        /// <summary>
        /// Evaluates the value of the arithmetic expression.<br></br>
        /// Example:<br></br>
        /// string "400 * 2 + 99"<br></br>
        /// reduces to<br></br>
        /// TResult 899<br></br>
        /// <b>Important note: This evaluator strictly evaluates from left to right.
        /// There is no preference for the order of operations! This also includes parenthesis,
        /// which are stripped away before evaluation.</b>
        /// </summary>
        /// <returns></returns>
        public TOperand Evaluate(bool throwExceptionOnOverflow = false)
        {
            // For the sake of simplicity, no AST is built and the entire expression is evaluated
            // strictly from left to right.
            // TODO: Might implement ASTs later

            // Note: At this point, this method expects the expression in expressionTokens to be valid,
            // i.e. no specific syntax error checking is done beyond this point.

            TOperand result = TOperand.Parse(expressionTokens[0].Value, null);
            
            for (int tokenIdx = 1; tokenIdx < expressionTokens.Length - 1; tokenIdx += 2)
            {
                Token previous = expressionTokens[tokenIdx - 1];
                Token current = expressionTokens[tokenIdx];
                Token next = expressionTokens[tokenIdx + 1];

                if (previous.Type != TokenType.NUMERIC_VALUE ||
                    current.Type != TokenType.SYMBOL_ARITHMETIC_OP ||
                    next.Type != TokenType.NUMERIC_VALUE)
                    throw new Exception($"Arithmetic expression has invalid structure: {new Statement(expressionTokens)} at index {tokenIdx}");

                if (!TOperand.TryParse(next.Value, null, out TOperand? nextValue))
                    throw new Exception($"Unable to parse numeric right token in arithmetic expression: {next.Value}");
                result = EvaluateSingleExpressionChecked(result, current, nextValue, throwExceptionOnOverflow: throwExceptionOnOverflow);
            }
            return result;
        }
        
        /// <summary>
        /// Evaluates the expression, but also sets the HasOverflown flag if necessary.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="op"></param>
        /// <param name="right"></param>
        /// <param name="throwExceptionOnOverflow"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private TOperand EvaluateSingleExpressionChecked(TOperand left, Token op, TOperand right, bool throwExceptionOnOverflow = false)
        {
            // Not the most beautiful code but eh, it works
            if (op.Type != TokenType.SYMBOL_ARITHMETIC_OP)
                throw new Exception($"Invalid operator {op} in single expression.");
            try
            {
                return EvaluateSingleExpression(left, op, right, @checked: true);
            }
            catch (OverflowException)
            {
                HasOverflown = true;
                if (throwExceptionOnOverflow)
                    throw;
                return EvaluateSingleExpression(left, op, right, @checked: false);
            }
        }
        private static TOperand EvaluateSingleExpression(TOperand left, Token op, TOperand right, bool @checked)
        {
            // Checked statements are a little of a nightmare to work with:
            // They only, and I state literally ONLY throw an exception, if there is literal direct code
            // involving an arithmetic operation inside the statement causing the overflow.
            // This means that checked { Func(); } will not throw an OverflowException, even if Func()
            // internally has an overflow.
            // If that wasn't a problem, here is an even bigger one arising from the previous:
            // Since this class operates on the INumber<T> interface instead of working with the number
            // primitives directly, this means the arithmetic operators of numbers are most definitely overridden.
            // Consequently, "n1 + n2" is just syntactic sugar for something akin to "op_Add(n1, n2)"
            // And guess what: checked(n1 + n2) won't work this way, since the plus operator just hides
            // the function it replaces and suddenly no checks are done again.
            // The solution?
            // F*ck the checked / unchecked syntax altogether and create separate functions to manually see
            // if an overflow occurred, by doing the operation twice, on the number type and on BigInteger.
            // If they both differ, an overflow must've occurred.
            // Note to self: TOperand.CreateChecked did, in fact, not check.
            //               Both TOperand.CreateChecked(left) op TOperand.CreateChecked(right)
            //               and TOperand.CreateChecked(left op right) failed.
            // Call it a skill issue or whatever. I'm done with this.
            // Update:
            // It seems I am stupid and had wrong results all over for writing
            // TOperand result = TOperand.Zero;
            // instead of
            // TOperand result = TOperand.Parse(expressionTokens[0].Value, null);
            // above in Evaluate() which led me to believe overflows weren't being detected, while
            // the values it was working on actually just never overflowed in the first place.
            // In my defense, ChatGPT was gaslighting me into thinking the former (see above) was the case.
            // I will leave this here for your satisfaction and my own "documentation".
            // Hours wasted on this: ~4.0
            return op.Value switch
            {
                "+" => @checked ? checked(left + right) : unchecked(left + right),
                "-" => @checked ? checked(left - right) : unchecked(left - right),
                "*" => @checked ? checked(left * right) : unchecked(left * right),
                // Theoretically, this code is not reachable, since all possible operators for token type SYMBOL_ARITHMETIC_OP
                // are handled above.
                _ => throw new Exception($"Invalid operator {op.Value} in single expression. (This code shouldn't be reachable)")
            };
        }

        // Fallback functions in case checked / unchecked does some nasty stuff again.
        // Read comment above in EvaluateSingleExpression()
        private static TOperand CheckedAdd(TOperand n1, TOperand n2)
        {
            TOperand res = n1 + n2;
            BigInteger resBig = BigInteger.CreateChecked(n1) + BigInteger.CreateChecked(n2);
            return BigInteger.CreateChecked(res) != resBig ? throw new OverflowException() : res;
        }
        private static TOperand UncheckedAdd(TOperand n1, TOperand n2)
        {
            // Just assume the context is globally unchecked
            return n1 + n2;
        }
        private static TOperand CheckedSubtract(TOperand n1, TOperand n2)
        {
            TOperand res = n1 - n2;
            BigInteger resBig = BigInteger.CreateChecked(n1) - BigInteger.CreateChecked(n2);
            return BigInteger.CreateChecked(res) != resBig ? throw new OverflowException() : res;
        }
        private static TOperand UncheckedSubtract(TOperand n1, TOperand n2)
        {
            // Just assume the context is globally unchecked
            return n1 - n2;
        }
        private static TOperand CheckedMultiply(TOperand n1, TOperand n2)
        {
            TOperand res = n1 * n2;
            BigInteger resBig = BigInteger.CreateChecked(n1) * BigInteger.CreateChecked(n2);
            return BigInteger.CreateChecked(res) != resBig ? throw new OverflowException() : res;
        }
        private static TOperand UncheckedMultiply(TOperand n1, TOperand n2)
        {
            // Just assume the context is globally unchecked
            return n1 * n2;
        }
    }
}