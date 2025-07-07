using JadeDSL.Core;
using JadeDSL.Core.Types;
using Xunit;

namespace JadeDSL.Tests
{
    public class ParsingTests
    {
        private readonly Action<Options> options = op =>
        {
            op.AllowedFields = ["name", "lastname", "age", "city", "price", "address.street"];
        };

        [Fact]
        public void Should_Parse_And_Group_Expressions_With_And_Operator()
        {
            var dsl = new FilterBuilder("(name:\"Renato\"&lastname:\"Burton\")", options);
            var node = dsl.Node;

            Assert.NotNull(node);
            var group = Assert.IsType<NodeGroup>(node);
            Assert.Equal(LogicalOperatorType.And, group.Operator);
            Assert.Equal(2, group.Children.Count);
        }

        [Fact]
        public void Should_Parse_Or_Operator_Between_Two_Expressions()
        {
            var dsl = new FilterBuilder("(city:\"NYC\"|city:\"LA\")", options);
            var node = dsl.Node;

            var group = Assert.IsType<NodeGroup>(node);
            Assert.Equal(LogicalOperatorType.Or, group.Operator);
            Assert.Equal(2, group.Children.Count);
        }

        [Fact]
        public void Should_Parse_Nested_And_Or_Groups()
        {
            var dsl = new FilterBuilder("((name:\"Renato\"&lastname:\"Burton\")|age:30)", options);
            var outerGroup = Assert.IsType<NodeGroup>(dsl.Node);

            Assert.Equal(LogicalOperatorType.Or, outerGroup.Operator);
            Assert.Equal(2, outerGroup.Children.Count);

            var innerGroup = Assert.IsType<NodeGroup>(outerGroup.Children[0]);
            Assert.Equal(LogicalOperatorType.And, innerGroup.Operator);
        }

        [Fact]
        public void Should_Parse_Single_Expression()
        {
            var dsl = new FilterBuilder("age:25", options);
            var expr = Assert.IsType<NodeExpression>(dsl.Node);

            Assert.Equal("age", expr.Field);
            Assert.Equal(Symbols.Colon, expr.Operator);
            Assert.Equal("25", expr.Value);
        }

        [Theory]
        [InlineData("age=30", "age", "=", "30")]
        [InlineData("age!=30", "age", "!=", "30")]
        [InlineData("age>30", "age", ">", "30")]
        [InlineData("age>=30", "age", ">=", "30")]
        [InlineData("age<30", "age", "<", "30")]
        [InlineData("age<=30", "age", "<=", "30")]
        [InlineData("name:\"Renato\"", "name", ":", "Renato")]
        [InlineData("name%\"Ren\"", "name", "%", "Ren")]
        [InlineData("price~10..100", "price", "~", "10..100")]
        [InlineData("address.street:\"Main\"", "address.street", ":", "Main")]
        [InlineData("name:\"John Doe\"", "name", ":", "John Doe")]
        public void Should_Parse_Single_Expression_With_Supported_Operators(string input, string expectedField, string expectedOp, string expectedValue)
        {
            var dsl = new FilterBuilder(input, options);
            var node = Assert.IsType<NodeExpression>(dsl.Node);

            Assert.Equal(expectedField, node.Field);
            Assert.Equal(expectedOp, node.Operator.ToString());
            Assert.Equal(expectedValue, node.Value);
        }

        [Theory]
        [InlineData("(age=30&name:\"Renato\")", LogicalOperatorType.And)]
        [InlineData("(age=30|name:\"Renato\")", LogicalOperatorType.Or)]
        public void Should_Parse_Grouped_Expressions_With_Logical_Operator(string input, LogicalOperatorType expectedOperator)
        {
            var dsl = new FilterBuilder(input, options);
            var group = Assert.IsType<NodeGroup>(dsl.Node);

            Assert.Equal(expectedOperator, group.Operator);
            Assert.Equal(2, group.Children.Count);
            Assert.All(group.Children, c => Assert.IsType<NodeExpression>(c));
        }

        [Fact]
        public void Should_Parse_Nested_Group_With_And_And_Or()
        {
            var input = "((age>30&name:\"Renato\")|price~10..100)";
            var dsl = new FilterBuilder(input, options);
            var outer = Assert.IsType<NodeGroup>(dsl.Node);

            Assert.Equal(LogicalOperatorType.Or, outer.Operator);
            Assert.Equal(2, outer.Children.Count);

            var inner = Assert.IsType<NodeGroup>(outer.Children[0]);
            Assert.Equal(LogicalOperatorType.And, inner.Operator);
            Assert.Equal(2, inner.Children.Count);
        }

        [Theory]
        [InlineData("invalid_expression")]
        [InlineData("(name:\"Renato\"&")]
        [InlineData("(name:\"Renato\"|age")]
        [InlineData("name:!abc")]
        [InlineData("name:==")]
        [InlineData("name:>")]
        [InlineData("(age=30|name:")]
        public void Should_Throw_InvalidOperationException_On_Syntax_Errors(string input)
        {
            Assert.Throws<InvalidOperationException>(() => new FilterBuilder(input, options));
        }

        [Theory]
        [InlineData("((((((((abc:foo)))))))")]
        [InlineData("((abc:foo)")]
        [InlineData("abc:foo))")]
        [InlineData("name:\"Unclosed quote")]
        public void Should_Throw_On_Unbalanced_Parentheses_Or_Quotes(string input)
        {
            Assert.ThrowsAny<Exception>(() => new FilterBuilder(input, options));
        }
    }
}
