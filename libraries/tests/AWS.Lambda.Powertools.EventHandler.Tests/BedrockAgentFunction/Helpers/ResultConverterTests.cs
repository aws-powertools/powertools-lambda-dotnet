using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

namespace AWS.Lambda.Powertools.EventHandler.BedrockAgentFunction.Helpers
{
    public class ResultConverterTests
    {
        private readonly ResultConverter _converter = new();
        private readonly BedrockFunctionRequest _defaultInput = new()
        {
            Function = "TestFunction",
            ActionGroup = "TestGroup",
            SessionAttributes = new Dictionary<string, string> { { "testKey", "testValue" } },
            PromptSessionAttributes = new Dictionary<string, string> { { "promptKey", "promptValue" } }
        };
        private readonly string _functionName = "TestFunction";
        private readonly ILambdaContext _context = new TestLambdaContext();

        [Fact]
        public void ProcessResult_WithBedrockFunctionResponse_ReturnsUnchanged()
        {
            // Arrange
            var response = BedrockFunctionResponse.WithText(
                "Test response", 
                "TestGroup", 
                "TestFunction",
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                new Dictionary<string, string>());
            
            // Act
            var result = _converter.ProcessResult<BedrockFunctionResponse>(response, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Same(response, result);
        }
        
        [Fact]
        public void ProcessResult_WithNullValue_ReturnsEmptyResponse()
        {
            // Arrange
            object? nullValue = null;
            
            // Act
            var result = _converter.ProcessResult<object>(nullValue, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal(string.Empty, result.Response.FunctionResponse.ResponseBody.Text.Body);
            Assert.Equal(_defaultInput.ActionGroup, result.Response.ActionGroup);
            Assert.Equal(_defaultInput.Function, result.Response.Function);
        }
        
        [Fact]
        public void ProcessResult_WithStringValue_ReturnsTextResponse()
        {
            // Arrange
            var stringValue = "Hello, world!";
            
            // Act
            var result = _converter.ProcessResult<string>(stringValue, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal(stringValue, result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void ProcessResult_WithIntValue_ReturnsTextResponse()
        {
            // Arrange
            var intValue = 42;
            
            // Act
            var result = _converter.ProcessResult<int>(intValue, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal("42", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void ProcessResult_WithDecimalValue_ReturnsTextResponse()
        {
            // Arrange
            var decimalValue = 42.5m;
            
            // Act
            var result = _converter.ProcessResult<decimal>(decimalValue, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal("42.5", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void ProcessResult_WithBoolValue_ReturnsTextResponse()
        {
            // Arrange
            var boolValue = true;
            
            // Act
            var result = _converter.ProcessResult<bool>(boolValue, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal("True", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void ProcessResult_WithObjectValue_ReturnsToString()
        {
            // Arrange
            var testObject = new TestObject { Name = "Test", Value = 42 };
            
            // Act
            var result = _converter.ProcessResult<TestObject>(testObject, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal(testObject.ToString(), result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public async Task ProcessResult_WithTaskStringResult_ReturnsTextResponse()
        {
            // Arrange
            Task<string> task = Task.FromResult("Async result");
            
            // Act
            var result = _converter.ProcessResult<string>(task, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal("Async result", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public async Task ProcessResult_WithTaskIntResult_ReturnsTextResponse()
        {
            // Arrange
            Task<int> task = Task.FromResult(42);
            
            // Act
            var result = _converter.ProcessResult<int>(task, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal("42", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public async Task ProcessResult_WithTaskBoolResult_ReturnsTextResponse()
        {
            // Arrange
            Task<bool> task = Task.FromResult(true);
            
            // Act
            var result = _converter.ProcessResult<bool>(task, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal("True", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public async Task ProcessResult_WithVoidTask_ReturnsEmptyResponse()
        {
            // Arrange
            Task task = Task.CompletedTask;
            
            // Act
            var result = _converter.ProcessResult<object>(task, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal(string.Empty, result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public async Task ProcessResult_WithTaskBedrockResponse_ReturnsResponse()
        {
            // Arrange
            var response = BedrockFunctionResponse.WithText(
                "Async response", 
                "AsyncGroup", 
                "AsyncFunction",
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                new Dictionary<string, string>());
                
            Task<BedrockFunctionResponse> task = Task.FromResult(response);
            
            // Act
            var result = _converter.ProcessResult<BedrockFunctionResponse>(task, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal("Async response", result.Response.FunctionResponse.ResponseBody.Text.Body);
            Assert.Equal("AsyncGroup", result.Response.ActionGroup);
            Assert.Equal("AsyncFunction", result.Response.Function);
        }
        
        [Fact]
        public void EnsureResponseMetadata_WithEmptyMetadata_FillsFromInput()
        {
            // Arrange
            var response = BedrockFunctionResponse.WithText(
                "Test response", 
                "", // Empty action group
                "", // Empty function name
                _defaultInput.SessionAttributes,
                _defaultInput.PromptSessionAttributes,
                new Dictionary<string, string>());
                
            // Act
            var result = _converter.ConvertToOutput(response, _defaultInput);
            
            // Assert
            Assert.Equal("Test response", result.Response.FunctionResponse.ResponseBody.Text.Body);
            Assert.Equal(_defaultInput.ActionGroup, result.Response.ActionGroup); // Filled from input
            Assert.Equal(_defaultInput.Function, result.Response.Function); // Filled from input
        }
        
        [Fact]
        public void ConvertToOutput_PreservesSessionAttributes()
        {
            // Arrange
            var sessionAttributes = new Dictionary<string, string> { { "userID", "test123" } };
            var promptAttributes = new Dictionary<string, string> { { "context", "testing" } };
            
            var input = new BedrockFunctionRequest
            {
                Function = "TestFunction",
                ActionGroup = "TestGroup",
                SessionAttributes = sessionAttributes,
                PromptSessionAttributes = promptAttributes
            };
            
            // Act
            var result = _converter.ConvertToOutput("Test response", input);
            
            // Assert
            Assert.Equal(sessionAttributes, result.SessionAttributes);
            Assert.Equal(promptAttributes, result.PromptSessionAttributes);
        }
        
        [Fact]
        public void ProcessResult_WithLongValue_ReturnsTextResponse()
        {
            // Arrange
            long longValue = 9223372036854775807;
            
            // Act
            var result = _converter.ProcessResult<long>(longValue, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal("9223372036854775807", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        [Fact]
        public void ProcessResult_WithDoubleValue_ReturnsTextResponse()
        {
            // Arrange
            double doubleValue = 123.456;
            
            // Act
            var result = _converter.ProcessResult<double>(doubleValue, _defaultInput, _functionName, _context);
            
            // Assert
            Assert.Equal("123.456", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }

        private class TestObject
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
            
            public override string ToString()
            {
                return $"{Name}:{Value}";
            }
        }
    }
}
