# Experimental work in progress, not yet released

# AWS Lambda Powertools for .NET - Bedrock Agent Function Resolver for ASP.NET Core

## Overview
This library provides ASP.NET Core integration for the AWS Lambda Powertools Bedrock Agent Function Resolver. It enables you to easily expose Bedrock Agent functions as endpoints in your ASP.NET Core applications using a simple, fluent API.

## Features

- **Minimal API Integration**: Register Bedrock Agent functions using familiar ASP.NET Core Minimal API patterns
- **AOT Compatibility**: Full support for .NET 8 AOT compilation through source generation
- **Simple Function Registration**: Register functions with a fluent API
- **Automatic Request Processing**: Automatic parsing of Bedrock Agent requests and formatting of responses
- **Error Handling**: Built-in error handling for Bedrock Agent function requests

## Installation

Install the package via NuGet:

```bash
dotnet add package AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore
```

## Basic Usage

Here's how to register Bedrock Agent functions in your ASP.NET Core application:

```csharp
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Register individual functions
app.MapBedrockFunction("GetWeather", (string city, int month) =>
    $"Weather forecast for {city} in month {month}: Warm and sunny");

app.MapBedrockFunction("Calculate", (int x, int y) =>
    $"Result: {x + y}");

app.Run();
```

When Amazon Bedrock Agent sends a request to your application, the appropriate function will be invoked with the extracted parameters, and the response will be formatted correctly for the agent.

## Using with Dependency Injection

Register the Bedrock resolver with dependency injection for more advanced scenarios:

```csharp
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register the resolver and any other services
builder.Services.AddBedrockResolver();
builder.Services.AddSingleton<IWeatherService, WeatherService>();

var app = builder.Build();

// Register functions that use injected services
app.MapBedrockFunction("GetWeatherForecast", 
    (string city, IWeatherService weatherService) =>
        weatherService.GetForecast(city),
    "Gets weather forecast for a city");

app.Run();
```

## Advanced Usage

### Function Documentation

Add descriptions to your functions for better documentation:

```csharp
app.MapBedrockFunction("GetWeather", 
    (string city, int month) => $"Weather forecast for {city} in month {month}: Warm and sunny",
    "Gets weather forecast for a specific city and month");
```

### Working with Tool Classes

Use the `MapBedrockToolClass<T>()` method to register all functions from a class directly:

```csharp
[BedrockFunctionType]
public class WeatherTools
{
    [BedrockFunctionTool(Name = "GetWeather", Description = "Gets weather forecast")]
    public static string GetWeather(string location, int days)
    {
        return $"Weather forecast for {location} for the next {days} days";
    }
}

// In Program.cs - directly register the tool class
app.MapBedrockToolClass<WeatherTools>();
```

## How It Works

1. When you call `MapBedrockFunction`, the function is registered with the resolver
2. An HTTP endpoint is set up at the root path (/) to handle incoming Bedrock Agent requests
3. When a request arrives, the library:
    - Deserializes the JSON payload
    - Extracts the function name and parameters
    - Invokes the matching function with the appropriate parameters
    - Serializes the result and returns it as a response

## Requirements

- .NET 8.0 or later
- ASP.NET Core 8.0 or later