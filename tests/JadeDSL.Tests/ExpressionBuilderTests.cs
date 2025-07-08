using JadeDSL.Core;
using JadeDSL.Core.Types;

namespace JadeDSL.Tests
{
    public class Property
    {
        public List<Attribute>? Attributes { get; set; }
    }

    public class Attribute
    {
        public string? Name { get; set; }
        public int? ValueInt { get; set; }
    }

    public class ExpressionBuilderTests
    {
        List<Property> _data = new List<Property>
            {
                new Property
                {
                    Attributes = new List<Attribute>
                    {
                        new Attribute { Name = "Monthly Cash Flow", ValueInt = 5 },
                        new Attribute { Name = "Other", ValueInt = 50 }
                    }
                },
                new Property
                {
                    Attributes = new List<Attribute>
                    {
                        new Attribute { Name = "Monthly Cash Flow", ValueInt = 20 }
                    }
                },
                new Property
                {
                    Attributes = new List<Attribute>
                    {
                        new Attribute { Name = "Other", ValueInt = 5 }
                    }
                }
            };

        [Fact]
        public void CombineGroup_Should_Group_All_Expressions_With_Same_Collection_Into_Single_Any()
        {
            // Arrange
            var group = new NodeGroup
            {
                Operator = LogicalOperatorType.And,
                Children = new List<Node>
                {
                    new NodeExpression
                    {
                        Field = "attributes.name",
                        Operator = Symbols.Colon,
                        Value = "Monthly Cash Flow"
                    },
                    new NodeExpression
                    {
                        Field = "attributes.value_int",
                        Operator = Symbols.Between,
                        Value = "1..10"
                    }
                }
            };

            // Act
            var predicate = ExpressionBuilder.BuildPredicate<Property>(group);
            var compiled = predicate.Compile();

            var filtered = _data.Where(compiled).ToList();

            // Assert
            Assert.Single(filtered);
            var matchedProperty = filtered[0];
            Assert.NotNull(matchedProperty.Attributes);

            bool hasMatchingAttribute = matchedProperty.Attributes.Any(a =>
                a.Name == "Monthly Cash Flow" &&
                a.ValueInt.HasValue &&
                a.ValueInt.Value >= 1 &&
                a.ValueInt.Value <= 10);

            Assert.True(hasMatchingAttribute, "Filtered property should contain attribute with Name='Monthly Cash Flow' and ValueInt between 1 and 10");
        }
    }
}
