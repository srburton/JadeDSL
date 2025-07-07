using System.Reflection;
using System.ComponentModel;

namespace JadeDSL.Core.Helpers
{
    public static class EnumHelper
    {
        public static TEnum ParseFromNameOrDescription<TEnum>(string raw, bool ignoreCase = true) where TEnum : struct, Enum
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var descAttr = field.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr != null && string.Equals(descAttr.Description, raw, comparison))
                {
                    return (TEnum)field.GetValue(null)!;
                }
            }

            return Enum.Parse<TEnum>(raw, ignoreCase);
        }

        public static object ParseFromNameOrDescription(Type enumType, string raw, bool ignoreCase = true)
        {
            if (!enumType.IsEnum)
                throw new ArgumentException("Type must be an enum.", nameof(enumType));

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var descAttr = field.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr != null && string.Equals(descAttr.Description, raw, comparison))
                {
                    return field.GetValue(null)!;
                }
            }

            return Enum.Parse(enumType, raw, ignoreCase);
        }
    }
}