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

        /// <summary>
        /// Combines a group of nodes (filters) into a single expression using AND/OR logic.
        /// Groups expressions with the same root collection property into a single `.Any()` call.
        /// </summary>
        /// <typeparam name="T">Type of the root entity.</typeparam>
        /// <param name="group">The group node containing child expressions.</param>
        /// <param name="param">The parameter expression representing the root entity.</param>
        /// <returns>An expression representing the combined filter logic.</returns>
        private static Expression CombineGroup<T>(NodeGroup group, ParameterExpression param)
        {
            // Separate simple expressions (NodeExpression) and nested groups (NodeGroup)
            var exprNodes = group.Children.OfType<NodeExpression>().ToList();
            var otherNodes = group.Children.Except(exprNodes).ToList();

            var expressions = new List<Expression>();

            // Group NodeExpressions by their root property (prefix before the first dot)
            var groupedByRoot = exprNodes.GroupBy(expr =>
            {
                var path = expr.Field.Split('.');
                return path[0];
            });

            foreach (var rootGroup in groupedByRoot)
            {
                var firstExpr = rootGroup.First();
                var path = firstExpr.Field.Split('.');
                var rootProp = ConvertToPascalCase(path[0]);
                var member = Expression.Property(param, rootProp);

                // Check if the root property is a collection (implements IEnumerable<T>)
                bool isCollection = member.Type.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                if (path.Length > 1 && isCollection)
                {
                    // If it's a collection with nested properties, build a single .Any() for all expressions in this group
                    expressions.Add(BuildGroupedAny<T>(param, rootGroup, group.Operator));
                }
                else
                {
                    // For non-collection or simple properties, build expressions individually
                    foreach (var expr in rootGroup)
                    {
                        expressions.Add(BuildConditionExpression<T>(param, expr));
                    }
                }
            }

            // Recursively process nested groups
            foreach (var node in otherNodes)
            {
                expressions.Add(BuildExpressionBody<T>(node, param));
            }

            // Combine all expressions with the group's logical operator (AND / OR)
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

                if (expr.Operator == Symbols.Between)
                    return ExpressionUtility.Between(member, Expression.Constant(expr.Value));

                if (expr.Operator == Symbols.Like || expr.Operator == Symbols.LikeBoth)
                    return ExpressionUtility.Like(member, Expression.Constant(expr.Value), expr.Operator);

                var constant = ExpressionUtility.ParseType(member.Type, expr.Value);

                return BuildComparison(expr.Operator, member, constant);
            }

            return BuildGroupedAny<T>(param, [expr], LogicalOperatorType.And);
        }

        private static Expression BuildGroupedAny<T>(ParameterExpression param, IEnumerable<NodeExpression> group, LogicalOperatorType logical)
        {
            if (!group.Any())
                throw new InvalidOperationException("No expressions to group.");

            var pathParts = group.First().Field.Split('.');
            var collectionName = ConvertToPascalCase(pathParts[0]);
            var collectionProp = Expression.Property(param, collectionName);
            var collectionType = collectionProp.Type;

            var enumerableInterface = collectionType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface == null)
                throw new InvalidOperationException($"Property '{collectionName}' must be a generic collection (IEnumerable<T>).");

            var itemType = enumerableInterface.GetGenericArguments().First();
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

            return Expression.AndAlso(
                Expression.NotEqual(collectionProp, Expression.Constant(null, collectionProp.Type)),
                Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Any),
                    [itemType],
                    collectionProp,
                    lambda
                )
            );
        }


        private static Expression BuildConditionExpressionForPath(Expression param, string[] path, Symbol op, string value, int index)
        {
            var member = Expression.PropertyOrField(param, ConvertToPascalCase(path[index]));

            if (index == path.Length - 1)
            {
                if (op == Symbols.Between)
                    return ExpressionUtility.Between(member, Expression.Constant(value));

                if (op == Symbols.Like || op == Symbols.LikeBoth)
                    return ExpressionUtility.Like(member, Expression.Constant(value), op);

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
                var o when o == Symbols.Like => ExpressionUtility.Like(left, right, op),
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
