// This file is referenced by docs/core/event_handler/bedrock_agent_function.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.BedrockAgentFunction;

// --8<-- [start:dependency_injection]
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
// --8<-- [end:dependency_injection]

// --8<-- [start:attribute_tool_classes]
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
// --8<-- [end:attribute_tool_classes]

// --8<-- [start:register_tool_classes]

var services = new ServiceCollection();
services.AddSingleton<IWeatherService, WeatherService>();
services.AddBedrockResolver(); // Extension method to register the resolver

var serviceProvider = services.BuildServiceProvider();
var resolver = serviceProvider.GetRequiredService<BedrockAgentFunctionResolver>()
    .RegisterTool<WeatherTools>(); // Register tools from the class during service registration

// --8<-- [end:register_tool_classes]
