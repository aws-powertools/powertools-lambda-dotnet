using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using NSubstitute;

namespace AWS.Lambda.Powertools.EventHandler.BedrockAgentFunction.Helpers
{
    public class ParameterMapperTests
    {
        private readonly ParameterMapper _mapper = new();

        [Fact]
        public void MapParameters_WithNoParameters_ReturnsEmptyArray()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.NoParameters))!;
            var input = new BedrockFunctionRequest();
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void MapParameters_WithLambdaContext_MapsCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithLambdaContext))!;
            var input = new BedrockFunctionRequest();
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Single(result);
            Assert.Same(context, result[0]);
        }

        [Fact]
        public void MapParameters_WithBedrockFunctionRequest_MapsCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithBedrockFunctionRequest))!;
            var input = new BedrockFunctionRequest();
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Single(result);
            Assert.Same(input, result[0]);
        }

        [Fact]
        public void MapParameters_WithStringParameter_MapsCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithStringParameter))!;
            var input = new BedrockFunctionRequest
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "name", Value = "TestValue", Type = "String" }
                }
            };
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Single(result);
            Assert.Equal("TestValue", result[0]);
        }

        [Fact]
        public void MapParameters_WithIntParameter_MapsCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithIntParameter))!;
            var input = new BedrockFunctionRequest
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "value", Value = "42", Type = "Number" }
                }
            };
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Single(result);
            Assert.Equal(42, result[0]);
        }

        [Fact]
        public void MapParameters_WithBoolParameter_MapsCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithBoolParameter))!;
            var input = new BedrockFunctionRequest
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "flag", Value = "true", Type = "Boolean" }
                }
            };
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Single(result);
            Assert.True((bool)result[0]!);
        }

        [Fact]
        public void MapParameters_WithEnumParameter_MapsCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithEnumParameter))!;
            var input = new BedrockFunctionRequest
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "testEnum", Value = "Option2", Type = "String" }
                }
            };
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Single(result);
            Assert.Equal(TestEnum.Option2, result[0]);
        }

        [Fact]
        public void MapParameters_WithStringArrayParameter_MapsCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithStringArrayParameter))!;
            var input = new BedrockFunctionRequest
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "values", Value = "[\"one\",\"two\",\"three\"]", Type = "String" }
                }
            };
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Single(result);
            var array = (string[])result[0]!;
            Assert.Equal(3, array.Length);
            Assert.Equal("one", array[0]);
            Assert.Equal("two", array[1]);
            Assert.Equal("three", array[2]);
        }

        [Fact]
        public void MapParameters_WithIntArrayParameter_MapsCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithIntArrayParameter))!;
            var input = new BedrockFunctionRequest
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "values", Value = "[1,2,3]", Type = "String" }
                }
            };
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Single(result);
            var array = (int[])result[0]!;
            Assert.Equal(3, array.Length);
            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
            Assert.Equal(3, array[2]);
        }

        [Fact]
        public void MapParameters_WithInvalidJsonArray_ReturnsNull()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithStringArrayParameter))!;
            var input = new BedrockFunctionRequest
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "values", Value = "[invalid json]", Type = "String" }
                }
            };
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Single(result);
            Assert.Null(result[0]);
        }

        [Fact]
        public void MapParameters_WithServiceProvider_ResolvesService()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithDependencyInjection))!;
            var input = new BedrockFunctionRequest();
            var context = new TestLambdaContext();
            
            // Create a test service
            var testService = new TestService();
            
            // Setup service provider
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(ITestService)).Returns(testService);

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, serviceProvider);

            // Assert
            Assert.Equal(3, result.Length);
            Assert.Same(context, result[0]);
            Assert.Same(input, result[1]);
            Assert.Same(testService, result[2]);
        }

        [Fact]
        public void MapParameters_WithMultipleParameterTypes_MapsAllCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestMethodsClass).GetMethod(nameof(TestMethodsClass.WithMultipleParameterTypes))!;
            var input = new BedrockFunctionRequest
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "name", Value = "TestUser", Type = "String" },
                    new() { Name = "age", Value = "30", Type = "Number" },
                    new() { Name = "isActive", Value = "true", Type = "Boolean" }
                }
            };
            var context = new TestLambdaContext();

            // Act
            var result = _mapper.MapParameters(methodInfo, input, context, null);

            // Assert
            Assert.Equal(4, result.Length);
            Assert.Equal("TestUser", result[0]);
            Assert.Equal(30, result[1]);
            Assert.True((bool)result[2]!);
            Assert.Same(context, result[3]);
        }

        public class TestMethodsClass
        {
            public void NoParameters() { }
            
            public void WithLambdaContext(ILambdaContext context) { }
            
            public void WithBedrockFunctionRequest(BedrockFunctionRequest request) { }
            
            public void WithStringParameter(string name) { }
            
            public void WithIntParameter(int value) { }
            
            public void WithBoolParameter(bool flag) { }
            
            public void WithEnumParameter(TestEnum testEnum) { }
            
            public void WithStringArrayParameter(string[] values) { }
            
            public void WithIntArrayParameter(int[] values) { }
            
            public void WithDependencyInjection(ILambdaContext context, BedrockFunctionRequest request, ITestService service) { }
            
            public void WithMultipleParameterTypes(string name, int age, bool isActive, ILambdaContext context) { }
        }

        public interface ITestService { }
        
        public class TestService : ITestService { }

        public enum TestEnum
        {
            Option1,
            Option2,
            Option3
        }
    }
}
