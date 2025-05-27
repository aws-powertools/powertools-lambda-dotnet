using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
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
        resolver.Tool("TestFunction", () => new BedrockFunctionResponse
        {
            Response = new Response
            {
                ActionGroup = "TestGroup",
                Function = "TestFunction",
                FunctionResponse = new FunctionResponse
                {
                    ResponseBody = new ResponseBody
                    {
                        Text = new TextBody { Body = "Hello, World!" }
                    }
                }
            }
        });

        var input = new BedrockFunctionRequest { Function = "TestFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("Hello, World!", result.Response.FunctionResponse.ResponseBody.Text.Body);
    }

    [Fact]
    public void TestFunctionHandlerWithDescription()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction", () => new BedrockFunctionResponse
            {
                Response = new Response
                {
                    ActionGroup = "TestGroup",
                    Function = "TestFunction",
                    FunctionResponse = new FunctionResponse
                    {
                        ResponseBody = new ResponseBody
                        {
                            Text = new TextBody { Body = "Hello, World!" }
                        }
                    }
                }
            },
            "This is a test function");

        var input = new BedrockFunctionRequest { Function = "TestFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("Hello, World!", result.Response.FunctionResponse.ResponseBody.Text.Body);
    }

    [Fact]
    public void TestFunctionHandlerWithMultiplTools()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        
        resolver.Tool("TestFunction1", () => new BedrockFunctionResponse
        {
            Response = new Response
            {
                ActionGroup = "TestGroup",
                Function = "TestFunction",
                FunctionResponse = new FunctionResponse
                {
                    ResponseBody = new ResponseBody
                    {
                        Text = new TextBody { Body = "Hello from Function 1!" }
                    }
                }
            }
        });
        resolver.Tool("TestFunction2", () => new BedrockFunctionResponse
        {
            Response = new Response
            {
                ActionGroup = "TestGroup",
                Function = "TestFunction",
                FunctionResponse = new FunctionResponse
                {
                    ResponseBody = new ResponseBody
                    {
                        Text = new TextBody { Body = "Hello from Function 2!" }
                    }
                }
            }
        });

        var input1 = new BedrockFunctionRequest { Function = "TestFunction1" };
        var input2 = new BedrockFunctionRequest { Function = "TestFunction2" };
        var context = new TestLambdaContext();

        // Act
        var result1 = resolver.Resolve(input1, context);
        var result2 = resolver.Resolve(input2, context);

        // Assert
        Assert.Equal("Hello from Function 1!", result1.Response.FunctionResponse.ResponseBody.Text.Body);
        Assert.Equal("Hello from Function 2!", result2.Response.FunctionResponse.ResponseBody.Text.Body);
    }
    
    [Fact]
    public void TestFunctionHandlerWithMultiplToolsDuplicate()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction1", () => new BedrockFunctionResponse
        {
            Response = new Response
            {
                ActionGroup = "TestGroup",
                Function = "TestFunction",
                FunctionResponse = new FunctionResponse
                {
                    ResponseBody = new ResponseBody
                    {
                        Text = new TextBody { Body = "Hello from Function 1!" }
                    }
                }
            }
        });
        resolver.Tool("TestFunction1", () => new BedrockFunctionResponse
        {
            Response = new Response
            {
                ActionGroup = "TestGroup",
                Function = "TestFunction",
                FunctionResponse = new FunctionResponse
                {
                    ResponseBody = new ResponseBody
                    {
                        Text = new TextBody { Body = "Hello from Function 2!" }
                    }
                }
            }
        });

        var input1 = new BedrockFunctionRequest { Function = "TestFunction1" };
        var input2 = new BedrockFunctionRequest { Function = "TestFunction1" };
        var context = new TestLambdaContext();

        // Act
        var result1 = resolver.Resolve(input1, context);
        var result2 = resolver.Resolve(input2, context);

        // Assert
        Assert.Equal("Hello from Function 2!", result1.Response.FunctionResponse.ResponseBody.Text.Body);
        Assert.Equal("Hello from Function 2!", result2.Response.FunctionResponse.ResponseBody.Text.Body);
    }


    [Fact]
    public void TestFunctionHandlerWithInput()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction",
            (input, context) => new BedrockFunctionResponse
            {
                Response = new Response
                {
                    ActionGroup = "TestGroup",
                    Function = "TestFunction",
                    FunctionResponse = new FunctionResponse
                    {
                        ResponseBody = new ResponseBody
                        {
                            Text = new TextBody { Body = $"Hello, {input.Function}!" }
                        }
                    }
                }
            });

        var input = new BedrockFunctionRequest { Function = "TestFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal("Hello, TestFunction!", result.Response.FunctionResponse.ResponseBody.Text.Body);
    }

    [Fact]
    public void TestFunctionHandlerNoToolMatch()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("TestFunction", () => new BedrockFunctionResponse
        {
            Response = new Response
            {
                ActionGroup = "TestGroup",
                Function = "TestFunction",
                FunctionResponse = new FunctionResponse
                {
                    ResponseBody = new ResponseBody
                    {
                        Text = new TextBody { Body = "Hello, World!" }
                    }
                }
            }
        });

        var input = new BedrockFunctionRequest { Function = "NonExistentFunction" };
        var context = new TestLambdaContext();

        // Act
        var result = resolver.Resolve(input, context);

        // Assert
        Assert.Equal($"Error: Tool {input.Function} has not been registered in handler",
            result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
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
        Assert.Equal("1-day forecast for Lisbon", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
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
        Assert.Equal("Forecast for Lisbon for 1 days", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
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
        Assert.Equal("Feature is enabled", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
        {
            Function = "RequiredParam",
            Parameters = new List<Parameter>() // Empty parameters
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Contains("Hello, !", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
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
        Assert.Equal("Name: Test, Count: 5, Active: True", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
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
        Assert.Equal("Selected option: Option2", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
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
        Assert.Equal("Hello, John!", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
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
        Assert.Equal("Name: John Smith", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
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
        Assert.Contains("35.99", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest
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
        Assert.Contains("Your workout plan:", result.Response.FunctionResponse.ResponseBody.Text.Body);
        Assert.Contains("1. Squats, 3 sets of 10 reps", result.Response.FunctionResponse.ResponseBody.Text.Body);
        Assert.Contains("2. Push-ups, 3 sets of 10 reps", result.Response.FunctionResponse.ResponseBody.Text.Body);
        Assert.Contains("3. Plank, 3 sets of 30 seconds", result.Response.FunctionResponse.ResponseBody.Text.Body);
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

        var input = new BedrockFunctionRequest { Function = "ThrowingFunction" };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Contains("Error when invoking tool: Test error", result.Response.FunctionResponse.ResponseBody.Text.Body);
    }

    [Fact]
    public void TestSessionAttributesPreservation()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "SessionTest",
            description: "Test session attributes preservation",
            handler: (string message) => message
        );

        var input = new BedrockFunctionRequest 
        { 
            Function = "SessionTest",
            ActionGroup = "TestGroup",
            Parameters = new List<Parameter>
            {
                new Parameter { Name = "message", Value = "Hello", Type = "String" }
            },
            SessionAttributes = new Dictionary<string, string>
            {
                { "userId", "12345" },
                { "preferredLanguage", "en-US" }
            },
            PromptSessionAttributes = new Dictionary<string, string>
            {
                { "context", "customer_support" },
                { "previousQuestion", "How do I reset my password?" }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("Hello", result.Response.FunctionResponse.ResponseBody.Text.Body);
        Assert.Equal(2, result.SessionAttributes.Count);
        Assert.Equal("12345", result.SessionAttributes["userId"]);
        Assert.Equal("en-US", result.SessionAttributes["preferredLanguage"]);
        Assert.Equal(2, result.PromptSessionAttributes.Count);
        Assert.Equal("customer_support", result.PromptSessionAttributes["context"]);
        Assert.Equal("How do I reset my password?", result.PromptSessionAttributes["previousQuestion"]);
    }

    [Fact]
    public void TestSessionAttributesPreservationWithErrorHandling()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "ErrorTest",
            description: "Test session attributes preservation with error",
            handler: () => { throw new Exception("Test error"); return "This will not run"; }
        );

        var input = new BedrockFunctionRequest 
        { 
            Function = "ErrorTest",
            ActionGroup = "TestGroup",
            SessionAttributes = new Dictionary<string, string>
            {
                { "userId", "12345" },
                { "session", "active" }
            },
            PromptSessionAttributes = new Dictionary<string, string>
            {
                { "lastAction", "login" }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Contains("Error when invoking tool: Test error", result.Response.FunctionResponse.ResponseBody.Text.Body);
        Assert.Equal(2, result.SessionAttributes.Count);
        Assert.Equal("12345", result.SessionAttributes["userId"]);
        Assert.Equal("active", result.SessionAttributes["session"]);
        Assert.Equal(1, result.PromptSessionAttributes?.Count);
        Assert.Equal("login", result.PromptSessionAttributes?["lastAction"]);
    }

    [Fact]
    public void TestSessionAttributesPreservationWithNoToolMatch()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        
        var input = new BedrockFunctionRequest 
        { 
            Function = "NonExistentTool",
            SessionAttributes = new Dictionary<string, string>
            {
                { "preferredTheme", "dark" }
            },
            PromptSessionAttributes = new Dictionary<string, string>
            {
                { "lastVisited", "homepage" }
            }
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Contains($"Error: Tool {input.Function} has not been registered in handler", result.Response.FunctionResponse.ResponseBody.Text.Body);
        Assert.Equal(1, result.SessionAttributes?.Count);
        Assert.Equal("dark", result.SessionAttributes?["preferredTheme"]);
        Assert.Equal(1, result.PromptSessionAttributes?.Count);
        Assert.Equal("homepage", result.PromptSessionAttributes?["lastVisited"]);
    }
    
    [Fact]
    public void TestSReturningNull()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool(
            name: "NullTest",
            description: "Test session attributes preservation with error",
            handler: () =>
            {
                string test = null!;
                return test;
            }
        );
        
        var input = new BedrockFunctionRequest 
        { 
            Function = "NullTest",
        };

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("", result.Response.FunctionResponse.ResponseBody.Text.Body);
    }

    [Fact]
    public void TestMaximumToolLimit()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        
        // Register 5 tools (the maximum)
        for (int i = 1; i <= 5; i++)
        {
            var toolName = $"Tool{i}";
            var response = $"Response from {toolName}";
            resolver.Tool(toolName, () => response);
            
            // Verify each tool works as it's registered
            var testInput = new BedrockFunctionRequest { Function = toolName };
            var testResult = resolver.Resolve(testInput);
            Assert.Contains(response, testResult.Response.FunctionResponse.ResponseBody.Text.Body);
        }
        
        // Try to register a 6th tool that should not be registered
        resolver.Tool("Tool6", () => "This should not be registered");
        
        // Verify the 6th tool doesn't work
        var input6 = new BedrockFunctionRequest { Function = "Tool6" };
        var result6 = resolver.Resolve(input6);
        
        // 6th tool should not be registered
        Assert.Contains("has not been registered", result6.Response.FunctionResponse.ResponseBody.Text.Body);
        
        // Double-check that the original 5 tools still work
        for (int i = 1; i <= 5; i++)
        {
            var toolName = $"Tool{i}";
            var input = new BedrockFunctionRequest { Function = toolName };
            var result = resolver.Resolve(input);
            Assert.Contains($"Response from {toolName}", result.Response.FunctionResponse.ResponseBody.Text.Body);
        }
    }

    [Fact]
    public void TestToolOverrideWithWarning()
    {
        // Arrange
        var resolver = new BedrockAgentFunctionResolver();
        
        // Register a tool
        resolver.Tool("Calculator", () => "Original Calculator");
        
        // Register same tool again with different implementation
        resolver.Tool("Calculator", () => "New Calculator");
        
        // Verify the tool was overridden
        var input = new BedrockFunctionRequest { Function = "Calculator" };
        var result = resolver.Resolve(input);
        
        // The second registration should have overwritten the first
        Assert.Equal("New Calculator", result.Response.FunctionResponse.ResponseBody.Text.Body);
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
