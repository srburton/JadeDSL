using JadeDSL.Core;
using JadeDSL.Core.Types;

public static class TokenExtensions
{
    public static List<Token> ResolveAliases(this IEnumerable<Token> tokens, Options options)
    {
        if (options.FieldAliases == null || options.FieldAliases.Count == 0)
            return [.. tokens];

        var result = new List<Token>();

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.Expression && token.Value is string val)
            {
                var alias = options.FieldAliases.Keys
                    .FirstOrDefault(a => val.StartsWith(a, StringComparison.OrdinalIgnoreCase));

                if (alias != null)
                {
                    var replacement = options.ResolveAlias(alias);
                    var newVal = replacement + val.Substring(alias.Length);

                    result.Add(new Token { Type = token.Type, Value = newVal });
                    continue;
                }
            }

            result.Add(token);
        }

        return result;
    }
}
