using JadeDSL.Core;
using JadeDSL.Interfaces;

namespace JadeDSL
{
    public class FilterBuilder : IFilter
    {
        public Node? Node { get; }

        private readonly Options options = new();

        public FilterBuilder(string expression, Action<Options>? action = null)
        {
            action?.Invoke(options);

            if (string.IsNullOrEmpty(expression) && !options.Required)
            {
                Node = Parser.Empty;
                return;
            }

            var parser = new Parser(options);
            var tokens = Tokenizer.Tokenize(expression);

            Node = parser.Parse(tokens);
        }        
    }
}