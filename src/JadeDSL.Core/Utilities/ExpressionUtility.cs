using JadeDSL.Core.Helpers;
using System.Linq.Expressions;

namespace JadeDSL.Core.Extensions
{
    public static class ExpressionUtility
    {
        public static Expression Between(Expression left, Expression right)
        {
            if (right is not ConstantExpression constExpr || constExpr.Value is not string raw)
                throw new NotSupportedException("Between expression must have a string constant value.");

            var parts = raw.Split("..");
            if (parts.Length != 2)
                throw new ArgumentException("Between value must be in 'min..max' format.");

            var leftBaseType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;

            object minValue = Convert.ChangeType(parts[0], leftBaseType);
            object maxValue = Convert.ChangeType(parts[1], leftBaseType);

            var minConst = Expression.Constant(minValue, leftBaseType);
            var maxConst = Expression.Constant(maxValue, leftBaseType);

            // Promote constants if left is nullable
            Expression min = left.Type != leftBaseType ? Expression.Convert(minConst, left.Type) : minConst;
            Expression max = left.Type != leftBaseType ? Expression.Convert(maxConst, left.Type) : maxConst;

            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(left, min),
                Expression.LessThanOrEqual(left, max)
            );
        }

        public static Expression Like(Expression member, Expression constant, Symbol op)
        {
            var method = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
            var methodStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
            var methodEndsWith = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!;

            var value = ((ConstantExpression)constant).Value?.ToString() ?? "";

            return op switch
            {
                var o when o == Symbols.Like => Expression.Call(member, methodEndsWith, constant),  // ends with
                var o when o == Symbols.LikeBoth => Expression.Call(member, method, constant),      // contains (both sides)
                _ => throw new NotSupportedException($"Operator {op} not supported.")
            };
        }


        public static Expression ParseType(Type type, string raw)
        {
            if (type == typeof(string)) return Expression.Constant(raw);
            if (type == typeof(int) || type == typeof(int?)) return Expression.Constant(int.Parse(raw));
            if (type == typeof(decimal) || type == typeof(decimal?)) return Expression.Constant(decimal.Parse(raw));
            if (type == typeof(bool) || type == typeof(bool?)) return Expression.Constant(bool.Parse(raw));
            if (type == typeof(DateTime) || type == typeof(DateTime?)) return Expression.Constant(DateTime.Parse(raw));
            if (type == typeof(Guid) || type == typeof(Guid?)) return Expression.Constant(Guid.Parse(raw));

            if (type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true)
                return CreateEnumConstant(type, raw);

            throw new InvalidOperationException($"Unsupported constant type: {type}");
        }

        public static Expression CreateEnumConstant(Type targetType, string raw)
        {
            var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (!enumType.IsEnum)
                throw new ArgumentException("Target type is not an enum or nullable enum.", nameof(targetType));

            var value = EnumHelper.ParseFromNameOrDescription(enumType, raw);
            return Expression.Constant(value, targetType);
        }
    }
}
