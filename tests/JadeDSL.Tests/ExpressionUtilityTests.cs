using System.ComponentModel;
using System.Linq.Expressions;
using JadeDSL.Core.Extensions;

namespace JadeDSL.Tests
{
    public class ExpressionUtilityTests
    {
        enum Status
        {
            Active,
            Inactive,

            [Description("Pending Approval")]
            PendingApproval
        }

        public static IEnumerable<object[]> ParseTypeData => [
            ["123", typeof(int), 123],
            ["3.14", typeof(decimal), 3.14m],
            ["true", typeof(bool), true],
            ["2025-01-01", typeof(DateTime), DateTime.Parse("2025-01-01")],
            ["5F250E29-5C7A-4B7E-BEAB-BC48A7F84632", typeof(Guid), Guid.Parse("5F250E29-5C7A-4B7E-BEAB-BC48A7F84632")],
            ["hello", typeof(string), "hello"]
        ];

        [Theory]
        [MemberData(nameof(ParseTypeData))]
        public void ParseType_ShouldParseBasicTypes(string input, Type type, object expected)
        {
            var expr = ExpressionUtility.ParseType(type, input);
            var value = ((ConstantExpression)expr).Value;

            Assert.Equal(expected, value);
        }

        [Fact]
        public void ParseType_ShouldParseNullableInt()
        {
            var expr = ExpressionUtility.ParseType(typeof(int?), "42");
            var value = ((ConstantExpression)expr).Value;
            Assert.Equal(42, value);
        }

        [Fact]
        public void ParseType_ShouldThrowOnUnsupportedType()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ExpressionUtility.ParseType(typeof(float), "3.14"));

            Assert.Contains("Unsupported constant type", ex.Message);
        }

        [Fact]
        public void ParseType_ShouldParseEnum_ByName()
        {
            var expr = ExpressionUtility.ParseType(typeof(Status), "Active");
            var value = ((ConstantExpression)expr).Value;
            Assert.Equal(Status.Active, value);
        }

        [Fact]
        public void ParseType_ShouldParseEnum_ByDescription()
        {
            var expr = ExpressionUtility.ParseType(typeof(Status), "Pending Approval");
            var value = ((ConstantExpression)expr).Value;
            Assert.Equal(Status.PendingApproval, value);
        }

        [Fact]
        public void ParseType_ShouldParseNullableEnum_ByName()
        {
            var expr = ExpressionUtility.ParseType(typeof(Status?), "Inactive");
            var value = ((ConstantExpression)expr).Value;
            Assert.Equal(Status.Inactive, value);
        }

        [Fact]
        public void ParseType_ShouldParseNullableEnum_ByDescription()
        {
            var expr = ExpressionUtility.ParseType(typeof(Status?), "Pending Approval");
            var value = ((ConstantExpression)expr).Value;
            Assert.Equal(Status.PendingApproval, value);
        }
    }
}