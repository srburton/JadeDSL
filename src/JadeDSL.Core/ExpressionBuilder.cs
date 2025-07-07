using JadeDSL.Core.Types;
using JadeDSL.Core.Extensions;
using System.Linq.Expressions;

namespace JadeDSL.Core
{
    /// <summary>
    /// Builds LINQ expressions dynamically based on DSL-style filter nodes.
    /// </summary>
    /// <remarks>
    /// This class is internal and intended to be used by the filter parser engine.
    /// </remarks>
    public static class ExpressionBuilder
    {
        /// <summary>
        /// Builds a predicate expression from a DSL node tree.
        /// </summary>
        /// <typeparam name="T">The root entity type.</typeparam>
        /// <param name="node">The root node of the expression tree.</param>
        /// <returns>A compiled lambda expression.</returns>
        public static Expression<Func<T, bool>> BuildPredicate<T>(Node node)
        {
            var parameter = Expression.Parameter(typeof(T), "e");
            var body = BuildExpressionBody<T>(node, parameter);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        private static Expression BuildExpressionBody<T>(Node node, ParameterExpression param)
        {
            return node switch
            {
                NodeGroup group => CombineGroup<T>(group, param),
                NodeExpression expr => BuildConditionExpression<T>(param, expr),
                _ => throw new NotSupportedException("Unknown node type")
            };
        }

        private static Expression CombineGroup<T>(NodeGroup group, ParameterExpression param)
        {
            var exprNodes = group.Children.OfType<NodeExpression>().ToList();
            var otherNodes = group.Children.Except(exprNodes).ToList();

            var expressions = new List<Expression>();

            // Group expressions by their root path (e.g. "attributes")
            var grouped = exprNodes.GroupBy(e => e.Field.Split('.')[0]);
            foreach (var g in grouped)
                expressions.Add(BuildGroupedAny<T>(param, g, group.Operator));

            // Recurse into nested group nodes
            foreach (var node in otherNodes)
                expressions.Add(BuildExpressionBody<T>(node, param));

            return group.Operator switch
            {
                LogicalOperatorType.And => expressions.Aggregate(Expression.AndAlso),
                LogicalOperatorType.Or => expressions.Aggregate(Expression.OrElse),
                _ => throw new NotSupportedException("Unknown logical operator")
            };
        }

        private static Expression BuildConditionExpression<T>(ParameterExpression param, NodeExpression expr)
        {
            var pathParts = expr.Field.Split('.');

            if (pathParts.Length == 1)
            {
                var member = Expression.PropertyOrField(param, ConvertToPascalCase(expr.Field));
                var constant = ExpressionUtility.ParseType(member.Type, expr.Value);
                return BuildComparison(expr.Operator, member, constant);
            }

            return BuildGroupedAny<T>(param, new[] { expr }, LogicalOperatorType.And);
        }

        private static Expression BuildGroupedAny<T>(ParameterExpression param, IEnumerable<NodeExpression> group, LogicalOperatorType logical)
        {
            var pathParts = group.First().Field.Split('.');
            var collectionName = ConvertToPascalCase(pathParts[0]);
            var collectionProp = Expression.Property(param, collectionName);

            var itemType = collectionProp.Type.GetGenericArguments().First();
            var itemParam = Expression.Parameter(itemType, "x");

            var inner = group.Select(expr =>
            {
                var subPath = expr.Field.Split('.').Skip(1).ToArray();
                return BuildConditionExpressionForPath(itemParam, subPath, expr.Operator, expr.Value, 0);
            });

            var combined = logical switch
            {
                LogicalOperatorType.And => inner.Aggregate(Expression.AndAlso),
                LogicalOperatorType.Or => inner.Aggregate(Expression.OrElse),
                _ => throw new NotSupportedException("Invalid logical operator")
            };

            var lambda = Expression.Lambda(combined, itemParam);

            return Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Any),
                new[] { itemType },
                collectionProp,
                lambda);
        }

        private static Expression BuildConditionExpressionForPath(Expression param, string[] path, Symbol op, string value, int index)
        {
            var member = Expression.PropertyOrField(param, ConvertToPascalCase(path[index]));

            if (index == path.Length - 1)
            {
                var constant = ExpressionUtility.ParseType(member.Type, value);
                return BuildComparison(op, member, constant);
            }

            return BuildConditionExpressionForPath(member, path, op, value, index + 1);
        }

        private static Expression BuildComparison(Symbol op, Expression left, Expression right)
        {
            if (left.Type != right.Type)
            {
                if (Nullable.GetUnderlyingType(left.Type) == right.Type)
                    right = Expression.Convert(right, left.Type);
                else if (Nullable.GetUnderlyingType(right.Type) == left.Type)
                    left = Expression.Convert(left, right.Type);
            }

            return op switch
            {
                var o when o == Symbols.Colon || o == Symbols.Equal => Expression.Equal(left, right),
                var o when o == Symbols.NotEqual => Expression.NotEqual(left, right),
                var o when o == Symbols.GreaterThan => Expression.GreaterThan(left, right),
                var o when o == Symbols.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
                var o when o == Symbols.LessThan => Expression.LessThan(left, right),
                var o when o == Symbols.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
                var o when o == Symbols.Like => ExpressionUtility.Like(left, right),
                var o when o == Symbols.Between => ExpressionUtility.Between(left, right),
                _ => throw new NotSupportedException($"Operator {op} not supported.")
            };
        }

        private static string ConvertToPascalCase(string input)
        {
            return string.Join("", input.Split('_').Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1)));
        }
    }
}
