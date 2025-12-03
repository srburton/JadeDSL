using JadeDSL.Core;
using JadeDSL.Core.Types;

namespace JadeDSL.Tests
{
    public class ParsingTests
    {
        private readonly Action<Options> optionsSetup = op =>
        {
            op.AddAllowedFields("name", "lastname", "age", "city", "price", "address.street");
            op.AddAlias("@aliasName", "name");
            op.AddAlias("@aliasAge", "age");
        };

        private FilterBuilder CreateBuilder(string expr)
        {
            return new FilterBuilder()
                .WithExpression(expr)
                .ConfigureOptions(optionsSetup)
                .Build();
        }

        [Fact]
        public void Should_Parse_Expressions_Empty()
        {
            var dsl = new FilterBuilder()
                .WithExpression("")
                .ConfigureOptions(op => { op.AddAllowedFields(); }) // no required
                .Build();

            Assert.Null(dsl.Node);
        }

        [Fact]
        public void Should_Throw_When_Expression_Empty_And_Required()
        {
            var builder = new FilterBuilder()
                .WithExpression("")
                .ConfigureOptions(op => op.Required = true);

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Fact]
        public void Should_Parse_And_Group_Expressions_With_And_Operator()
        {
            var dsl = CreateBuilder("(name:\"Renato\"&lastname:\"Burton\")");
            var node = dsl.Node;

            Assert.NotNull(node);
            var group = Assert.IsType<NodeGroup>(node);
            Assert.Equal(LogicalOperatorType.And, group.Operator);
            Assert.Equal(2, group.Children.Count);
        }

        [Fact]
        public void Should_Parse_Or_Operator_Between_Two_Expressions()
        {
            var dsl = CreateBuilder("(city:\"NYC\"|city:\"LA\")");
            var node = dsl.Node;

            var group = Assert.IsType<NodeGroup>(node);
            Assert.Equal(LogicalOperatorType.Or, group.Operator);
            Assert.Equal(2, group.Children.Count);
        }

        [Fact]
        public void Should_Parse_Nested_And_Or_Groups()
        {
            var dsl = CreateBuilder("((name:\"Renato\"&lastname:\"Burton\")|age:30)");
            var outerGroup = Assert.IsType<NodeGroup>(dsl.Node);

            Assert.Equal(LogicalOperatorType.Or, outerGroup.Operator);
            Assert.Equal(2, outerGroup.Children.Count);

            var innerGroup = Assert.IsType<NodeGroup>(outerGroup.Children[0]);
            Assert.Equal(LogicalOperatorType.And, innerGroup.Operator);
        }

        [Fact]
        public void Should_Parse_Single_Expression()
        {
            var dsl = CreateBuilder("age:25");
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
            var dsl = CreateBuilder(input);
            var node = Assert.IsType<NodeExpression>(dsl.Node);

            Assert.Equal(expectedField, node.Field);
            Assert.Equal(expectedOp, node.Operator.ToString());
            Assert.Equal(expectedValue, node.Value);
        }

        [Theory]
        [InlineData("@aliasName:\"Renato\"", "name", ":", "Renato")]
        [InlineData("@aliasAge>30", "age", ">", "30")]
        public void Should_Parse_Single_Expression_With_Alias(string input, string expectedField, string expectedOp, string expectedValue)
        {
            var dsl = CreateBuilder(input);
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
            var dsl = CreateBuilder(input);
            var group = Assert.IsType<NodeGroup>(dsl.Node);

            Assert.Equal(expectedOperator, group.Operator);
            Assert.Equal(2, group.Children.Count);
            Assert.All(group.Children, c => Assert.IsType<NodeExpression>(c));
        }

        [Fact]
        public void Should_Parse_Nested_Group_With_And_And_Or()
        {
            var input = "((age>30&name:\"Renato\")|price~10..100)";
            var dsl = CreateBuilder(input);
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
            Assert.Throws<InvalidOperationException>(() => CreateBuilder(input));
        }

        [Theory]
        [InlineData("((((((((abc:foo)))))))")]
        [InlineData("((abc:foo)")]
        [InlineData("abc:foo))")]
        [InlineData("name:\"Unclosed quote")]
        public void Should_Throw_On_Unbalanced_Parentheses_Or_Quotes(string input)
        {
            Assert.ThrowsAny<Exception>(() => CreateBuilder(input));
        }

        [Theory]
        [InlineData("name:\"$\"")]
        [InlineData("name:\"%\"")]
        [InlineData("name:\"+\"")]
        [InlineData("name:\"!\"")]
        [InlineData("name:\"*\"")]
        [InlineData("name:\"()\"")]
        [InlineData("name:\"------------**&+++???;;\"")]        
        public void Should_Allow_Special_Characters_Inside_Quoted_Value(string input)
        {
            var builder = CreateBuilder(input);
            Assert.NotNull(builder);
        }

        [Theory]
        [InlineData("name[]john,maria,pedro")]
        [InlineData("name[](john,maria,pedro)")]
        [InlineData("name[]\"john\",\"maria\",\"pedro\"")]
        [InlineData("name[]\"John Doe\",\"Maria Silva\"")]
        [InlineData("name[]\"%\"")]
        [InlineData("name[]\"*\"")]
        [InlineData("name[]\"------------**&+++???;;\"")]
        [InlineData("name[]\"john, the great\"")]  // vírgula dentro de aspas
        [InlineData("name[]\"()\",\"[]\",\"{}\"")]
        [InlineData("lastname[]silva,souza,lima")]
        [InlineData("lastname[](silva,souza,lima)")]
        [InlineData("lastname[]\"Silva\",\"Souza\",\"Lima\"")]
        [InlineData("lastname[]\"A\",\"B\",\"C\"")]
        [InlineData("age[]1,2,3")]       
        [InlineData("age[]10")]
        [InlineData("age[]\"1\",\"2\",\"3\",\"4\"")]
        [InlineData("age[]  1 ,  20 ,300")]
        [InlineData("age[]1,1,1")] // repetidos
        [InlineData("city[]NY,LA,Miami")]
        [InlineData("city[]\"New York\",\"Los Angeles\",\"Miami\"")]
        [InlineData("city[]\"São Paulo\",\"Rio de Janeiro\"")]
        [InlineData("price[]10,20,30")]
        [InlineData("price[](100.5,200.75,300.25)")]
        [InlineData("price[]0,999,1500")]
        [InlineData("address.street[]main,first,second")]
        [InlineData("address.street[]\"Main St\",\"First Ave\",\"Second Blvd\"")]
        [InlineData("address.street[]\"R. das Flores\",\"Av. Central\"")]
        [InlineData("@aliasName[]john,pedro")]
        [InlineData("@aliasName[]\"john\",\"maria\"")]
        [InlineData("@aliasName[]\"%\"")]
        [InlineData("@aliasAge[](1,2,3)")]
        [InlineData("@aliasAge[](5,10,15)")]
        [InlineData("@aliasAge[]100")]
        [InlineData("@aliasAge[]1,1,1")]
        public void Should_Parse_In_Operator(string input)
        {
            var builder = CreateBuilder(input);
            Assert.NotNull(builder);
        }
    }
}
