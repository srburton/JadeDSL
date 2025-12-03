using JadeDSL.Core.Helpers;
using System.Collections;
using System.Linq.Expressions;
using System.Text;

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
            var method = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
            var methodStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
            var methodEndsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

            var value = ((ConstantExpression)constant).Value?.ToString() ?? "";

            return op switch
            {
                var o when o == Symbols.Like => Expression.Call(member, methodEndsWith, constant),  // ends with
                var o when o == Symbols.LikeBoth => Expression.Call(member, method, constant),      // contains (both sides)
                _ => throw new NotSupportedException($"Operator {op} not supported.")
            };
        }

        public static Expression In(Expression left, Expression right)
        {
            if (right is not ConstantExpression constExpr || constExpr.Value is not string raw)
                throw new NotSupportedException("IN expression must have a string constant value.");

            raw = raw.Trim();

            // Remove parênteses se existirem
            if (raw.StartsWith("(") && raw.EndsWith(")"))
                raw = raw[1..^1];

            // Lista para armazenar os valores
            var parts = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes; // alterna estado de aspas
                    continue; // remove as aspas
                }

                if (c == ',' && !inQuotes)
                {
                    // separa os valores fora de aspas
                    var value = current.ToString().Trim();
                    if (!string.IsNullOrEmpty(value))
                        parts.Add(value);
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            // último valor
            var last = current.ToString().Trim();
            if (!string.IsNullOrEmpty(last))
                parts.Add(last);

            if (parts.Count == 0)
                throw new ArgumentException("IN list cannot be empty.");

            // Tipo base do membro
            var leftBaseType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;

            // Cria lista fortemente tipada
            var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(leftBaseType))!;
            foreach (var part in parts)
            {
                typedList.Add(Convert.ChangeType(part, leftBaseType));
            }

            // ConstantExpression da lista
            var listExpr = Expression.Constant(typedList);

            // Converte membro se for Nullable<T>
            Expression leftConverted = left.Type != leftBaseType ? Expression.Convert(left, leftBaseType) : left;

            // Chama Enumerable.Contains<T>(IEnumerable<T>, T)
            var containsMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == nameof(Enumerable.Contains)
                            && m.GetParameters().Length == 2)
                .MakeGenericMethod(leftBaseType);

            return Expression.Call(
                null,
                containsMethod,
                listExpr,
                leftConverted
            );
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
