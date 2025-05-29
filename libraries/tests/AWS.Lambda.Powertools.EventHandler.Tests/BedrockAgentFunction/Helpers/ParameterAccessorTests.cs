using AWS.Lambda.Powertools.EventHandler.Resolvers;

namespace AWS.Lambda.Powertools.EventHandler.BedrockAgentFunction.Helpers
{
    public class ParameterAccessorTests
    {
        [Fact]
        public void Get_WithStringParameter_ReturnsValue()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new Parameter { Name = "name", Value = "TestValue", Type = "String" }
            };
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var result = accessor.Get<string>("name");
            
            // Assert
            Assert.Equal("TestValue", result);
        }
        
        [Fact]
        public void Get_WithIntParameter_ReturnsValue()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new Parameter { Name = "age", Value = "30", Type = "Number" }
            };
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var result = accessor.Get<int>("age");
            
            // Assert
            Assert.Equal(30, result);
        }
        
        [Fact]
        public void Get_WithBoolParameter_ReturnsValue()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new Parameter { Name = "active", Value = "true", Type = "Boolean" }
            };
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var result = accessor.Get<bool>("active");
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public void Get_WithLongParameter_ReturnsValue()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new Parameter { Name = "bigNumber", Value = "9223372036854775807", Type = "Number" }
            };
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var result = accessor.Get<long>("bigNumber");
            
            // Assert
            Assert.Equal(9223372036854775807, result);
        }
        
        [Fact]
        public void Get_WithDoubleParameter_ReturnsValue()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new Parameter { Name = "price", Value = "99.99", Type = "Number" }
            };
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var result = accessor.Get<double>("price");
            
            // Assert
            Assert.Equal(99.99, result);
        }
        
        [Fact]
        public void Get_WithDecimalParameter_ReturnsValue()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new Parameter { Name = "amount", Value = "123.456", Type = "Number" }
            };
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var result = accessor.Get<decimal>("amount");
            
            // Assert
            Assert.Equal(123.456m, result);
        }
        
        [Fact]
        public void Get_WithNonExistentParameter_ReturnsDefault()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new Parameter { Name = "existing", Value = "value", Type = "String" }
            };
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var stringResult = accessor.Get<string>("nonExistent");
            var intResult = accessor.Get<int>("nonExistent");
            var boolResult = accessor.Get<bool>("nonExistent");
            
            // Assert
            Assert.Null(stringResult);
            Assert.Equal(0, intResult);
            Assert.False(boolResult);
        }
        
        [Fact]
        public void Get_WithCaseSensitivity_WorksCaseInsensitively()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new Parameter { Name = "userName", Value = "John", Type = "String" }
            };
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var result1 = accessor.Get<string>("userName");
            var result2 = accessor.Get<string>("UserName");
            var result3 = accessor.Get<string>("USERNAME");
            
            // Assert
            Assert.Equal("John", result1);
            Assert.Equal("John", result2);
            Assert.Equal("John", result3);
        }
        
        [Fact]
        public void Get_WithNullParameters_ReturnsDefault()
        {
            // Arrange
            var accessor = new ParameterAccessor(null);
            
            // Act
            var stringResult = accessor.Get<string>("any");
            var intResult = accessor.Get<int>("any");
            
            // Assert
            Assert.Null(stringResult);
            Assert.Equal(0, intResult);
        }
        
        [Fact]
        public void Get_WithInvalidType_ReturnsDefault()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new Parameter { Name = "number", Value = "not-a-number", Type = "Number" }
            };
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var result = accessor.Get<int>("number");
            
            // Assert
            Assert.Equal(0, result);
        }
        
        [Fact]
        public void Get_WithEmptyParameters_ReturnsDefault()
        {
            // Arrange
            var parameters = new List<Parameter>();
            var accessor = new ParameterAccessor(parameters);
            
            // Act
            var result = accessor.Get<string>("anything");
            
            // Assert
            Assert.Null(result);
        }
    }
}
