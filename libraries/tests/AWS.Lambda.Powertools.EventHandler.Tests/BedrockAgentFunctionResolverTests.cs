using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.BedrockAgentRuntime.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0162 // Unreachable code detected

// ReSharper disable once CheckNamespace
namespace AWS.Lambda.Powertools.EventHandler.Resolvers.Tests;

public class BedrockAgentFunctionResolverTests
{
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
            (input, context) => new ActionGroupInvocationOutput { Text = $"Hello, {input.Function}!" });

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
            input => new ActionGroupInvocationOutput { Text = $"Hello, {input.Function}!" });

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
        resolver.Tool(
            name: "GetCustomForecast",
            description: "Get detailed forecast for a location",
            handler: (string location, int days, ILambdaContext ctx) =>
            {
                ctx.Logger.LogLine($"Getting forecast for {location}");
                return $"{days}-day forecast for {location}";
            }
        );

        resolver.Tool(
            name: "Greet",
            description: "Greet a user",
            handler: (string name) => { return $"Hello {name}"; }
        );

        resolver.Tool(
            name: "Simple",
            description: "Greet a user",
            handler: () => { return "Hello"; }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "GetCustomForecast",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "location",
                    Value = "Lisbon",
                    Type = "String"
                },
                new Parameter
                {
                    Name = "days",
                    Value = "1",
                    Type = "Number"
                }
            }
        };

        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("1-day forecast for Lisbon", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithEventAndServices()
    {
        // Setup DI
        var services = new ServiceCollection();
        services.AddSingleton<IMyInterface>(new MyImplementation());
        services.AddBedrockResolver();

        var serviceProvider = services.BuildServiceProvider();
        var resolver = serviceProvider.GetRequiredService<BedrockAgentFunctionResolver>();

        resolver.Tool(
            name: "GetCustomForecast",
            description: "Get detailed forecast for a location",
            handler: async (string location, int days, IMyInterface client, ILambdaContext ctx) =>
            {
                var resp = await client.DoSomething(location, days);
                return resp;
            }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "GetCustomForecast",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "location",
                    Value = "Lisbon",
                    Type = "String"
                },
                new Parameter
                {
                    Name = "days",
                    Value = "1",
                    Type = "Number"
                }
            }
        };

        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("Forecast for Lisbon for 1 days", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithEventTypes()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "GetCustomForecast",
            description: "Get detailed forecast for a location",
            handler: (string location, int days, ILambdaContext ctx) =>
            {
                ctx.Logger.LogLine($"Getting forecast for {location}");
                return $"{days}-day forecast for {location}";
            }
        );

        resolver.Tool(
            name: "Greet",
            description: "Greet a user",
            handler: (string name) => { return $"Hello {name}"; }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "GetCustomForecast",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "location",
                    Value = "Lisbon",
                    Type = "String"
                },
                new Parameter
                {
                    Name = "days",
                    Value = "1",
                    Type = "Number"
                }
            }
        };

        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("1-day forecast for Lisbon", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithBooleanParameter()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "TestBool",
            description: "Test boolean parameter",
            handler: (bool isEnabled) => { return $"Feature is {(isEnabled ? "enabled" : "disabled")}"; }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "TestBool",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "isEnabled",
                    Value = "true",
                    Type = "Boolean"
                }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("Feature is enabled", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithMissingRequiredParameter()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "RequiredParam",
            description: "Function with required parameter",
            handler: (string name) => $"Hello, {name}!"
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "RequiredParam",
            Parameters = new List<Parameter>() // Empty parameters
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Contains("Hello, !", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithMultipleParameterTypes()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "ComplexFunction",
            description: "Test multiple parameter types",
            handler: (string name, int count, bool isActive) =>
            {
                return $"Name: {name}, Count: {count}, Active: {isActive}";
            }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "ComplexFunction",
            Parameters = new List<Parameter>
            {
                new Parameter { Name = "name", Value = "Test", Type = "String" },
                new Parameter { Name = "count", Value = "5", Type = "Integer" },
                new Parameter { Name = "isActive", Value = "true", Type = "Boolean" }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("Name: Test, Count: 5, Active: True", result.Text);
    }

    public enum TestEnum
    {
        Option1,
        Option2,
        Option3
    }

    [Fact]
    public void TestFunctionHandlerWithEnumParameter()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "EnumTest",
            description: "Test enum parameter",
            handler: (TestEnum option) => { return $"Selected option: {option}"; }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "EnumTest",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "option",
                    Value = "Option2",
                    Type = "String" // Enums come as strings
                }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("Selected option: Option2", result.Text);
    }

    [Fact]
    public void TestParameterNameCaseSensitivity()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "CaseTest",
            description: "Test case sensitivity",
            handler: (string userName) => $"Hello, {userName}!"
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "CaseTest",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "UserName", // Different case than parameter
                    Value = "John",
                    Type = "String"
                }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("Hello, John!", result.Text);
    }

    [Fact]
    public void TestParameterOrderIndependence()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "OrderTest",
            description: "Test parameter order independence",
            handler: (string firstName, string lastName) => { return $"Name: {firstName} {lastName}"; }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "OrderTest",
            Parameters = new List<Parameter>
            {
                // Parameters in reverse order of handler parameters
                new Parameter { Name = "lastName", Value = "Smith", Type = "String" },
                new Parameter { Name = "firstName", Value = "John", Type = "String" }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("Name: John Smith", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithDecimalParameter()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "PriceCalculator",
            description: "Calculate total price with tax",
            handler: (decimal price) =>
            {
                var withTax = price * 1.2m;
                return $"Total price with tax: {withTax.ToString("F2", CultureInfo.InvariantCulture)}";
            }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "PriceCalculator",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "price",
                    Value = "29.99",
                    Type = "Number"
                }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Contains("35.99", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithArrayParameter()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "ArrayTest",
            description: "Test with array parameter",
            handler: (string text) =>
            {
                // In a real implementation, you'd parse the array from the string
                // ActionGroupInvocationInput doesn't directly support array types
                return $"Received: {text}";
            }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "ArrayTest",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "text",
                    Value = "[\"item1\",\"item2\"]", // Array as JSON string
                    Type = "Array"
                }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("Received: [\"item1\",\"item2\"]", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithStringArrayParameter()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "ProcessWorkout",
            description: "Process workout exercises",
            handler: (string[] exercises) =>
            {
                var result = new StringBuilder();
                result.AppendLine("Your workout plan:");

                for (int i = 0; i < exercises.Length; i++)
                {
                    result.AppendLine($"  {i + 1}. {exercises[i]}");
                }

                return result.ToString();
            }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "ProcessWorkout",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "exercises",
                    Value =
                        "[\"Squats, 3 sets of 10 reps\",\"Push-ups, 3 sets of 10 reps\",\"Plank, 3 sets of 30 seconds\"]",
                    Type = "String" // The type is String since it contains JSON
                }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Contains("Your workout plan:", result.Text);
        Assert.Contains("1. Squats, 3 sets of 10 reps", result.Text);
        Assert.Contains("2. Push-ups, 3 sets of 10 reps", result.Text);
        Assert.Contains("3. Plank, 3 sets of 30 seconds", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithStringArrayParameterManualParse()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "ProcessWorkout",
            description: "Process workout exercises",
            handler: (ActionGroupInvocationInput input) =>
            {
                // Manual array parsing since the resolver doesn't natively support arrays
                var exercisesJson = input.Parameters.FirstOrDefault(p => p.Name == "exercises")?.Value ?? "[]";

                // Parse JSON array
                var exercises = JsonSerializer.Deserialize<string[]>(exercisesJson);

                // Process the array items
                var result = new StringBuilder();
                result.AppendLine("Your workout plan:");

                if (exercises != null)
                {
                    for (int i = 0; i < exercises.Length; i++)
                    {
                        result.AppendLine($"  {i + 1}. {exercises[i]}");
                    }
                }

                return result.ToString();
            }
        );

        var input = new ActionGroupInvocationInput
        {
            Function = "ProcessWorkout",
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = "exercises",
                    Value =
                        "[\"Squats, 3 sets of 10 reps\",\"Push-ups, 3 sets of 10 reps\",\"Plank, 3 sets of 30 seconds\"]",
                    Type = "String" // The type is still String even though it contains JSON
                }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Contains("Your workout plan:", result.Text);
        Assert.Contains("1. Squats, 3 sets of 10 reps", result.Text);
        Assert.Contains("2. Push-ups, 3 sets of 10 reps", result.Text);
        Assert.Contains("3. Plank, 3 sets of 30 seconds", result.Text);
    }
    
    [Fact]
    public async Task TestPayload2()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("getWeatherForCity", "Get weather for a specific city", async (string city, ILambdaContext context) =>
        {
            return await Task.FromResult(city);
        });

        var input = JsonSerializer.Deserialize<ActionGroupInvocationInput>(
            File.ReadAllText("bedrockFunctionEvent2.json"),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            })!;

        // Act
        var result = await resolver.ResolveAsync(input);

        // Assert
        Assert.Equal("Lisbon", result.Text);
    }

    [Fact]
    public void TestFunctionHandlerWithExceptionInHandler()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "ThrowingFunction",
            description: "Function that throws exception",
            handler: () =>
            {
                throw new InvalidOperationException("Test error");
                return "This will not run:";
            }
        );

        var input = new ActionGroupInvocationInput { Function = "ThrowingFunction" };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Contains("Error executing function", result.Text);
    }
}

internal interface IMyInterface
{
    Task<string> DoSomething(string location, int days);
}

internal class MyImplementation : IMyInterface
{
    public async Task<string> DoSomething(string location, int days)
    {
        return await Task.FromResult($"Forecast for {location} for {days} days");
    }
}