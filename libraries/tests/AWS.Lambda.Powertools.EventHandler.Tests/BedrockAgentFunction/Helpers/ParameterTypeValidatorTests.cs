using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers;

namespace AWS.Lambda.Powertools.EventHandler.BedrockAgentFunction.Helpers
{
    public class ParameterTypeValidatorTests
    {
        private readonly ParameterTypeValidator _validator = new();

        [Theory]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(long), true)]
        [InlineData(typeof(double), true)]
        [InlineData(typeof(bool), true)]
        [InlineData(typeof(decimal), true)]
        [InlineData(typeof(DateTime), true)]
        [InlineData(typeof(Guid), true)]
        [InlineData(typeof(string[]), true)]
        [InlineData(typeof(int[]), true)]
        [InlineData(typeof(long[]), true)]
        [InlineData(typeof(double[]), true)]
        [InlineData(typeof(bool[]), true)]
        [InlineData(typeof(decimal[]), true)]
        [InlineData(typeof(TestEnum), true)] // Enum should be valid
        [InlineData(typeof(object), false)]
        [InlineData(typeof(Dictionary<string, string>), false)]
        [InlineData(typeof(List<string>), false)]
        [InlineData(typeof(float), false)]
        [InlineData(typeof(char), false)]
        [InlineData(typeof(byte), false)]
        [InlineData(typeof(float[]), false)]
        [InlineData(typeof(object[]), false)]
        public void IsBedrockParameter_WithVariousTypes_ReturnsExpectedResult(Type type, bool expected)
        {
            // Act
            var result = _validator.IsBedrockParameter(type);
            
            // Assert
            Assert.Equal(expected, result);
        }

        private enum TestEnum
        {
            One,
            Two,
            Three
        }
    }
}
