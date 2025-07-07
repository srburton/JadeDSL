using JadeDSL.Core;
using JadeDSL.Interfaces;

namespace JadeDSL.Extensions
{
    public static class QueryableExtension
    {
        /// <summary>
        /// Applies a Node to a queryable collection.
        /// </summary>
        public static IQueryable<T> WhereDsl<T>(this IQueryable<T> source, IFilter filter)
        {
            if (filter.Node is null)
                return source;

            var lambda = ExpressionBuilder.BuildPredicate<T>(filter.Node);
            return source.Where(lambda);
        }

        /// <summary>
        /// Applies a Node to an in-memory enumerable (compiled).
        /// </summary>
        public static IEnumerable<T> WhereDsl<T>(this IEnumerable<T> source, IFilter filter)
        {
            if (filter.Node is null)
                return source;

            var compiled = ExpressionBuilder.BuildPredicate<T>(filter.Node).Compile();
            return source.Where(compiled);
        }

        /// <summary>
        /// Applies a JadeDSL predicate to a queryable collection.
        /// </summary>
        public static IQueryable<T> WhereDsl<T>(this IQueryable<T> source, JadeDSL dsl)
        {
            if (dsl.Node is null)
                return source;

            var lambda = ExpressionBuilder.BuildPredicate<T>(dsl.Node);
            return source.Where(lambda);
        }

        /// <summary>
        /// Applies a JadeDSL predicate to an in-memory enumerable (compiled).
        /// </summary>
        public static IEnumerable<T> WhereDsl<T>(this IEnumerable<T> source, JadeDSL dsl)
        {
            if (dsl.Node is null)
                return source;

            var compiled = ExpressionBuilder.BuildPredicate<T>(dsl.Node).Compile();
            return source.Where(compiled);
        }
    }
}
