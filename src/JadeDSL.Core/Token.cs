using JadeDSL.Core.Types;

namespace JadeDSL.Core
{
    public struct Token
    {
        public TokenType Type { get; set; }

        public string Value { get; set; }

        public override readonly string ToString()
        {
            return Type switch
            {
                TokenType.Expression => $"[Expr: {Value}]",
                TokenType.LeftParen => "(",
                TokenType.RightParen => ")",
                TokenType.And => "&",
                TokenType.Or => "|",
                _ => Value
            };
        }
    }
}
