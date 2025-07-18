using JadeDSL.Core.Types;
using JadeDSL.Core.Extensions;
using System.Linq.Expressions;

namespace JadeDSL.Core
{
    /// <summary>
    /// Dynamically builds LINQ expressions from a tree of DSL filter nodes.
    /// </summary>
    /// <remarks>
    /// This static class is intended to be used internally by the filtering engine to translate parsed filter nodes
    /// into executable expression trees.
    /// </remarks>
    public static class ExpressionBuilder
    {
        /// <summary>
        /// Constructs a LINQ predicate expression from a filter node tree.
        /// </summary>
        /// <typeparam name="T">Type of the root entity.</typeparam>
        /// <param name="node">Root filter node.</param>
        /// <returns>Lambda expression representing the filter predicate.</returns>
        public static Expression<Func<T, bool>> BuildPredicate<T>(Node node)
        {
            var parameter = Expression.Parameter(typeof(T), "e");
            var body = BuildExpressionBody<T>(node, parameter);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        /// <summary>
        /// Recursively builds an expression from a node (group or expression).
        /// </summary>
        private static Expression BuildExpressionBody<T>(Node node, ParameterExpression param)
        {
            return node switch
            {
                NodeGroup group => CombineGroup<T>(group, param),
                NodeExpression expr => BuildConditionExpression<T>(param, expr),
                _ => throw new NotSupportedException("Unknown node type encountered.")
            };
        }

        /// <summary>
        /// Combines multiple child expressions of a group using AND/OR logic.
        /// Groups expressions sharing the same root collection property into a single `.Any()` call for efficiency.
        /// </summary>
        private static Expression CombineGroup<T>(NodeGroup group, ParameterExpression param)
        {
            // Separate simple filter expressions and nested groups
            var simpleExpressions = group.Children.OfType<NodeExpression>().ToList();
            var nestedGroups = group.Children.Except(simpleExpressions).ToList();

            var expressions = new List<Expression>();

            // Group simple expressions by their root property name (before first dot)
            var groupedByRoot = simpleExpressions.GroupBy(expr => expr.Field.Split('.')[0]);

            foreach (var rootGroup in groupedByRoot)
            {
                var firstExpr = rootGroup.First();
                var pathParts = firstExpr.Field.Split('.');
                var rootPropName = ConvertToPascalCase(pathParts[0]);
                var rootProperty = Expression.Property(param, rootPropName);

                // Determine if root property is a collection (IEnumerable<T>)
                bool isCollection = rootProperty.Type.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                if (pathParts.Length > 1 && isCollection)
                {
                    // Build a single .Any() expression combining all filters on this collection
                    expressions.Add(BuildGroupedAny<T>(param, rootGroup, group.Operator));
                }
                else
                {
                    // Build individual expressions for non-collection properties or simple paths
                    foreach (var expr in rootGroup)
                    {
                        expressions.Add(BuildConditionExpression<T>(param, expr));
                    }
                }
            }

            // Recursively process nested groups
            foreach (var node in nestedGroups)
            {
                expressions.Add(BuildExpressionBody<T>(node, param));
            }

            // Combine all expressions using group's logical operator (AND / OR)
            return group.Operator switch
            {
                LogicalOperatorType.And => expressions.Aggregate(Expression.AndAlso),
                LogicalOperatorType.Or => expressions.Aggregate(Expression.OrElse),
                _ => throw new NotSupportedException("Unknown logical operator.")
            };
        }

        /// <summary>
        /// Builds a condition expression for a single filter node.
        /// Handles nested properties and collections.
        /// </summary>
        private static Expression BuildConditionExpression<T>(ParameterExpression param, NodeExpression expr)
        {
            var pathParts = expr.Field.Split('.');
            if (pathParts.Length == 1)
            {
                // properties simple mapped to the parameter
                var member = Expression.PropertyOrField(param, ConvertToPascalCase(expr.Field));
                if (expr.Operator == Symbols.Between)
                    return ExpressionUtility.Between(member, Expression.Constant(expr.Value));
                if (expr.Operator == Symbols.Like || expr.Operator == Symbols.LikeBoth)
                    return ExpressionUtility.Like(member, Expression.Constant(expr.Value), expr.Operator);
                var constant = ExpressionUtility.ParseType(member.Type, expr.Value);
                return BuildComparison(expr.Operator, member, constant);
            }

            // Nested recursive
            return BuildConditionExpressionForPath(param, pathParts, expr.Operator, expr.Value, 0);
        }

        /// <summary>
        /// Builds an expression that calls .Any() on a collection property,
        /// combining multiple filter expressions with logical AND or OR inside the Any() lambda.
        /// </summary>
        private static Expression BuildGroupedAny<T>(
            ParameterExpression rootParam,
            IEnumerable<NodeExpression> expressions,
            LogicalOperatorType logicalOperator)
        {
            if (!expressions.Any())
                throw new InvalidOperationException("No expressions provided for grouped .Any() construction.");

            var firstExpr = expressions.First();
            var pathParts = firstExpr.Field.Split('.');
            var collectionName = ConvertToPascalCase(pathParts[0]);
            var collectionProperty = Expression.Property(rootParam, collectionName);
            var collectionType = collectionProperty.Type;

            // Find the generic IEnumerable<T> interface to determine element type
            var enumerableInterface = collectionType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface == null)
                throw new InvalidOperationException($"Property '{collectionName}' must be a generic collection (IEnumerable<T>).");

            var elementType = enumerableInterface.GetGenericArguments()[0];
            var elementParam = Expression.Parameter(elementType, "x");

            // Build individual expressions for the inner properties starting after the collection root
            var innerExpressions = expressions.Select(expr =>
            {
                var subPath = expr.Field.Split('.').Skip(1).ToArray();
                return BuildConditionExpressionForPath(elementParam, subPath, expr.Operator, expr.Value, 0);
            });

            // Combine inner expressions with specified logical operator
            var combinedInner = logicalOperator switch
            {
                LogicalOperatorType.And => innerExpressions.Aggregate(Expression.AndAlso),
                LogicalOperatorType.Or => innerExpressions.Aggregate(Expression.OrElse),
                _ => throw new NotSupportedException("Invalid logical operator in grouped .Any().")
            };

            var lambda = Expression.Lambda(combinedInner, elementParam);

            // Protect against null collection and build the .Any() call expression
            return Expression.AndAlso(
                Expression.NotEqual(collectionProperty, Expression.Constant(null, collectionProperty.Type)),
                Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Any),
                    [elementType],
                    collectionProperty,
                    lambda
                )
            );
        }

        /// <summary>
        /// Recursively builds condition expressions for nested paths, with null-checks.
        /// </summary>
        private static Expression BuildConditionExpressionForPath(
            Expression param,
            string[] path,
            Symbol op,
            string value,
            int index)
        {
            var propName = ConvertToPascalCase(path[index]);
            var member = Expression.PropertyOrField(param, propName);
            var memberType = member.Type;

            // Check if the current member is a collection (excluding string)
            bool isCollection = memberType != typeof(string) &&
                                typeof(System.Collections.IEnumerable).IsAssignableFrom(memberType) &&
                                memberType.IsGenericType;

            if (isCollection)
            {
                var elementType = memberType.GetGenericArguments()[0];
                var elementParam = Expression.Parameter(elementType, "x");

                var subExpression = BuildConditionExpressionForPath(elementParam, [.. path.Skip(index + 1)], op, value, 0);

                var lambda = Expression.Lambda(subExpression, elementParam);
                var collectionNotNull = Expression.NotEqual(member, Expression.Constant(null));

                return Expression.AndAlso(
                    collectionNotNull,
                    Expression.Call(
                        typeof(Enumerable),
                        nameof(Enumerable.Any),
                        new[] { elementType },
                        member,
                        lambda
                    )
                );
            }

            // Base case: last property in path
            if (index == path.Length - 1)
            {
                // Add null-check for reference types excluding string and value types
                Expression? nullCheck = null;
                if (!memberType.IsValueType && memberType != typeof(string))
                {
                    nullCheck = Expression.NotEqual(member, Expression.Constant(null, memberType));
                }

                Expression comparison;
                if (op == Symbols.Between)
                    comparison = ExpressionUtility.Between(member, Expression.Constant(value));
                else if (op == Symbols.Like || op == Symbols.LikeBoth)
                    comparison = ExpressionUtility.Like(member, Expression.Constant(value), op);
                else
                {
                    var constant = ExpressionUtility.ParseType(member.Type, value);
                    comparison = BuildComparison(op, member, constant);
                }

                return nullCheck != null ? Expression.AndAlso(nullCheck, comparison) : comparison;
            }

            // Recursive case: build expression for next path element, with null check at current level
            var nextExpr = BuildConditionExpressionForPath(member, path, op, value, index + 1);
            var currentNullCheck = Expression.NotEqual(member, Expression.Constant(null, member.Type));
            return Expression.AndAlso(currentNullCheck, nextExpr);
        }

        /// <summary>
        /// Builds a binary comparison expression between two expressions according to the operator.
        /// Handles nullable conversions automatically.
        /// </summary>
        private static Expression BuildComparison(Symbol op, Expression left, Expression right)
        {
            // Convert nullable types if necessary for type compatibility
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
                _ => throw new NotSupportedException($"Operator {op} is not supported.")
            };
        }

        /// <summary>
        /// Converts a snake_case string to PascalCase.
        /// </summary>
        private static string ConvertToPascalCase(string input)
        {
            return string.Join("", input.Split('_').Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1)));
        }
    }
}
