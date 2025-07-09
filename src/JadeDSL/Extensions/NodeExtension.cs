using JadeDSL.Core;
using JadeDSL.Core.Types;

namespace JadeDSL.Extensions
{
    public static class NodeExtension
    {
        public static string ToDslString(this Node node, LogicalOperatorType? parentOp = null, bool isRoot = true)
        {
            if (node is NodeGroup group)
            {
                var op = group.Operator == LogicalOperatorType.And ? "&" : "|";
                var children = group.Children
                    .Select(child => child.ToDslString(group.Operator, false))
                    .ToList();

                var joined = string.Join(op, children);

                if (!isRoot && group.Children.Count > 1 && parentOp != null && group.Operator != parentOp & !children.All(x => x.StartsWith("(") && x.EndsWith(")")))
                    return $"({joined})";
                else
                    return joined;
            }
            else if (node is NodeExpression expr)
            {
                bool isRange = expr.Operator == Symbols.Between && expr.Value.Contains(Symbols.Range);
                string valueStr;
                if (isRange)
                    valueStr = expr.Value;
                else if (!expr.Value.NeedsQuotes())
                    valueStr = expr.Value;
                else
                    valueStr = $"\"{expr.Value}\"";
                return $"{expr.Field}{expr.Operator}{valueStr}";
            }
            throw new NotSupportedException();
        }

        private static bool NeedsQuotes(this string value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            if (value.StartsWith(Symbols.DoubleQuote) && value.EndsWith(Symbols.DoubleQuote)) return false;
            if (double.TryParse(value, out _)) return false;
            if (value.Contains(Symbols.Range)) return false;
            return true;
        }
    }
}