
namespace JadeDSL.Core
{
    public class Options
    {
        /// <summary>
        /// Use true when the filter is required
        /// </summary>
        public bool Required { get; set; } = false;

        public int MaxExpressionLength { get; set; } = 10;

        public int MaxExpressionDepth { get; set; } = 5;

        public int MaxNodeCount { get; set; } = 10;
       
        public string[] AllowedFields { get; set; } = [];

        public Symbol[] AllowedSymbols { get; set; } =
        [
            Symbols.Equal,
            Symbols.NotEqual,
            Symbols.GreaterThan,
            Symbols.GreaterThanOrEqual,
            Symbols.LessThan,
            Symbols.LessThanOrEqual,
            Symbols.Colon,
            Symbols.Like,
            Symbols.Between
        ];       
    }
}