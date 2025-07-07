using System.Text;

namespace JadeDSL.Core
{
    public static class Symbols
    {
        public static readonly Symbol Equal = "=";
        public static readonly Symbol NotEqual = "!=";
        public static readonly Symbol GreaterThan = ">";
        public static readonly Symbol GreaterThanOrEqual = ">=";
        public static readonly Symbol LessThan = "<";
        public static readonly Symbol LessThanOrEqual = "<=";
        public static readonly Symbol Colon = ":";
        public static readonly Symbol Like = "%";
        public static readonly Symbol LikeBoth = "%%";
        public static readonly Symbol Between = "~";

        public static readonly Symbol[] Others = [
           "(",
           ")",
           ".",
           "$",
           ",",
           "*",
           "'",
           "`",
           "´",
           "\\",
           "]",
           "[",
           ";",
           "!"           
        ];

        public static readonly Symbol[] All = [
           Equal,
           NotEqual,
           GreaterThan,
           GreaterThanOrEqual,
           LessThan,
           LessThanOrEqual,
           Colon,
           Like,
           LikeBoth,
           Between               
        ];
    }

    public readonly struct Symbol(string value) : IEquatable<Symbol>
    {
        public string Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

        public static implicit operator Symbol(string value) => new Symbol(value);
        public static implicit operator string(Symbol symbol) => symbol.Value;

        public static bool operator ==(Symbol left, Symbol right) => left.Equals(right);
        public static bool operator !=(Symbol left, Symbol right) => !left.Equals(right);

        public static bool operator ==(Symbol left, string right) => left.Value == right;
        public static bool operator !=(Symbol left, string right) => left.Value != right;

        public static bool operator ==(string left, Symbol right) => left == right.Value;
        public static bool operator !=(string left, Symbol right) => left != right.Value;

        public override string ToString() => Value.Normalize(NormalizationForm.FormC);

        public override bool Equals(object? obj) => obj is Symbol other && Equals(other);
        public bool Equals(Symbol other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
    }
}
