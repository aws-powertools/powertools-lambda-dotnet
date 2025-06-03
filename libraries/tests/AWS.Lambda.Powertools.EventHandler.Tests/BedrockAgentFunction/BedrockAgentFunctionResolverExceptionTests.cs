using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

namespace AWS.Lambda.Powertools.EventHandler.BedrockAgentFunction
{
    public class BedrockAgentFunctionResolverExceptionTests
    {
        [Fact]
        public void RegisterToolHandler_WithParameterMappingException_ReturnsErrorResponse()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            
            // Register a tool that requires a complex parameter that can't be mapped automatically
            resolver.Tool<string>("ComplexTest", (TestComplexType complex) => $"Name: {complex.Name}");
            
            var input = new BedrockFunctionRequest 
            { 
                Function = "ComplexTest",
                Parameters = new List<Parameter>
                {
                    // This parameter can't be automatically mapped to the complex type
                    new Parameter { Name = "complex", Value = "{\"name\":\"Test\"}", Type = "String" }
                }
            };
            var context = new TestLambdaContext();
            
            // Act
            var result = resolver.Resolve(input, context);
            
            // Assert
            // This should trigger the parameter mapping exception path
            Assert.Contains("Error when invoking tool:", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void RegisterToolHandler_WithNestedExceptionInDelegateInvoke_HandlesCorrectly()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            
            // Register a tool with a delegate that will throw an exception with inner exception
            resolver.Tool("NestedExceptionTest", () => {
                throw new AggregateException("Outer exception", 
                    new ApplicationException("Inner exception message"));
                return "Should not reach here";
            });
            
            var input = new BedrockFunctionRequest { Function = "NestedExceptionTest" };
            var context = new TestLambdaContext();
            
            // Act
            var result = resolver.Resolve(input, context);
            
            // Assert
            // The error should contain the inner exception message
            Assert.Contains("Inner exception message", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        // A test complex type that can't be automatically mapped from parameters
        private class TestComplexType
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
        }
    }
}
