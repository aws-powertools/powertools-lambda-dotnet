// This file is referenced by docs/core/event_handler/bedrock_agent_function.md
// via pymdownx.snippets (mkdocs).

using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using Microsoft.AspNetCore.Builder;

namespace Docs.BedrockAgentFunctionSnippets.NativeAot
{
public enum TemperatureUnit
{
    Celsius,
    Fahrenheit
}

internal static class Example { public static async Task Run() { // --8<-- [start:hidden]
// --8<-- [start:named_parameter_tools]
var resolver = new BedrockAgentFunctionResolver();

resolver
    // The names after the handler are the Bedrock parameters bound to each handler parameter, in order
    .Tool("GetWeather", "Gets the weather forecast for a city",
        (string city, int days, TemperatureUnit unit) => $"{city}: sunny for {days} days, 25 degrees {unit}",
        "city", "days", "unit")
    // ILambdaContext, BedrockFunctionRequest and services from dependency injection are bound by type
    .Tool("GetTime", "Gets the time in a city",
        async (string city, ILambdaContext context) =>
        {
            context.Logger.LogInformation($"Getting the time in {city}");
            await Task.Delay(10);
            return $"It's {DateTime.UtcNow:t} in {city}";
        },
        "city", "context")
    // Tool classes work too
    .RegisterTool<AirportTools>();

await LambdaBootstrapBuilder.Create(
        (BedrockFunctionRequest input, ILambdaContext context) => resolver.ResolveAsync(input, context),
        new SourceGeneratorLambdaJsonSerializer<BedrockFunctionJsonContext>())
    .Build()
    .RunAsync();
// --8<-- [end:named_parameter_tools]
// --8<-- [start:hidden]
} }

// --8<-- [start:native_aot_tool_class]
[BedrockFunctionType]
public class AirportTools
{
    [BedrockFunctionTool(Name = "GetAirportCode", Description = "Gets the airport code for a city")]
    public static string GetAirportCode(string city, bool international) =>
        international ? $"{city.ToUpperInvariant()[..3]}-INTL" : city.ToUpperInvariant()[..3];
}

[JsonSerializable(typeof(BedrockFunctionRequest))]
[JsonSerializable(typeof(BedrockFunctionResponse))]
public partial class BedrockFunctionJsonContext : JsonSerializerContext
{
}
// --8<-- [end:native_aot_tool_class]

internal static class AspNetCoreExample { public static void Run(WebApplication app) { // --8<-- [start:hidden]
// --8<-- [start:native_aot_aspnetcore]
app.MapBedrockFunction("GetWeather", "Gets the weather forecast for a city",
    (string city, int days) => $"{city}: sunny for {days} days",
    "city", "days");

app.MapBedrockToolType<AirportTools>();
// --8<-- [end:native_aot_aspnetcore]
// --8<-- [start:hidden]
} }
}
