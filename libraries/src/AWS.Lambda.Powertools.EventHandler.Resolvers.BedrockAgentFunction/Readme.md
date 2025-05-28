# AWS Lambda Powertools for .NET - Bedrock Agent Function Resolver

## Overview
The Bedrock Agent Function Resolver is a utility for AWS Lambda that simplifies building serverless applications working with Amazon Bedrock Agents. This library eliminates boilerplate code typically required when implementing Lambda functions that serve as action groups for Bedrock Agents.

Amazon Bedrock Agents can invoke functions to perform tasks based on user input. This library provides an elegant way to register, manage, and execute these functions with minimal code, handling all the parameter extraction and response formatting automatically.

## Features

- **Simple Tool Registration**: Register functions with descriptive names that Bedrock Agents can invoke
- **Automatic Parameter Handling**: Parameters are automatically extracted from Bedrock Agent requests and converted to the appropriate types
- **Type Safety**: Strongly typed parameters and return values
- **Multiple Return Types**: Support for returning strings, primitive types, objects, or custom types
- **Flexible Input Options**: Support for various parameter types including string, int, bool, DateTime, and enums
- **Lambda Context Access**: Easy access to Lambda context for logging and AWS Lambda features
- **Dependency Injection Support**: Seamless integration with .NET's dependency injection system
- **Error Handling**: Automatic error capturing and formatting for responses
- **Async Support**: First-class support for asynchronous function execution

## Installation

Install the package via NuGet:

```bash
dotnet add package AWS.Lambda.Powertools.EventHandler.BedrockAgentFunctionResolver
```

## Basic Usage

Here's a simple example showing how to register and use tool functions:

```csharp
using Amazon.BedrockAgentRuntime.Model;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MyLambdaFunction
{
    public class Function
    {
        private readonly BedrockAgentFunctionResolver _resolver;
        
        public Function()
        {
            _resolver = new BedrockAgentFunctionResolver();
            
            // Register simple tool functions
            _resolver
                .Tool("GetWeather", (string city) => $"The weather in {city} is sunny")
                .Tool("CalculateSum", (int a, int b) => $"The sum of {a} and {b} is {a + b}")
                .Tool("GetCurrentTime", () => $"The current time is {DateTime.Now}");
        }
        
        // Lambda handler function
        public ActionGroupInvocationOutput FunctionHandler(
            ActionGroupInvocationInput input, ILambdaContext context)
        {
            return _resolver.Resolve(input, context);
        }
    }
}
```

When the Bedrock Agent invokes your Lambda function with a request to use the "GetWeather" tool and a parameter for "city", the resolver automatically extracts the parameter, passes it to your function, and formats the response.

## Advanced Usage

### Functions with Descriptions

Add descriptive information to your tool functions:

```csharp
_resolver.Tool(
    "CheckInventory", 
    "Checks if a product is available in inventory",
    (string productId, bool checkWarehouse) => 
    {
        return checkWarehouse 
            ? $"Product {productId} has 15 units in warehouse" 
            : $"Product {productId} has 5 units in store";
    });
```

### Accessing Lambda Context

Access the Lambda context in your functions:

```csharp
_resolver.Tool(
    "LogRequest",
    "Logs request information and returns confirmation",
    (string requestId, ILambdaContext context) => 
    {
        context.Logger.LogLine($"Processing request {requestId}");
        return $"Request {requestId} logged successfully";
    });
```

### Working with Complex Return Types

Return complex objects that will be converted to appropriate responses:

```csharp
public class WeatherReport
{
    public string City { get; set; }
    public string Conditions { get; set; }
    public int Temperature { get; set; }
    
    public override string ToString()
    {
        return $"Weather in {City}: {Conditions}, {Temperature}°F";
    }
}

_resolver.Tool<WeatherReport>(
    "GetDetailedWeather",
    "Returns detailed weather information for a location",
    (string city) => new WeatherReport 
    { 
        City = city, 
        Conditions = "Partly Cloudy", 
        Temperature = 72 
    });
```

### Asynchronous Functions

Register and use asynchronous functions:

```csharp
_resolver.Tool(
    "FetchUserData",
    "Fetches user data from external API", 
    async (string userId, ILambdaContext ctx) => 
    {
        // Log the request
        ctx.Logger.LogLine($"Fetching data for user {userId}");
        
        // Simulate API call
        await Task.Delay(100); 
        
        // Return user information
        return new { Id = userId, Name = "John Doe", Status = "Active" }.ToString();
    });
```

### Direct Access to Request Payload

Access the raw Bedrock Agent request:

```csharp
_resolver.Tool(
    "ProcessRawRequest",
    "Processes the raw Bedrock Agent request", 
    (ActionGroupInvocationInput input) => 
    {
        var functionName = input.Function;
        var parameterCount = input.Parameters.Count;
        return $"Received request for {functionName} with {parameterCount} parameters";
    });
```

## Dependency Injection

The library supports dependency injection for integrating with services:

```csharp
using Microsoft.Extensions.DependencyInjection;

// Set up dependency injection
var services = new ServiceCollection();
services.AddSingleton<IWeatherService, WeatherService>();
services.AddBedrockResolver(); // Extension method to register the resolver

var serviceProvider = services.BuildServiceProvider();
var resolver = serviceProvider.GetRequiredService<BedrockAgentFunctionResolver>();

// Register a tool that uses an injected service
resolver.Tool(
    "GetWeatherForecast",
    "Gets the weather forecast for a location",
    (string city, IWeatherService weatherService, ILambdaContext ctx) => 
    {
        ctx.Logger.LogLine($"Getting weather for {city}");
        return weatherService.GetForecast(city);
    });
```

## How It Works with Amazon Bedrock Agents

1. When a user interacts with a Bedrock Agent, the agent identifies when it needs to call an action to fulfill the user's request.
2. The agent determines which function to call and what parameters are needed.
3. Bedrock sends a request to your Lambda function with the function name and parameters.
4. The BedrockAgentFunctionResolver automatically:
   - Finds the registered handler for the requested function
   - Extracts and converts parameters to the correct types
   - Invokes your handler with the parameters
   - Formats the response in the way Bedrock Agents expect
5. The agent receives the response and uses it to continue the conversation with the user

## Supported Parameter Types

- `string`
- `int`
- `number`
- `bool`
- `enum` types
- `ILambdaContext` (for accessing Lambda context)
- `ActionGroupInvocationInput` (for accessing raw request)
- Any service registered in dependency injection


## Using Attributes to Define Tools

You can define Bedrock Agent functions using attributes instead of explicit registration. This approach provides a clean, declarative way to organize your tools into classes:

### Define Tool Classes with Attributes

```csharp
// Define your tool class with BedrockFunctionType attribute
[BedrockFunctionType]
public class WeatherTools
{
    // Each method marked with BedrockFunctionTool attribute becomes a tool
    [BedrockFunctionTool(Name = "GetWeather", Description = "Gets weather forecast for a location")]
    public static string GetWeather(string city, int days)
    {
        return $"Weather forecast for {city} for the next {days} days: Sunny";
    }
    
    // Supports dependency injection and Lambda context access
    [BedrockFunctionTool(Name = "GetDetailedForecast", Description = "Gets detailed weather forecast")]
    public static string GetDetailedForecast(
        string location, 
        IWeatherService weatherService, 
        ILambdaContext context)
    {
        context.Logger.LogLine($"Getting forecast for {location}");
        return weatherService.GetForecast(location);
    }
}
```

### Register Tool Classes in Your Application

Using the extension method provided in the library, you can easily register all tools from a class:

```csharp

var services = new ServiceCollection();
services.AddSingleton<IWeatherService, WeatherService>();
services.AddBedrockResolver(); // Extension method to register the resolver

var serviceProvider = services.BuildServiceProvider();
var resolver = serviceProvider.GetRequiredService<BedrockAgentFunctionResolver>()
    .RegisterTool<WeatherTools>(); // Register tools from the class during service registration

```

## Complete Example with Dependency Injection

```csharp
using Amazon.BedrockAgentRuntime.Model;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MyBedrockAgent
{
    // Service interfaces and implementations
    public interface IWeatherService
    {
        string GetForecast(string city);
    }

    public class WeatherService : IWeatherService
    {
        public string GetForecast(string city) => $"Weather forecast for {city}: Sunny, 75°F";
    }

    public interface IProductService
    {
        string CheckInventory(string productId);
    }

    public class ProductService : IProductService
    {
        public string CheckInventory(string productId) => $"Product {productId} has 25 units in stock";
    }
    
    // Main Lambda function
    public class Function
    {
        private readonly BedrockAgentFunctionResolver _resolver;

        public Function()
        {
            // Set up dependency injection
            var services = new ServiceCollection();
            services.AddSingleton<IWeatherService, WeatherService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddBedrockResolver(); // Extension method to register the resolver
            
            var serviceProvider = services.BuildServiceProvider();
            _resolver = serviceProvider.GetRequiredService<BedrockAgentFunctionResolver>();

            // Register tool functions that use injected services
            _resolver
                .Tool("GetWeatherForecast", 
                    "Gets weather forecast for a city",
                    (string city, IWeatherService weatherService, ILambdaContext ctx) => 
                    {
                        ctx.Logger.LogLine($"Weather request for {city}");
                        return weatherService.GetForecast(city);
                    })
                .Tool("CheckInventory",
                    "Checks inventory for a product",
                    (string productId, IProductService productService) => 
                        productService.CheckInventory(productId))
                .Tool("GetServerTime",
                    "Returns the current server time",
                    () => DateTime.Now.ToString("F"));
        }

        public ActionGroupInvocationOutput FunctionHandler(
            ActionGroupInvocationInput input, ILambdaContext context)
        {
            return _resolver.Resolve(input, context);
        }
    }
}
```