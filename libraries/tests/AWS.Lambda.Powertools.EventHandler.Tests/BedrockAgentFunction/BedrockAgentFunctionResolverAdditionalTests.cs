using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

namespace AWS.Lambda.Powertools.EventHandler.BedrockAgentFunction
{
    public class BedrockAgentFunctionResolverAdditionalTests
    {
        [Fact]
        public async Task ResolveAsync_WithValidInput_ReturnsResult()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            resolver.Tool("AsyncTest", () => "Async result");
            
            var input = new BedrockFunctionRequest { Function = "AsyncTest" };
            var context = new TestLambdaContext();
            
            // Act
            var result = await resolver.ResolveAsync(input, context);
            
            // Assert
            Assert.Equal("Async result", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void Tool_WithNullHandler_ThrowsException()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            Func<string> nullHandler = null!;
            
            // Act/Assert
            Assert.Throws<ArgumentNullException>(() => resolver.Tool("NullTest", nullHandler));
        }
        
        [Fact]
        public void Resolve_WithNullFunction_ReturnsErrorResponse()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            var input = new BedrockFunctionRequest { Function = null };
            
            // Act
            var result = resolver.Resolve(input);
            
            // Assert
            Assert.Equal("No tool specified in the request", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void Resolve_WithEmptyFunction_ReturnsErrorResponse()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            var input = new BedrockFunctionRequest { Function = "" };
            
            // Act
            var result = resolver.Resolve(input);
            
            // Assert
            Assert.Equal("No tool specified in the request", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void Tool_WithHandlerThrowingException_ReturnsErrorResponse()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            resolver.Tool("ExceptionTest", (BedrockFunctionRequest input, ILambdaContext ctx) => { 
                throw new InvalidOperationException("Handler exception"); 
                return new BedrockFunctionResponse(); 
            });
            
            var input = new BedrockFunctionRequest { Function = "ExceptionTest" };
            
            // Act
            var result = resolver.Resolve(input);
            
            // Assert
            Assert.Equal("Error when invoking tool: Handler exception", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact] 
        public void Tool_WithDynamicInvokeException_ReturnsErrorResponse()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            resolver.Tool<string>("ExceptionTest", (Func<string>)(() => { 
                throw new InvalidOperationException("Dynamic invoke exception"); 
            }));
            
            var input = new BedrockFunctionRequest { Function = "ExceptionTest" };
            
            // Act
            var result = resolver.Resolve(input);
            
            // Assert
            Assert.Contains("Error when invoking tool", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void Tool_ObjectFunctionRegistration_ReturnsObjectAsString()
        {
            // Arrange
            var testObject = new TestObject { Id = 123, Name = "Test" };
            var resolver = new BedrockAgentFunctionResolver();
            resolver.Tool("ObjectTest", () => testObject);
            
            var input = new BedrockFunctionRequest { Function = "ObjectTest" };
            
            // Act
            var result = resolver.Resolve(input);
            
            // Assert
            Assert.Equal(testObject.ToString(), result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public async Task Resolve_WithAsyncTask_HandlesCorrectly()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            resolver.Tool("AsyncTaskTest", async (string message) => {
                await Task.Delay(10); // Simulate async work
                return $"Processed: {message}";
            });
            
            var input = new BedrockFunctionRequest { 
                Function = "AsyncTaskTest",
                Parameters = new List<Parameter> {
                    new Parameter { Name = "message", Value = "hello", Type = "String" }
                }
            };
            
            // Act
            var result = resolver.Resolve(input);
            
            // Assert
            Assert.Equal("Processed: hello", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void Tool_WithBedrockFunctionResponseHandlerNoContext_MapsCorrectly()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            resolver.Tool("NoContextTest", (BedrockFunctionRequest request) => new BedrockFunctionResponse
            {
                Response = new Response
                {
                    ActionGroup = "TestGroup",
                    Function = "NoContextTest",
                    FunctionResponse = new FunctionResponse
                    {
                        ResponseBody = new ResponseBody
                        {
                            Text = new TextBody { Body = "No context needed" }
                        }
                    }
                }
            });
            
            var input = new BedrockFunctionRequest { Function = "NoContextTest" };
            
            // Act
            var result = resolver.Resolve(input);
            
            // Assert
            Assert.Equal("No context needed", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void Tool_WithBedrockFunctionResponseHandler_MapsCorrectly()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            resolver.Tool("ResponseTest", () => new BedrockFunctionResponse
            {
                Response = new Response
                {
                    ActionGroup = "TestGroup",
                    Function = "ResponseTest",
                    FunctionResponse = new FunctionResponse
                    {
                        ResponseBody = new ResponseBody
                        {
                            Text = new TextBody { Body = "Direct response" }
                        }
                    }
                }
            });
            
            var input = new BedrockFunctionRequest { Function = "ResponseTest" };
            
            // Act
            var result = resolver.Resolve(input);
            
            // Assert
            Assert.Equal("Direct response", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            
            public override string ToString() => $"{Name} (ID: {Id})";
        }
    }
}
