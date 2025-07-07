using JadeDSL.Core;
using JadeDSL.Core.Types;
using System.Text;

public class Tokenizer
{
    public static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        var buffer = new StringBuilder();
        bool inQuotes = false;
        int parenBalance = 0;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '"')
            {
                buffer.Append(c);
                inQuotes = !inQuotes;
                continue;
            }

            if (inQuotes)
            {
                buffer.Append(c);
                continue;
            }

            if (c == '(' || c == ')' || c == '&' || c == '|')
            {
                FlushBuffer(tokens, buffer);

                if (c == '(') parenBalance++;
                if (c == ')') parenBalance--;

                if (parenBalance < 0)
                    throw new InvalidOperationException($"Unexpected closing parenthesis at position {i}");

                tokens.Add(new Token
                {
                    Type = c switch
                    {
                        '(' => TokenType.LeftParen,
                        ')' => TokenType.RightParen,
                        '&' => TokenType.And,
                        '|' => TokenType.Or,
                        _ => throw new InvalidOperationException()
                    }
                });
            }
            else if (!char.IsWhiteSpace(c))
            {
                buffer.Append(c);
            }
            else
            {
                FlushBuffer(tokens, buffer);
            }
        }

        FlushBuffer(tokens, buffer);

        if (inQuotes)
            throw new InvalidOperationException("Unclosed quotes in expression.");

        if (parenBalance != 0)
            throw new InvalidOperationException($"Mismatched parentheses in expression. Open: {parenBalance}");

        return tokens;
    }

    private static void FlushBuffer(List<Token> tokens, StringBuilder buffer)
    {
        if (buffer.Length > 0)
        {
            tokens.Add(new Token
            {
                Type = TokenType.Expression,
                Value = buffer.ToString()
            });
            buffer.Clear();
        }
    }
}
