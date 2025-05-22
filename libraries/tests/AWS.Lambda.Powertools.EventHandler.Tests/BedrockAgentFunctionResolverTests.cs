using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.BedrockAgentRuntime.Model;
using Amazon.Lambda.TestUtilities;

namespace AWS.Lambda.Powertools.EventHandler.Tests;

public class BedrockAgentFunctionResolverTests
{
    private readonly ActionGroupInvocationInput _bedrockEvent;

    public BedrockAgentFunctionResolverTests()
    {
        _bedrockEvent = JsonSerializer.Deserialize<ActionGroupInvocationInput>(
            File.ReadAllText("bedrockFunctionEvent.json"),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            })!;
    }
    
    [Fact]
    public void TestFunctionHandlerWithNoParameters()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction", () => new ActionGroupInvocationOutput { Text = "Hello, World!" });

        var input = new ActionGroupInvocationInput { Function = "TestFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("Hello, World!", result.Text);
    }

    [Fact]
    public async Task TestFunctionHandlerWithNoParametersAsync()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction", () => new ActionGroupInvocationOutput { Text = "Hello, World!" });

        var input = new ActionGroupInvocationInput { Function = "TestFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = await resolver.ResolveAsync(input, context);

        // Assert
        Assert.Equal("Hello, World!", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithDescription()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction", () => new ActionGroupInvocationOutput { Text = "Hello, World!" },
            "This is a test function");

        var input = new ActionGroupInvocationInput { Function = "TestFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("Hello, World!", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithMultiplTools()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction1", () => new ActionGroupInvocationOutput { Text = "Hello from Function 1!" });
        resolver.Tool("TestFunction2", () => new ActionGroupInvocationOutput { Text = "Hello from Function 2!" });

        var input1 = new ActionGroupInvocationInput { Function = "TestFunction1" };
        var input2 = new ActionGroupInvocationInput { Function = "TestFunction2" };
        var context = new TestLambdaContext();

        // Act
        var result1 = resolver.Resolve(input1, context);
        var result2 = resolver.Resolve(input2, context);

        // Assert
        Assert.Equal("Hello from Function 1!", result1.Text);
        Assert.Equal("Hello from Function 2!", result2.Text);
    }


    [Fact]
    public void TestFunctionHandlerWithInput()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction",
            (input) => new ActionGroupInvocationOutput { Text = $"Hello, {input.Function}!" });

        var input = new ActionGroupInvocationInput { Function = "TestFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("Hello, TestFunction!", result.Text);
    }

    [Fact]
    public async Task TestFunctionHandlerWithInputAsync()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction",
            (input) => new ActionGroupInvocationOutput { Text = $"Hello, {input.Function}!" });

        var input = new ActionGroupInvocationInput { Function = "TestFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = await resolver.ResolveAsync(input, context);

        // Assert
        Assert.Equal("Hello, TestFunction!", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerNoToolMatch()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction", () => new ActionGroupInvocationOutput { Text = "Hello, World!" });

        var input = new ActionGroupInvocationInput { Function = "NonExistentFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("No handler registered for function: NonExistentFunction", result.Text);
    }

    [Fact]
    public async Task TestFunctionHandlerNoToolMatchAsync()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction", () => new ActionGroupInvocationOutput { Text = "Hello, World!" });

        var input = new ActionGroupInvocationInput { Function = "NonExistentFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = await resolver.ResolveAsync(input, context);

        // Assert
        Assert.Equal("No handler registered for function: NonExistentFunction", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithParameters()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction", () => new ActionGroupInvocationOutput { Text = "Hello, World!" });

        var input = new ActionGroupInvocationInput
        {
            Function = "TestFunction",
            RequestBody = new RequestBody
            {
                
            },
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "a",
                    Value = "1",
                    Type = "Number"
                },
                new Parameter
                {
                    Name = "b",
                    Value = "1",
                    Type = "Number"
                }
            }
        };
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("Hello, World!", result.Text);
    }
    
    [Fact]
    public void TestFunctionHandlerWithEvent()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("sum_numbers", (payload, context ) =>
        {
            
            return new ActionGroupInvocationOutput { Text = "2" };
        });
        
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(_bedrockEvent, context);

        // Assert
        Assert.Equal("2", result.Text);
    }
}

// Types
// String
// Number
// Integer
// Boolean
// Array