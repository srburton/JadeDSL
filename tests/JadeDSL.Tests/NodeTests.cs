using JadeDSL.Core;
using JadeDSL.Core.Types;
using JadeDSL.Extensions;

namespace JadeDSL.Tests
{
    public class NodeTests
    {
        [Theory]
        [InlineData("name", ":", "John", "name:\"John\"")]
        [InlineData("@aliasAge", ">=", "30", "@aliasAge>=30")]
        [InlineData("price", "~", "100..500", "price~100..500")]
        [InlineData("Name", "%%", "Prod", "Name%%\"Prod\"")]
        [InlineData("Name", "%", "Prod", "Name%\"Prod\"")]
        public void Should_Convert_NodeExpression_To_DslString(string field, string op, string value, string expected)
        {
            var expr = new NodeExpression { Field = field, Operator = new Symbol(op), Value = value };
            var dsl = expr.ToDslString();
            Assert.Equal(expected, dsl);
        }

        [Fact]
        public void Should_Convert_Or_Group_To_DslString()
        {
            var group = new NodeGroup
            {
                Operator = LogicalOperatorType.Or,
                Children =
            [
                new NodeExpression { Field = "city", Operator = Symbols.Colon, Value = "NYC" },
                new NodeExpression { Field = "city", Operator = Symbols.Colon, Value = "LA" }
            ]
            };
            Assert.Equal("city:\"NYC\"|city:\"LA\"", group.ToDslString());
        }

        [Fact]
        public void Should_Convert_And_Group_To_DslString()
        {
            var group = new NodeGroup
            {
                Operator = LogicalOperatorType.And,
                Children =
                [
                    new NodeExpression { Field = "name", Operator = Symbols.Colon, Value = "Alice" },
                    new NodeExpression { Field = "lastname", Operator = Symbols.Colon, Value = "Smith" }
                ]
            };
            Assert.Equal("name:\"Alice\"&lastname:\"Smith\"", group.ToDslString());
        }

        [Fact]
        public void Should_Convert_Or_Of_And_Groups_To_DslString()
        {
            var group = new NodeGroup
            {
                Operator = LogicalOperatorType.Or,
                Children =
                [
                    new NodeGroup
                    {
                        Operator = LogicalOperatorType.And,
                        Children =
                        [
                            new NodeExpression { Field = "name", Operator = Symbols.Colon, Value = "Renato" },
                            new NodeExpression { Field = "age", Operator = Symbols.GreaterThan, Value = "18" }
                        ]
                    },
                    new NodeGroup
                    {
                        Operator = LogicalOperatorType.And,
                        Children =
                        [
                            new NodeExpression { Field = "name", Operator = Symbols.Colon, Value = "Beto" },
                            new NodeExpression { Field = "age", Operator = Symbols.LessThan, Value = "20" }
                        ]
                    }
                ]
            };

            Assert.Equal("(name:\"Renato\"&age>18)|(name:\"Beto\"&age<20)", group.ToDslString());
        }

        [Fact]
        public void Should_Convert_Complex_And_Or_Groups_To_DslString()
        {
            var group = new NodeGroup
            {
                Operator = LogicalOperatorType.And,
                Children =
                [
                    new NodeGroup
                    {
                        Operator = LogicalOperatorType.Or,
                        Children =
                        [
                            new NodeGroup
                            {
                                Operator = LogicalOperatorType.And,
                                Children =
                                [
                                    new NodeExpression { Field = "name", Operator = Symbols.Colon, Value = "Renato" },
                                    new NodeExpression { Field = "age", Operator = Symbols.GreaterThan, Value = "18" }
                                ]
                            },
                            new NodeGroup
                            {
                                Operator = LogicalOperatorType.And,
                                Children =
                                [
                                    new NodeExpression { Field = "name", Operator = Symbols.Colon, Value = "Beto" },
                                    new NodeExpression { Field = "age", Operator = Symbols.LessThan, Value = "20" }
                                ]
                            }
                        ]
                    },
                    new NodeGroup
                    {
                        Operator = LogicalOperatorType.And,
                        Children =
                        [
                            new NodeExpression { Field = "policy", Operator = Symbols.Colon, Value = "dev" }
                        ]
                    }
                ]
            };
            var value = group.ToDslString();

            Assert.Equal("(name:\"Renato\"&age>18)|(name:\"Beto\"&age<20)&policy:\"dev\"", value);
        }
    }
}
