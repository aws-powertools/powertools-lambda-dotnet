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
// --8<-- [start:native_aot_tool_class]
public enum TemperatureUnit
{
    Celsius,
    Fahrenheit
}

[BedrockFunctionType]
public class WeatherTools
{
    // Parameters are bound by name from the Bedrock request
    [BedrockFunctionTool(Name = "GetWeather", Description = "Gets the weather forecast for a city")]
    public static string GetWeather(string city, int days, TemperatureUnit unit) =>
        $"{city}: sunny for {days} days, 25 degrees {unit}";

    // ILambdaContext, BedrockFunctionRequest and services from dependency injection are bound by type
    [BedrockFunctionTool(Name = "GetTime", Description = "Gets the time in a city")]
    public static async Task<string> GetTime(string city, ILambdaContext context)
    {
        context.Logger.LogInformation($"Getting the time in {city}");
        await Task.Delay(10);
        return $"It's {DateTime.UtcNow:t} in {city}";
    }
}
// --8<-- [end:native_aot_tool_class]

// --8<-- [start:native_aot_serialization_context]
[JsonSerializable(typeof(BedrockFunctionRequest))]
[JsonSerializable(typeof(BedrockFunctionResponse))]
public partial class BedrockFunctionJsonContext : JsonSerializerContext
{
}
// --8<-- [end:native_aot_serialization_context]

internal static class Example { public static async Task Run() { // --8<-- [start:hidden]
// --8<-- [start:native_aot_register]
var resolver = new BedrockAgentFunctionResolver()
    .RegisterTool<WeatherTools>()
    // Typed handlers that read the request themselves work too
    .Tool("GetParameterCount", (BedrockFunctionRequest request) =>
        BedrockFunctionResponse.WithText($"{request.Parameters.Count} parameters"));

await LambdaBootstrapBuilder.Create(
        (BedrockFunctionRequest input, ILambdaContext context) => resolver.ResolveAsync(input, context),
        new SourceGeneratorLambdaJsonSerializer<BedrockFunctionJsonContext>())
    .Build()
    .RunAsync();
// --8<-- [end:native_aot_register]
// --8<-- [start:hidden]
} }

internal static class AspNetCoreExample { public static void Run(WebApplication app) { // --8<-- [start:hidden]
// --8<-- [start:native_aot_aspnetcore]
app.MapBedrockToolType<WeatherTools>();
// --8<-- [end:native_aot_aspnetcore]
// --8<-- [start:hidden]
} }
}
