using JadeDSL.Core.Types;

namespace JadeDSL.Core
{
    /// <summary>
    /// Responsible for parsing tokenized input into an abstract syntax tree (AST) used for filtering operations.
    /// </summary>
    public class Parser
    {
        private int position;
        private readonly Options options;        
        
        private List<Token> tokens = [];

        public static Node? Empty => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class with the provided options.
        /// </summary>
        /// <param name="options">The parser configuration options.</param>
        public Parser(Options options)
        {
            this.options = options;
        }     

        /// <summary>
        /// Parses a list of tokens into a filter expression tree (AST).
        /// </summary>
        /// <param name="tokens">The token list to parse.</param>
        /// <returns>The root <see cref="Node"/> of the resulting abstract syntax tree.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input token list is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the number of tokens exceeds allowed limits.</exception>
        public Node Parse(List<Token> tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));

            if (tokens.Count > options.MaxNodeCount)
                throw new InvalidOperationException($"Token count exceeds limit ({options.MaxNodeCount})");

            this.tokens = tokens;
            
            position = 0;

            return ParseExpression();
        }

        /// <summary>
        /// Parses OR expressions (|) recursively.
        /// </summary>
        private Node ParseExpression()
        {
            var left = ParseTerm();

            while (Match(TokenType.Or))
            {
                Consume(TokenType.Or);
                var right = ParseTerm();

                left = new NodeGroup
                {
                    Operator = LogicalOperatorType.Or,
                    Children = new List<Node> { left, right }
                };
            }

            return left;
        }

        /// <summary>
        /// Parses AND expressions (&) recursively.
        /// </summary>
        private Node ParseTerm()
        {
            var left = ParseFactor();

            while (Match(TokenType.And))
            {
                Consume(TokenType.And);
                var right = ParseFactor();

                left = new NodeGroup
                {
                    Operator = LogicalOperatorType.And,
                    Children = new List<Node> { left, right }
                };
            }

            return left;
        }

        /// <summary>
        /// Parses either grouped expressions with parentheses or leaf filter expressions.
        /// </summary>
        private Node ParseFactor()
        {
            if (Match(TokenType.LeftParen))
            {
                Consume(TokenType.LeftParen);
                var expr = ParseExpression();
                Consume(TokenType.RightParen);
                return expr;
            }

            return ParseExpressionNode();
        }

        /// <summary>
        /// Parses a single filter expression like "field:operator:value".
        /// </summary>
        private Node ParseExpressionNode()
        {
            if (!Match(TokenType.Expression))
                throw new InvalidOperationException("Expected expression");

            var token = Consume(TokenType.Expression);
            return ParseFilterExpression(token.Value);
        }

        /// <summary>
        /// Converts a filter expression string into a <see cref="NodeExpression"/>.
        /// </summary>
        /// <param name="expr">The raw expression string.</param>
        /// <returns>A validated <see cref="NodeExpression"/> object.</returns>
        /// <exception cref="InvalidOperationException">Thrown for invalid or malformed expressions.</exception>
        private Node ParseFilterExpression(string expr)
        {
            Symbol op = Symbols.Equal;
            string name = "";
            string value = "";

            foreach (var symbol in Symbols.OperatorSymbols.OrderByDescending(s => s.ToString().Length))
            {
                var symbolStr = symbol.ToString();
                var opIndex = expr.IndexOf(symbolStr, StringComparison.Ordinal);

                if (opIndex > 0)
                {
                    op = symbol;
                    name = expr.Substring(0, opIndex);
                    value = expr.Substring(opIndex + symbolStr.Length);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"Invalid expression '{expr}'");

            value = value.Trim().Trim('"');

            var nodeExpression = new NodeExpression
            {
                Field = name,
                Operator = op,
                Value = value
            };

            if (!nodeExpression.IsValid)
                throw new InvalidOperationException($"Invalid filter expression: {expr}");

            if(options.AllowedFields.Contains(nodeExpression.Field) == false)
                throw new InvalidOperationException($"Field '{nodeExpression.Field}' is not allowed in this context.");

            if(options.AllowedSymbols.Contains(nodeExpression.Operator) == false)
                throw new InvalidOperationException($"Operator '{nodeExpression.Operator}' is not allowed in this context.");

            return nodeExpression;
        }

        /// <summary>
        /// Checks whether the current token matches the expected type.
        /// </summary>
        private bool Match(TokenType type)
        {
            return position < tokens.Count && tokens[position].Type == type;
        }

        /// <summary>
        /// Consumes the current token if it matches the expected type, otherwise throws.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the expected token type is not matched.</exception>
        private Token Consume(TokenType type)
        {
            if (!Match(type))
                throw new InvalidOperationException($"Expected token {type} but found {(position < tokens.Count ? tokens[position].Type.ToString() : "EOF")}");

            return tokens[position++];
        }
    }
}
