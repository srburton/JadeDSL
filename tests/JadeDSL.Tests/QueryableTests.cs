using JadeDSL.Core;
using JadeDSL.Extensions;

namespace JadeDSL.Tests
{
    public class AttributeValue
    {
        public int? ValueInt { get; set; }
    }

    public class ItemWithAttributes
    {
        public List<AttributeValue>? Attributes { get; set; }
    }

    public class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }

        public List<Documents>? Documents { get; set; }
    }

    public class Documents
    {
        public string? Title { get; set; }
    }

    public class Product
    {
        public string? Name { get; set; }
        public decimal Price { get; set; }
    }

    public class QueryableTests
    {
        private readonly List<ItemWithAttributes> _attributesItems =
        [
            new ItemWithAttributes { Attributes = [new() { ValueInt = 9 }] },
            new ItemWithAttributes { Attributes = [new() { ValueInt = 10 }] },
            new ItemWithAttributes { Attributes = [new() { ValueInt = 11 }] },
            new ItemWithAttributes { Attributes = [new() { ValueInt = 12 }] },
            new ItemWithAttributes { Attributes = [new() { ValueInt = null }] },
            new ItemWithAttributes { Attributes = null }
        ];

        private readonly List<Product> _products =
        [
            new Product { Name = "Product A", Price = 10.5m },
            new Product { Name = "Product B", Price = 25.0m },
            new Product { Name = "Product C", Price = 40.0m },
            new Product { Name = "Product D", Price = 50.0m },
        ];

        private readonly List<Person> _people =
        [
            new Person { Name = "Alice", Age = 25 },
            new Person { Name = "Bob", Age = 35 },
            new Person { Name = "Charlie", Age = 40, Documents = [new() { Title = "MOU" }] },
            new Person { Name = "David", Age = 32, Documents = [new() { Title = "Contract" }] },
        ];

        private readonly Action<Options> _options = opts =>
        {
            opts.MaxNodeCount = 20;
            opts.AddAllowedFields("Name", "Age", "Documents.Title", "Price", "Attributes.ValueInt");
        };

        [Fact]
        public void Should_Filter_By_Age_Greater_Than_30()
        {
            var filter = new FilterBuilder()
                .WithExpression("Age>30")
                .ConfigureOptions(_options)
                .Build();

            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            Assert.Equal(3, result.Count);
            Assert.DoesNotContain(result, p => p.Age <= 30);
        }

        [Fact]
        public void Should_Return_All_When_Node_Is_Null()
        {
            var filter = new FilterBuilder()
                .WithExpression("")
                .ConfigureOptions(_options)
                .Build();

            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void Should_Filter_By_Age_Range_With_Tilde()
        {
            var filter = new FilterBuilder()
                .WithExpression("Age~30..40")
                .ConfigureOptions(_options)
                .Build();

            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, p => p.Name == "Bob");
            Assert.Contains(result, p => p.Name == "Charlie");
            Assert.Contains(result, p => p.Name == "David");
        }

        [Fact]
        public void Should_Filter_By_Document_Title()
        {
            var filter = new FilterBuilder()
                .WithExpression("Documents.Title:\"MOU\"")
                .ConfigureOptions(_options)
                .Build();

            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            Assert.Single(result);
            Assert.Equal("Charlie", result[0].Name);
        }

        [Fact]
        public void Should_Filter_By_Age_Range_And_Document_Title()
        {
            // Expression: Age between 30 and 40 AND document title equals "MOU"
            var filter = new FilterBuilder()
                .WithExpression("Age~30..40&Documents.Title:\"MOU\"")
                .ConfigureOptions(_options)
                .Build();

            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            // Only Charlie matches both conditions
            Assert.Single(result);
            Assert.Equal("Charlie", result[0].Name);
        }

        [Fact]
        public void Should_Filter_By_Age_Range_Or_Document_Title()
        {
            // Expression: Age between 30 and 40 OR document title equals "MOU"
            var filter = new FilterBuilder()
                .WithExpression("Age~30..40|Documents.Title:\"MOU\"")
                .ConfigureOptions(_options)
                .Build();

            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            // Expected: Bob (age 35), Charlie (has MOU), David (age 32)
            Assert.Equal(3, result.Count);
            Assert.Contains(result, p => p.Name == "Bob");
            Assert.Contains(result, p => p.Name == "Charlie");
            Assert.Contains(result, p => p.Name == "David");
        }

        [Fact]
        public void Should_Filter_Nested_Groups_With_Or_Operators()
        {
            var filter = new FilterBuilder()
                .WithExpression("Age~30..40&(Documents.Title:\"MOU\"|Documents.Title:\"Contract\")")
                .ConfigureOptions(_options)
                .Build();

            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.Name == "Charlie");
            Assert.Contains(result, p => p.Name == "David");
        }

        [Fact]
        public void Should_Filter_By_Price_Range_With_Tilde()
        {
            var filter = new FilterBuilder()
                .WithExpression("Price~20.0..45.0")
                .ConfigureOptions(_options)
                .Build();

            var result = _products.AsQueryable().WhereDsl(filter).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.Name == "Product B");
            Assert.Contains(result, p => p.Name == "Product C");
        }

        [Fact]
        public void Should_Filter_By_Price_And_Name()
        {
            var filter = new FilterBuilder()
                .WithExpression("Price~20.0..45.0&Name%%\"Product\"")
                .ConfigureOptions(_options)
                .Build();

            var result = _products.AsQueryable().WhereDsl(filter).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Contains("Product", p.Name));
        }

        [Fact]
        public void Should_Filter_By_Nullable_Int_Between_Operator()
        {
            var filter = new FilterBuilder()
                .WithExpression("Attributes.ValueInt~10..11")
                .ConfigureOptions(_options)
                .Build();

            var result = _attributesItems.AsQueryable().WhereDsl(filter).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, i =>
                Assert.True(i.Attributes?.Any(a => a.ValueInt == 10 || a.ValueInt == 11)));
        }
    }
}
