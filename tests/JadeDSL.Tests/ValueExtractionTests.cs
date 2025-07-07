using JadeDSL.Core;

namespace JadeDSL.Tests
{
    public class ValueExtractionTests
    {
        private readonly Action<Options> optionsSetup = op =>
        {
            op.AddAllowedFields("name", "age", "city");
            op.AddAlias("@aliasName", "name");
        };

        private FilterBuilder CreateBuilder(string expr)
        {
            return new FilterBuilder()
                .WithExpression(expr)
                .ConfigureOptions(optionsSetup)
                .Build();
        }

        [Fact]
        public void Should_Extract_Single_Field_Value()
        {
            var builder = CreateBuilder("name:\"Alice\"");
            var values = builder.GetValuesFor("name").ToList();

            Assert.Single(values);
            Assert.Contains("Alice", values);
        }

        [Fact]
        public void Should_Extract_Multiple_Values_For_Field()
        {
            var builder = CreateBuilder("(name:\"Alice\"|name:\"Bob\")");
            var values = builder.GetValuesFor("name").ToList();

            Assert.Equal(2, values.Count);
            Assert.Contains("Alice", values);
            Assert.Contains("Bob", values);
        }

        [Fact]
        public void Should_Extract_Values_From_Nested_Groups()
        {
            var builder = CreateBuilder("((name:\"Alice\"&age>30)|city:\"NYC\")");
            var nameValues = builder.GetValuesFor("name").ToList();
            var ageValues = builder.GetValuesFor("age").ToList();
            var cityValues = builder.GetValuesFor("city").ToList();

            Assert.Single(nameValues);
            Assert.Contains("Alice", nameValues);

            Assert.Single(ageValues);
            Assert.Contains("30", ageValues);

            Assert.Single(cityValues);
            Assert.Contains("NYC", cityValues);
        }

        [Fact]
        public void Should_Handle_Unknown_Field()
        {
            var builder = CreateBuilder("name:\"Alice\"");
            var values = builder.GetValuesFor("unknown").ToList();

            Assert.Empty(values);
        }

        [Fact]
        public void Should_Resolve_Alias_And_Extract_Value()
        {
            var builder = CreateBuilder("@aliasName:\"Alice\"");
            var values = builder.GetValuesFor("name").ToList();

            Assert.Single(values);
            Assert.Contains("Alice", values);
        }
    }
}
