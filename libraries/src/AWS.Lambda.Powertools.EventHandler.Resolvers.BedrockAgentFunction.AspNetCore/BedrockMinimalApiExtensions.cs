/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore;

// Source generation for JSON serialization
[JsonSerializable(typeof(BedrockFunctionRequest))]
internal partial class BedrockJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Extension methods for registering Bedrock Agent Functions in ASP.NET Core Minimal API.
/// </summary>
public static class BedrockMinimalApiExtensions
{
    // Static flag to track if handler is mapped (thread-safe with volatile)
    private static volatile bool _bedrockRequestHandlerMapped;

    // JSON options with case insensitivity
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Maps an individual Bedrock Agent function that will be called directly from the root endpoint.
    /// The function name is extracted from the incoming request payload.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <param name="functionName">The name of the function to register.</param>
    /// <param name="handler">The delegate handler that implements the function.</param>
    /// <param name="description">Optional description of the function.</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// // Register individual functions
    /// app.MapBedrockFunction("GetWeather", (string city, int month) =>
    ///     $"Weather forecast for {city} in month {month}: Warm and sunny");
    ///
    /// app.MapBedrockFunction("Calculate", (int x, int y) =>
    ///     $"Result: {x + y}");
    /// </code>
    /// </example>
    public static WebApplication MapBedrockFunction(
        this WebApplication app,
        string functionName,
        Delegate handler,
        string description = "")
    {
        // Get or create the resolver from services
        var resolver = app.Services.GetService<BedrockAgentFunctionResolver>()
            ?? new BedrockAgentFunctionResolver();

        // Register the function with the resolver
        resolver.Tool(functionName, description, handler);

        // Ensure we have a global handler for Bedrock requests
        EnsureBedrockRequestHandler(app, resolver);

        return app;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "The handler implementation is controlled and AOT-compatible")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "The handler implementation is controlled and trim-compatible")]
    private static void EnsureBedrockRequestHandler(WebApplication app, BedrockAgentFunctionResolver resolver)
    {
        // Check if we've already mapped the handler (we only need to do this once)
        if (_bedrockRequestHandlerMapped)
            return;

        // Map the root endpoint to handle all Bedrock Agent Function requests
        app.MapPost("/", [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Handler is AOT-friendly")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Handler is trim-friendly")]
        async (HttpContext context) =>
        {
            try
            {
                // Read the request body
                string requestBody;
                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                // Use source-generated serialization for the request
                var bedrockRequest = JsonSerializer.Deserialize(requestBody,
                    BedrockJsonContext.Default.BedrockFunctionRequest);

                if (bedrockRequest == null)
                    return Results.BadRequest("Invalid request format");

                // Process the request through the resolver
                var result = await resolver.ResolveAsync(bedrockRequest);

                // For the response, use the standard serializer with suppressed warnings
                // This is more compatible with different response types
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(result, JsonOptions);
                return Results.Empty;
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error processing Bedrock Agent request: {ex.Message}");
            }
        });

        // Mark that we've set up the handler
        _bedrockRequestHandlerMapped = true;
    }
    
    /// <summary>
    /// Registers all methods from a class marked with BedrockFunctionTypeAttribute.
    /// </summary>
    /// <typeparam name="T">The type containing tool methods marked with BedrockFunctionToolAttribute</typeparam>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// // Define your tool class
    /// [BedrockFunctionType]
    /// public class WeatherTools
    /// {
    ///     [BedrockFunctionTool(Name = "GetWeather", Description = "Gets weather forecast")]
    ///     public static string GetWeather(string location, int days)
    ///     {
    ///         return $"Weather forecast for {location} for the next {days} days";
    ///     }
    /// }
    /// 
    /// // Register all tools from the class
    /// app.MapBedrockToolClass&lt;WeatherTools&gt;();
    /// </code>
    /// </example>
    public static WebApplication MapBedrockToolType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        this WebApplication app) 
        where T : class
    {
        // Get or create the resolver from services
        var resolver = app.Services.GetService<BedrockAgentFunctionResolver>()
                       ?? new BedrockAgentFunctionResolver();

        // Register the tool class
        resolver.RegisterTool<T>();

        // Ensure we have a global handler for Bedrock requests
        EnsureBedrockRequestHandler(app, resolver);

        return app;
    }
}