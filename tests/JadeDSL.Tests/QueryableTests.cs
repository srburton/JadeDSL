using JadeDSL.Core;
using JadeDSL.Extensions;


namespace JadeDSL.Tests
{
    public class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
    public class QueryableTests
    {
        private readonly List<Person> _people = new()
    {
        new Person { Name = "Alice", Age = 25 },
        new Person { Name = "Bob", Age = 35 },
        new Person { Name = "Charlie", Age = 40 }
    };

        private readonly Action<Options> _options = opts =>
        {
            opts.AddAllowedFields("Name", "Age");
        };

        [Fact]
        public void Should_Filter_By_Age_Greater_Than_30()
        {
            // Arrange
            var filter = new FilterBuilder()
                .WithExpression("Age>30")
                .ConfigureOptions(_options)
                .Build();

            // Act
            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, p => p.Age <= 30);
        }

        [Fact]
        public void Should_Return_All_When_Node_Is_Null()
        {
            // Arrange
            var filter = new FilterBuilder()
                .WithExpression("") // empty, so Node will be null
                .ConfigureOptions(_options)
                .Build();

            // Act
            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            // Assert
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void Should_Filter_By_Age_Range_With_Tilde()
        {
            var filter = new FilterBuilder()
                .WithExpression("Age~30..40")
                .ConfigureOptions(_options)
                .Build();

            var result = _people.AsQueryable().WhereDsl(filter).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.Name == "Bob");
            Assert.Contains(result, p => p.Name == "Charlie");
        }
    }
}
