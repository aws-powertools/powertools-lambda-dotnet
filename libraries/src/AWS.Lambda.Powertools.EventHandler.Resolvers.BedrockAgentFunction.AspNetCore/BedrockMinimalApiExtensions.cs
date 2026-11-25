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
        "supported under Native AOT. Use the MapBedrockFunction overload that takes a typed Func and the names of " +
        "its parameters instead, for example MapBedrockFunction(name, description, (string city, int days) => ..., " +
        "\"city\", \"days\"), or map a [BedrockFunctionType] class with MapBedrockToolType<T>().";

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
    /// Maps a Bedrock Agent function whose handler takes 1 parameter, bound by name from the Bedrock request.
    /// This overload doesn't use reflection, so it works under Native AOT.
    /// </summary>
    /// <remarks>
    /// See <see cref="BedrockAgentFunctionResolver.Tool{T1, TResult}(string, string, Func{T1, TResult}, string)"/>
    /// for how parameters are bound and converted.
    /// </remarks>
    /// <typeparam name="T1">The type of the first handler parameter</typeparam>
    /// <typeparam name="TResult">The return type of the handler</typeparam>
    /// <param name="app">The web application to configure.</param>
    /// <param name="functionName">The name of the function to register.</param>
    /// <param name="description">Description of the function.</param>
    /// <param name="handler">The handler that implements the function.</param>
    /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", "Gets the weather forecast",
    ///     (string city, int days) => $"Weather for {city} for {days} days",
    ///     "city", "days");
    /// </code>
    /// </example>
    public static WebApplication MapBedrockFunction<T1, TResult>(
        this WebApplication app,
        string functionName,
        string description,
        Func<T1, TResult> handler,
        string parameterName1)
    {
        var resolver = GetResolver(app);
        resolver.Tool(functionName, description, handler, parameterName1);
        EnsureBedrockRequestHandler(app, resolver);
        return app;
    }

    /// <summary>
    /// Maps a Bedrock Agent function whose handler takes 2 parameters, bound by name from the Bedrock request.
    /// This overload doesn't use reflection, so it works under Native AOT.
    /// </summary>
    /// <remarks>
    /// See <see cref="BedrockAgentFunctionResolver.Tool{T1, TResult}(string, string, Func{T1, TResult}, string)"/>
    /// for how parameters are bound and converted.
    /// </remarks>
    /// <typeparam name="T1">The type of the first handler parameter</typeparam>
    /// <typeparam name="T2">The type of the second handler parameter</typeparam>
    /// <typeparam name="TResult">The return type of the handler</typeparam>
    /// <param name="app">The web application to configure.</param>
    /// <param name="functionName">The name of the function to register.</param>
    /// <param name="description">Description of the function.</param>
    /// <param name="handler">The handler that implements the function.</param>
    /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
    /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", "Gets the weather forecast",
    ///     (string city, int days) => $"Weather for {city} for {days} days",
    ///     "city", "days");
    /// </code>
    /// </example>
    public static WebApplication MapBedrockFunction<T1, T2, TResult>(
        this WebApplication app,
        string functionName,
        string description,
        Func<T1, T2, TResult> handler,
        string parameterName1,
        string parameterName2)
    {
        var resolver = GetResolver(app);
        resolver.Tool(functionName, description, handler, parameterName1, parameterName2);
        EnsureBedrockRequestHandler(app, resolver);
        return app;
    }

    /// <summary>
    /// Maps a Bedrock Agent function whose handler takes 3 parameters, bound by name from the Bedrock request.
    /// This overload doesn't use reflection, so it works under Native AOT.
    /// </summary>
    /// <remarks>
    /// See <see cref="BedrockAgentFunctionResolver.Tool{T1, TResult}(string, string, Func{T1, TResult}, string)"/>
    /// for how parameters are bound and converted.
    /// </remarks>
    /// <typeparam name="T1">The type of the first handler parameter</typeparam>
    /// <typeparam name="T2">The type of the second handler parameter</typeparam>
    /// <typeparam name="T3">The type of the third handler parameter</typeparam>
    /// <typeparam name="TResult">The return type of the handler</typeparam>
    /// <param name="app">The web application to configure.</param>
    /// <param name="functionName">The name of the function to register.</param>
    /// <param name="description">Description of the function.</param>
    /// <param name="handler">The handler that implements the function.</param>
    /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
    /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
    /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", "Gets the weather forecast",
    ///     (string city, int days) => $"Weather for {city} for {days} days",
    ///     "city", "days");
    /// </code>
    /// </example>
    public static WebApplication MapBedrockFunction<T1, T2, T3, TResult>(
        this WebApplication app,
        string functionName,
        string description,
        Func<T1, T2, T3, TResult> handler,
        string parameterName1,
        string parameterName2,
        string parameterName3)
    {
        var resolver = GetResolver(app);
        resolver.Tool(functionName, description, handler, parameterName1, parameterName2, parameterName3);
        EnsureBedrockRequestHandler(app, resolver);
        return app;
    }

    /// <summary>
    /// Maps a Bedrock Agent function whose handler takes 4 parameters, bound by name from the Bedrock request.
    /// This overload doesn't use reflection, so it works under Native AOT.
    /// </summary>
    /// <remarks>
    /// See <see cref="BedrockAgentFunctionResolver.Tool{T1, TResult}(string, string, Func{T1, TResult}, string)"/>
    /// for how parameters are bound and converted.
    /// </remarks>
    /// <typeparam name="T1">The type of the first handler parameter</typeparam>
    /// <typeparam name="T2">The type of the second handler parameter</typeparam>
    /// <typeparam name="T3">The type of the third handler parameter</typeparam>
    /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
    /// <typeparam name="TResult">The return type of the handler</typeparam>
    /// <param name="app">The web application to configure.</param>
    /// <param name="functionName">The name of the function to register.</param>
    /// <param name="description">Description of the function.</param>
    /// <param name="handler">The handler that implements the function.</param>
    /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
    /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
    /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
    /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", "Gets the weather forecast",
    ///     (string city, int days) => $"Weather for {city} for {days} days",
    ///     "city", "days");
    /// </code>
    /// </example>
    public static WebApplication MapBedrockFunction<T1, T2, T3, T4, TResult>(
        this WebApplication app,
        string functionName,
        string description,
        Func<T1, T2, T3, T4, TResult> handler,
        string parameterName1,
        string parameterName2,
        string parameterName3,
        string parameterName4)
    {
        var resolver = GetResolver(app);
        resolver.Tool(functionName, description, handler, parameterName1, parameterName2, parameterName3, parameterName4);
        EnsureBedrockRequestHandler(app, resolver);
        return app;
    }

    /// <summary>
    /// Maps a Bedrock Agent function whose handler takes 5 parameters, bound by name from the Bedrock request.
    /// This overload doesn't use reflection, so it works under Native AOT.
    /// </summary>
    /// <remarks>
    /// See <see cref="BedrockAgentFunctionResolver.Tool{T1, TResult}(string, string, Func{T1, TResult}, string)"/>
    /// for how parameters are bound and converted.
    /// </remarks>
    /// <typeparam name="T1">The type of the first handler parameter</typeparam>
    /// <typeparam name="T2">The type of the second handler parameter</typeparam>
    /// <typeparam name="T3">The type of the third handler parameter</typeparam>
    /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
    /// <typeparam name="T5">The type of the fifth handler parameter</typeparam>
    /// <typeparam name="TResult">The return type of the handler</typeparam>
    /// <param name="app">The web application to configure.</param>
    /// <param name="functionName">The name of the function to register.</param>
    /// <param name="description">Description of the function.</param>
    /// <param name="handler">The handler that implements the function.</param>
    /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
    /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
    /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
    /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
    /// <param name="parameterName5">The name of the Bedrock request parameter bound to the fifth handler parameter</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", "Gets the weather forecast",
    ///     (string city, int days) => $"Weather for {city} for {days} days",
    ///     "city", "days");
    /// </code>
    /// </example>
    public static WebApplication MapBedrockFunction<T1, T2, T3, T4, T5, TResult>(
        this WebApplication app,
        string functionName,
        string description,
        Func<T1, T2, T3, T4, T5, TResult> handler,
        string parameterName1,
        string parameterName2,
        string parameterName3,
        string parameterName4,
        string parameterName5)
    {
        var resolver = GetResolver(app);
        resolver.Tool(functionName, description, handler, parameterName1, parameterName2, parameterName3, parameterName4, parameterName5);
        EnsureBedrockRequestHandler(app, resolver);
        return app;
    }

    /// <summary>
    /// Maps a Bedrock Agent function whose handler takes 6 parameters, bound by name from the Bedrock request.
    /// This overload doesn't use reflection, so it works under Native AOT.
    /// </summary>
    /// <remarks>
    /// See <see cref="BedrockAgentFunctionResolver.Tool{T1, TResult}(string, string, Func{T1, TResult}, string)"/>
    /// for how parameters are bound and converted.
    /// </remarks>
    /// <typeparam name="T1">The type of the first handler parameter</typeparam>
    /// <typeparam name="T2">The type of the second handler parameter</typeparam>
    /// <typeparam name="T3">The type of the third handler parameter</typeparam>
    /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
    /// <typeparam name="T5">The type of the fifth handler parameter</typeparam>
    /// <typeparam name="T6">The type of the sixth handler parameter</typeparam>
    /// <typeparam name="TResult">The return type of the handler</typeparam>
    /// <param name="app">The web application to configure.</param>
    /// <param name="functionName">The name of the function to register.</param>
    /// <param name="description">Description of the function.</param>
    /// <param name="handler">The handler that implements the function.</param>
    /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
    /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
    /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
    /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
    /// <param name="parameterName5">The name of the Bedrock request parameter bound to the fifth handler parameter</param>
    /// <param name="parameterName6">The name of the Bedrock request parameter bound to the sixth handler parameter</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", "Gets the weather forecast",
    ///     (string city, int days) => $"Weather for {city} for {days} days",
    ///     "city", "days");
    /// </code>
    /// </example>
    public static WebApplication MapBedrockFunction<T1, T2, T3, T4, T5, T6, TResult>(
        this WebApplication app,
        string functionName,
        string description,
        Func<T1, T2, T3, T4, T5, T6, TResult> handler,
        string parameterName1,
        string parameterName2,
        string parameterName3,
        string parameterName4,
        string parameterName5,
        string parameterName6)
    {
        var resolver = GetResolver(app);
        resolver.Tool(functionName, description, handler, parameterName1, parameterName2, parameterName3, parameterName4, parameterName5, parameterName6);
        EnsureBedrockRequestHandler(app, resolver);
        return app;
    }

    /// <summary>
    /// Maps a Bedrock Agent function whose handler takes 7 parameters, bound by name from the Bedrock request.
    /// This overload doesn't use reflection, so it works under Native AOT.
    /// </summary>
    /// <remarks>
    /// See <see cref="BedrockAgentFunctionResolver.Tool{T1, TResult}(string, string, Func{T1, TResult}, string)"/>
    /// for how parameters are bound and converted.
    /// </remarks>
    /// <typeparam name="T1">The type of the first handler parameter</typeparam>
    /// <typeparam name="T2">The type of the second handler parameter</typeparam>
    /// <typeparam name="T3">The type of the third handler parameter</typeparam>
    /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
    /// <typeparam name="T5">The type of the fifth handler parameter</typeparam>
    /// <typeparam name="T6">The type of the sixth handler parameter</typeparam>
    /// <typeparam name="T7">The type of the seventh handler parameter</typeparam>
    /// <typeparam name="TResult">The return type of the handler</typeparam>
    /// <param name="app">The web application to configure.</param>
    /// <param name="functionName">The name of the function to register.</param>
    /// <param name="description">Description of the function.</param>
    /// <param name="handler">The handler that implements the function.</param>
    /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
    /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
    /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
    /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
    /// <param name="parameterName5">The name of the Bedrock request parameter bound to the fifth handler parameter</param>
    /// <param name="parameterName6">The name of the Bedrock request parameter bound to the sixth handler parameter</param>
    /// <param name="parameterName7">The name of the Bedrock request parameter bound to the seventh handler parameter</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", "Gets the weather forecast",
    ///     (string city, int days) => $"Weather for {city} for {days} days",
    ///     "city", "days");
    /// </code>
    /// </example>
    public static WebApplication MapBedrockFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(
        this WebApplication app,
        string functionName,
        string description,
        Func<T1, T2, T3, T4, T5, T6, T7, TResult> handler,
        string parameterName1,
        string parameterName2,
        string parameterName3,
        string parameterName4,
        string parameterName5,
        string parameterName6,
        string parameterName7)
    {
        var resolver = GetResolver(app);
        resolver.Tool(functionName, description, handler, parameterName1, parameterName2, parameterName3, parameterName4, parameterName5, parameterName6, parameterName7);
        EnsureBedrockRequestHandler(app, resolver);
        return app;
    }

    /// <summary>
    /// Maps a Bedrock Agent function whose handler takes 8 parameters, bound by name from the Bedrock request.
    /// This overload doesn't use reflection, so it works under Native AOT.
    /// </summary>
    /// <remarks>
    /// See <see cref="BedrockAgentFunctionResolver.Tool{T1, TResult}(string, string, Func{T1, TResult}, string)"/>
    /// for how parameters are bound and converted.
    /// </remarks>
    /// <typeparam name="T1">The type of the first handler parameter</typeparam>
    /// <typeparam name="T2">The type of the second handler parameter</typeparam>
    /// <typeparam name="T3">The type of the third handler parameter</typeparam>
    /// <typeparam name="T4">The type of the fourth handler parameter</typeparam>
    /// <typeparam name="T5">The type of the fifth handler parameter</typeparam>
    /// <typeparam name="T6">The type of the sixth handler parameter</typeparam>
    /// <typeparam name="T7">The type of the seventh handler parameter</typeparam>
    /// <typeparam name="T8">The type of the eighth handler parameter</typeparam>
    /// <typeparam name="TResult">The return type of the handler</typeparam>
    /// <param name="app">The web application to configure.</param>
    /// <param name="functionName">The name of the function to register.</param>
    /// <param name="description">Description of the function.</param>
    /// <param name="handler">The handler that implements the function.</param>
    /// <param name="parameterName1">The name of the Bedrock request parameter bound to the first handler parameter</param>
    /// <param name="parameterName2">The name of the Bedrock request parameter bound to the second handler parameter</param>
    /// <param name="parameterName3">The name of the Bedrock request parameter bound to the third handler parameter</param>
    /// <param name="parameterName4">The name of the Bedrock request parameter bound to the fourth handler parameter</param>
    /// <param name="parameterName5">The name of the Bedrock request parameter bound to the fifth handler parameter</param>
    /// <param name="parameterName6">The name of the Bedrock request parameter bound to the sixth handler parameter</param>
    /// <param name="parameterName7">The name of the Bedrock request parameter bound to the seventh handler parameter</param>
    /// <param name="parameterName8">The name of the Bedrock request parameter bound to the eighth handler parameter</param>
    /// <returns>The web application instance.</returns>
    /// <example>
    /// <code>
    /// app.MapBedrockFunction("GetWeather", "Gets the weather forecast",
    ///     (string city, int days) => $"Weather for {city} for {days} days",
    ///     "city", "days");
    /// </code>
    /// </example>
    public static WebApplication MapBedrockFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
        this WebApplication app,
        string functionName,
        string description,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> handler,
        string parameterName1,
        string parameterName2,
        string parameterName3,
        string parameterName4,
        string parameterName5,
        string parameterName6,
        string parameterName7,
        string parameterName8)
    {
        var resolver = GetResolver(app);
        resolver.Tool(functionName, description, handler, parameterName1, parameterName2, parameterName3, parameterName4, parameterName5, parameterName6, parameterName7, parameterName8);
        EnsureBedrockRequestHandler(app, resolver);
        return app;
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