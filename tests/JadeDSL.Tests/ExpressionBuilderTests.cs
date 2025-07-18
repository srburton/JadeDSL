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

        public NestedAttribute? SubAttribute { get; set; }
    }

    public class NestedAttribute
    {
        public string? Name { get; set; }
    }

    public class CompanyDocument
    {
        public Document? Document { get; set; }
    }

    public class Document
    {
        public string? Title { get; set; }
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

        private readonly List<CompanyDocument> _documents = new()
        {
            new CompanyDocument { Document = new Document { Title = "Invoice 2024" } },
            new CompanyDocument { Document = new Document { Title = "Receipt" } },
            new CompanyDocument { Document = null }
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

            Assert.True(hasMatchingAttribute);
        }

        [Fact]
        public void Should_Filter_By_Document_Title()
        {

            // Arrange
            var group = new NodeGroup
            {
                Operator = LogicalOperatorType.And,
                Children = new List<Node>
                {
                    new NodeExpression
                    {
                        Field = "document.title",
                        Operator = Symbols.LikeBoth,
                        Value = "Invoice"
                    }
                }
            };

            // Act
            var predicate = ExpressionBuilder.BuildPredicate<CompanyDocument>(group);
            var compiled = predicate.Compile();
            var filtered = _documents.Where(compiled).ToList();

            // Assert
            Assert.Single(filtered);
            Assert.Equal("Invoice 2024", filtered[0].Document?.Title);
        }

        [Fact]
        public void Should_Filter_By_Deeply_Nested_Collection_Attribute()
        {
            var extendedData = new List<Property>(_data)
            {
                new Property
                {
                    Attributes = new List<Attribute>
                    {
                        new Attribute
                        {
                            SubAttribute = new NestedAttribute { Name = "TargetName" },
                            ValueInt = 100
                        }
                    }
                }
            };

            var group = new NodeGroup
            {
                Operator = LogicalOperatorType.And,
                Children =
                [
                    new NodeExpression
                    {
                        Field = "attributes.sub_attribute.name",
                        Operator = Symbols.Colon,
                        Value = "TargetName"
                    }
                ]
            };

            // Act
            var predicate = ExpressionBuilder.BuildPredicate<Property>(group);
            var compiled = predicate.Compile();
            var filtered = extendedData.Where(compiled).ToList();

            // Assert
            Assert.Single(filtered);
            var matched = filtered.First();
            Assert.NotNull(matched.Attributes);
            Assert.Contains(matched.Attributes, a => a.SubAttribute?.Name == "TargetName");
        }
    }
}
