using JadeDSL.Core.Types;

namespace JadeDSL.Core
{
    public class NodeGroup : Node
    {
        public LogicalOperatorType Operator { get; set; }

        public List<Node> Children { get; set; } = [];
    }
}
