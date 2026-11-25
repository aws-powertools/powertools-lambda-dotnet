using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore;

// Source generation for JSON serialization, so the endpoint works under Native AOT
[JsonSerializable(typeof(BedrockFunctionRequest))]
[JsonSerializable(typeof(BedrockFunctionResponse))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class BedrockJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Extension methods for registering Bedrock Agent Functions in ASP.NET Core Minimal API.
/// </summary>
public static class BedrockMinimalApiExtensions
{
    internal const string DelegateToolMessage =
        "Tools registered with a Delegate read the delegate's method and parameters with reflection, which isn't " +
        "supported under Native AOT. Define the tool as a static method of a [BedrockFunctionType] class and map " +
        "it with MapBedrockToolType<T>() instead.";

    // Static flag to track if handler is mapped (thread-safe with volatile)
    private static volatile bool _bedrockRequestHandlerMapped;

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
    [RequiresUnreferencedCode(DelegateToolMessage)]
    [RequiresDynamicCode(DelegateToolMessage)]
    public static WebApplication MapBedrockFunction(
        this WebApplication app,
        string functionName,
        Delegate handler,
        string description = "")
    {
        var resolver = GetResolver(app);

        // Register the function with the resolver
        resolver.Tool(functionName, description, handler);

        // Ensure we have a global handler for Bedrock requests
        EnsureBedrockRequestHandler(app, resolver);

        return app;
    }

    private static BedrockAgentFunctionResolver GetResolver(WebApplication app)
    {
        // Get or create the resolver from services
        return app.Services.GetService<BedrockAgentFunctionResolver>() ?? new BedrockAgentFunctionResolver();
    }

    private static void EnsureBedrockRequestHandler(WebApplication app, BedrockAgentFunctionResolver resolver)
    {
        // Check if we've already mapped the handler (we only need to do this once)
        if (_bedrockRequestHandlerMapped)
            return;

        // Map the root endpoint to handle all Bedrock Agent Function requests. A RequestDelegate, rather than a
        // lambda that returns IResult, doesn't need RequestDelegateFactory, which isn't supported under Native AOT.
        app.MapPost("/", context => HandleBedrockRequestAsync(context, resolver));

        // Mark that we've set up the handler
        _bedrockRequestHandlerMapped = true;
    }

    internal static async Task HandleBedrockRequestAsync(HttpContext context, BedrockAgentFunctionResolver resolver)
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
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync("Invalid request format", BedrockJsonContext.Default.String);
                return;
            }

            // Process the request through the resolver
            var result = await resolver.ResolveAsync(bedrockRequest);

            // Use source-generated serialization for the response
            await context.Response.WriteAsJsonAsync(result, BedrockJsonContext.Default.BedrockFunctionResponse);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, $"Error processing Bedrock Agent request: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes a 500 problem details response, like <c>Results.Problem(detail)</c> does, but falls back to
    /// source-generated serialization when the app has no <see cref="IProblemDetailsService"/>, so it also works under
    /// Native AOT.
    /// </summary>
    private static async Task WriteProblemAsync(HttpContext context, string detail)
    {
        var problem = TypedResults.Problem(detail);
        context.Response.StatusCode = problem.StatusCode;

        var problemDetailsService = context.RequestServices?.GetService<IProblemDetailsService>();
        if (problemDetailsService is null || !await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem.ProblemDetails
            }))
        {
            await context.Response.WriteAsJsonAsync(problem.ProblemDetails,
                BedrockJsonContext.Default.ProblemDetails, problem.ContentType);
        }
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
    /// app.MapBedrockToolType&lt;WeatherTools&gt;();
    /// </code>
    /// </example>
    /// <remarks>
    /// Works under Native AOT, see <see cref="BedrockResolverExtensions.RegisterTool{T}"/>.
    /// </remarks>
    public static WebApplication MapBedrockToolType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        this WebApplication app) 
        where T : class
    {
        var resolver = GetResolver(app);

        // Register the tool class
        resolver.RegisterTool<T>();

        // Ensure we have a global handler for Bedrock requests
        EnsureBedrockRequestHandler(app, resolver);

        return app;
    }
}