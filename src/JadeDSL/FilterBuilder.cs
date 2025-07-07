using JadeDSL.Core;
using JadeDSL.Interfaces;

public class FilterBuilder : IFilter
{
    public Node? Node { get; private set; }

    public Options Options { get; private set; } = new Options();

    private string _expression = "";

    public FilterBuilder WithExpression(string expr)
    {
        _expression = expr;
        return this;
    }

    public FilterBuilder ConfigureOptions(Action<Options> configure)
    {
        configure?.Invoke(Options);
        return this;
    }

    public FilterBuilder Build()
    {
        if (string.IsNullOrEmpty(_expression))
        {
            if (Options.Required)
                throw new ArgumentException("Expression is required.");

            Node = null;
            return this;
        }
        
        var tokens = Tokenizer.Tokenize(_expression)
                              .ResolveAliases(Options);

        var parser = new Parser(Options);
        
        Node = parser.Parse(tokens);

        return this;
    }

    public IEnumerable<string> GetValuesFor(string field)
    {
        if (Node is null)
            return [];

        return ExtractValues(Node, field);
    }

    private static IEnumerable<string> ExtractValues(Node node, string field)
    {
        if (node is NodeExpression expr && expr.Field == field)
        {
            yield return expr.Value;
        }
        else if (node is NodeGroup group)
        {
            foreach (var child in group.Children)
            {
                foreach (var val in ExtractValues(child, field))
                    yield return val;
            }
        }
    }
}