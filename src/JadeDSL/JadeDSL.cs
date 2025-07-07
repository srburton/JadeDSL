using JadeDSL.Core;
using JadeDSL.Interfaces;

namespace JadeDSL
{
    public class JadeDSL: IFilter
    {
        public Node Node { get; }

        private readonly Options options = new();
        
        public JadeDSL(string expression, Action<Options>? action = null)
        {
            action?.Invoke(options);

            var parser = new Parser(options);
           
            Node = parser.Parse(Tokenizer.Tokenize(expression));
        }
    }
}
