using JadeDSL.Core;
using JadeDSL.Interfaces;

namespace JadeDSL
{
    public class Filter : IFilter
    {
        public Node? Node { get; }
        public Options Options { get; }

        internal Filter(Node? node, Options options)
        {
            Node = node;
            Options = options;
        }
    }
}