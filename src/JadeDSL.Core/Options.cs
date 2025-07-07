
namespace JadeDSL.Core
{
    public class Options
    {
        /// <summary>
        /// Use true when the filter is required
        /// </summary>
        public bool Required { get; set; } = false;

        public int MaxNodeCount { get; set; } = 10;

        /// <summary>
        /// Allowed fields for filtering; duplicates are avoided
        /// </summary>
        public List<string> AllowedFields { get; set; } = [];

        /// <summary>
        /// Map of aliases to real field names
        /// </summary>
        public Dictionary<string, string> FieldAliases { get; set; } = [];

        public Symbol[] AllowedSymbols { get; set; } =
        {
            Symbols.Equal,
            Symbols.NotEqual,
            Symbols.GreaterThan,
            Symbols.GreaterThanOrEqual,
            Symbols.LessThan,
            Symbols.LessThanOrEqual,
            Symbols.Colon,
            Symbols.Like,
            Symbols.Between
        };

        /// <summary>
        /// Adds fields to the allowed fields set
        /// </summary>
        public Options AddAllowedFields(params string[] fields)
        {
            if (fields != null)
            {
                foreach (var f in fields)
                    AllowedFields.Add(f);
            }
            return this;
        }

        /// <summary>
        /// Adds an alias mapping from alias to real field name
        /// Example: alias = "@myAlias", realField = "Name"
        /// </summary>
        public Options AddAlias(string alias, string realField)
        {
            if (!string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(realField))
            {
                FieldAliases[alias] = realField;
            }
            return this;
        }

        /// <summary>
        /// Resolves a field alias to the real field name,
        /// or returns the original string if not an alias
        /// </summary>
        public string ResolveAlias(string fieldOrAlias)
        {
            if (FieldAliases.TryGetValue(fieldOrAlias, out var realField))
                return realField;
            return fieldOrAlias;
        }
    }
}