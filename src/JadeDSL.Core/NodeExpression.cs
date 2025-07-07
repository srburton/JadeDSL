namespace JadeDSL.Core
{
    /// <summary>
    /// Represents a basic field-operator-value filter expression node.
    /// </summary>
    public class NodeExpression : Node
    {
        /// <summary>
        /// The target field path (e.g., "age", "user.name").
        /// </summary>
        public required string Field { get; set; }

        /// <summary>
        /// The operator used for comparison (e.g., =, !=, >, etc.).
        /// </summary>
        public Symbol Operator { get; set; } = Symbols.Equal;

        /// <summary>
        /// The value to compare against.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Validates the current expression structure and values.
        /// </summary>
        /// <returns><c>true</c> if the expression is structurally valid; otherwise, <c>false</c>.</returns>
        public bool IsValid
        {
            get
            {
                // Field must not be empty and must not end with an operator symbol
                if (string.IsNullOrWhiteSpace(Field) ||
                    Symbols.All.Any(sym => Field.EndsWith(sym.ToString())))
                    return false;

                // Operator must be one of the supported ones
                if (!Symbols.All.Contains(Operator))
                    return false;

                // Value must not start with a known operator (to avoid malformed inputs like ":!=abc")
                if (string.IsNullOrWhiteSpace(Value) ||
                    Symbols.All.Any(sym => Value.StartsWith(sym.ToString()) || Value.EndsWith(sym.ToString())) ||
                    Symbols.Others.Any(sym => Value.StartsWith(sym.ToString()) || Value.EndsWith(sym.ToString())))
                    return false;

                return true;
            }
        }
    }
}
