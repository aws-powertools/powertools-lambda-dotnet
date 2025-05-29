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

        [Fact]
        public void Tool_WithCustomFailureResponse_ReturnsFailureState()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            resolver.Tool("CustomFailure", () => 
            {
                // Return a custom FAILURE response
                return new BedrockFunctionResponse
                {
                    Response = new Response
                    {
                        ActionGroup = "TestGroup",
                        Function = "CustomFailure",
                        FunctionResponse = new FunctionResponse
                        {
                            ResponseBody = new ResponseBody
                            {
                                Text = new TextBody 
                                { 
                                    Body = "Critical error occurred: Database unavailable" 
                                }
                            },
                            ResponseState = ResponseState.FAILURE  // Mark as FAILURE to abort the conversation
                        }
                    }
                };
            });
            
            var input = new BedrockFunctionRequest { Function = "CustomFailure" };
            var context = new TestLambdaContext();
            
            // Act
            var result = resolver.Resolve(input, context);
            
            // Assert
            Assert.Equal("Critical error occurred: Database unavailable", result.Response.FunctionResponse.ResponseBody.Text.Body);
            Assert.Equal("FAILURE", result.Response.FunctionResponse.ResponseState.ToString());
        }

        [Fact]
        public void Tool_WithSessionAttributesPersistence_MaintainsStateAcrossInvocations()
        {
            // Arrange
            var resolver = new BedrockAgentFunctionResolver();
            
            // Create a counter tool that reads and updates session attributes
            resolver.Tool("CounterTool", (BedrockFunctionRequest request) => 
            {
                // Read the current count from session attributes
                int currentCount = 0;
                if (request.SessionAttributes != null && 
                    request.SessionAttributes.TryGetValue("counter", out var countStr) &&
                    int.TryParse(countStr, out var count))
                {
                    currentCount = count;
                }
                
                // Increment the counter
                currentCount++;
                
                // Create a new dictionary with updated counter
                var updatedSessionAttributes = new Dictionary<string, string>(request.SessionAttributes ?? new Dictionary<string, string>())
                {
                    ["counter"] = currentCount.ToString(),
                    ["lastAccessed"] = DateTime.UtcNow.ToString("o")
                };

                // Return response with updated session attributes
                return new BedrockFunctionResponse
                {
                    Response = new Response
                    {
                        ActionGroup = request.ActionGroup,
                        Function = request.Function,
                        FunctionResponse = new FunctionResponse
                        {
                            ResponseBody = new ResponseBody
                            {
                                Text = new TextBody { Body = $"Current count: {currentCount}" }
                            }
                        }
                    },
                    SessionAttributes = updatedSessionAttributes,
                    PromptSessionAttributes = request.PromptSessionAttributes
                };
            });
            
            // First invocation - should start with 0 and increment to 1
            var firstInput = new BedrockFunctionRequest 
            { 
                Function = "CounterTool", 
                SessionAttributes = new Dictionary<string, string>(),
                PromptSessionAttributes = new Dictionary<string, string> { ["prompt"] = "initial" }
            };
            
            // Second invocation - should use the session attributes from first response
            var secondInput = new BedrockFunctionRequest { Function = "CounterTool" };
            
            // Act
            var firstResult = resolver.Resolve(firstInput);
            // In a real scenario, the agent would pass the updated session attributes back to us
            secondInput.SessionAttributes = firstResult.SessionAttributes;
            secondInput.PromptSessionAttributes = firstResult.PromptSessionAttributes;
            var secondResult = resolver.Resolve(secondInput);
            
            // Now a third invocation to verify the counter keeps incrementing
            var thirdInput = new BedrockFunctionRequest { Function = "CounterTool" };
            thirdInput.SessionAttributes = secondResult.SessionAttributes;
            thirdInput.PromptSessionAttributes = secondResult.PromptSessionAttributes;
            var thirdResult = resolver.Resolve(thirdInput);
            
            // Assert
            Assert.Equal("Current count: 1", firstResult.Response.FunctionResponse.ResponseBody.Text.Body);
            Assert.Equal("Current count: 2", secondResult.Response.FunctionResponse.ResponseBody.Text.Body);
            Assert.Equal("Current count: 3", thirdResult.Response.FunctionResponse.ResponseBody.Text.Body);
            
            // Verify session attributes are maintained
            Assert.Equal("1", firstResult.SessionAttributes["counter"]);
            Assert.Equal("2", secondResult.SessionAttributes["counter"]);
            Assert.Equal("3", thirdResult.SessionAttributes["counter"]);
            
            // Verify prompt attributes are preserved
            Assert.Equal("initial", firstResult.PromptSessionAttributes["prompt"]);
            Assert.Equal("initial", secondResult.PromptSessionAttributes["prompt"]);
            Assert.Equal("initial", thirdResult.PromptSessionAttributes["prompt"]);
        }
        
        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            
            public override string ToString() => $"{Name} (ID: {Id})";
        }
    }
}
