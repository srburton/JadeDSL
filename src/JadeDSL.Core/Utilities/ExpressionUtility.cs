using JadeDSL.Core.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace JadeDSL.Core.Extensions
{
    public static class ExpressionUtility
    {
        public static Expression Between(Expression left, Expression right)
        {
            if (right is not ConstantExpression constExpr || constExpr.Value is not string s)
                throw new NotSupportedException("BETWEEN requires a constant range string like '10/20'.");

            var parts = s.Split('/');

            if (parts.Length != 2)
                throw new ArgumentException("BETWEEN value must be in 'min/max' format.");

            var (minRaw, maxRaw) = (parts[0], parts[1]);

            var min = ParseType(left.Type, minRaw);
            var max = ParseType(left.Type, maxRaw);

            var greaterThanOrEqual = Expression.GreaterThanOrEqual(left, min);
            var lessThanOrEqual = Expression.LessThanOrEqual(left, max);

            return Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
        }

        public static Expression Like(Expression left, Expression right)
        {
            if (left.Type != typeof(string))
                throw new NotSupportedException("LIKE operator only supports string expressions.");

            if (right is not ConstantExpression constExpr)
                throw new NotSupportedException("LIKE operator requires a constant string pattern.");

            var pattern = constExpr.Value?.ToString() ?? "";

            MethodInfo method;
            string value;

            if (pattern.StartsWith("%") && pattern.EndsWith("%"))
            {
                method = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
                value = pattern.Trim('%');
            }
            else if (pattern.StartsWith("%"))
            {
                method = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
                value = pattern.TrimStart('%');
            }
            else if (pattern.EndsWith("%"))
            {
                method = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
                value = pattern.TrimEnd('%');
            }
            else
            {
                method = typeof(string).GetMethod(nameof(string.Equals), [typeof(string)])!;
                value = pattern;
            }

            return Expression.Call(left, method, Expression.Constant(value));
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
