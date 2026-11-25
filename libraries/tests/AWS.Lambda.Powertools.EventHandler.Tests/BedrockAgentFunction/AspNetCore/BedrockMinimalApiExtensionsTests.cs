using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.EventHandler.BedrockAgentFunction.AspNetCore;

[BedrockFunctionType]
public class AspNetCoreWeatherTools
{
    [BedrockFunctionTool(Name = "GetWeather", Description = "Gets weather")]
    public static string GetWeather(string city, int days) => $"Weather for {city} for {days} days";

    [BedrockFunctionTool(Name = "GetCity", Description = "Echoes the city")]
    public static string GetCity(string city) => $"City: {city}";
}

public class BedrockMinimalApiExtensionsTests
{
    private static Parameter P(string name, string value, string type = "string") =>
        new() { Name = name, Value = value, Type = type };

    private static string Resolve(WebApplication app, string function, params Parameter[] parameters) =>
        app.Services.GetRequiredService<BedrockAgentFunctionResolver>()
            .Resolve(new BedrockFunctionRequest { Function = function, Parameters = parameters.ToList() })
            .Response.FunctionResponse.ResponseBody.Text.Body;

    // The app is built but never started. Tools are registered on the resolver from dependency injection, so the
    // tests read them back from it instead of going through the endpoint.
    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddBedrockResolver();
        return builder.Build();
    }

    [Fact]
    public void MapBedrockToolType_RegistersTheToolsOfTheType()
    {
        var app = CreateApp();
        app.MapBedrockToolType<AspNetCoreWeatherTools>();

        Assert.Equal("Weather for Lisbon for 3 days",
            Resolve(app, "GetWeather", P("city", "Lisbon"), P("days", "3", "integer")));
    }

    [Fact]
    public void MapBedrockFunction_WithADelegate_RegistersTheTool()
    {
        var app = CreateApp();
        app.MapBedrockFunction("GetCity", (string city) => $"City: {city}", "Echoes the city");

        Assert.Equal("City: Lisbon", Resolve(app, "GetCity", P("city", "Lisbon")));
    }

    [Fact]
    public void MapBedrockToolType_WithoutAResolverInDependencyInjection_DoesNotThrow()
    {
        var app = WebApplication.CreateSlimBuilder().Build();

        var result = app.MapBedrockToolType<AspNetCoreWeatherTools>();

        Assert.Same(app, result);
    }

    [Fact]
    public void MapBedrockToolType_IsNotAnnotated_AndTheDelegateMappingIs()
    {
        var methods = typeof(BedrockMinimalApiExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);
        var toolType = methods.Single(m => m.Name == nameof(BedrockMinimalApiExtensions.MapBedrockToolType));
        var delegateMapping = methods.Single(m => m.Name == nameof(BedrockMinimalApiExtensions.MapBedrockFunction));

        Assert.Null(toolType.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
        Assert.Null(toolType.GetCustomAttribute<RequiresDynamicCodeAttribute>());
        Assert.NotNull(delegateMapping.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
        Assert.NotNull(delegateMapping.GetCustomAttribute<RequiresDynamicCodeAttribute>());
    }

    private static async Task<(int Status, string? ContentType, string Body)> Post(string json,
        IServiceProvider? services = null)
    {
        var resolver = new BedrockAgentFunctionResolver().RegisterTool<AspNetCoreWeatherTools>();
        var context = new DefaultHttpContext
        {
            RequestServices = services ?? new ServiceCollection().BuildServiceProvider(),
            Request = { Body = new MemoryStream(Encoding.UTF8.GetBytes(json)) },
            Response = { Body = new MemoryStream() }
        };

        await BedrockMinimalApiExtensions.HandleBedrockRequestAsync(context, resolver);

        var body = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
        return (context.Response.StatusCode, context.Response.ContentType, body);
    }

    [Fact]
    public async Task Endpoint_ResolvesTheRequestAndWritesTheResponseAsJson()
    {
        var response = await Post(
            "{\"function\":\"GetCity\",\"actionGroup\":\"ag\",\"sessionAttributes\":{\"k\":\"v\"}," +
            "\"parameters\":[{\"name\":\"city\",\"type\":\"string\",\"value\":\"Lisbon\"}]}");

        Assert.Equal(200, response.Status);
        Assert.Equal("application/json; charset=utf-8", response.ContentType);
        Assert.Equal(
            "{\"messageVersion\":\"1.0\",\"response\":{\"actionGroup\":\"ag\",\"function\":\"GetCity\"," +
            "\"functionResponse\":{\"responseBody\":{\"TEXT\":{\"body\":\"City: Lisbon\"}}}}," +
            "\"sessionAttributes\":{\"k\":\"v\"},\"promptSessionAttributes\":{},\"knowledgeBasesConfiguration\":{}}",
            response.Body);
    }

    [Fact]
    public async Task Endpoint_ReturnsBadRequestForANullRequest()
    {
        var response = await Post("null");

        Assert.Equal(400, response.Status);
        Assert.Equal("application/json; charset=utf-8", response.ContentType);
        Assert.Equal("\"Invalid request format\"", response.Body);
    }

    [Fact]
    public async Task Endpoint_ReturnsProblemDetailsForAnInvalidRequest()
    {
        var response = await Post("not json");

        Assert.Equal(500, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        Assert.StartsWith(
            "{\"type\":\"https://tools.ietf.org/html/rfc9110#section-15.6.1\"," +
            "\"title\":\"An error occurred while processing your request.\",\"status\":500," +
            "\"detail\":\"Error processing Bedrock Agent request: ", response.Body);
    }

    [Fact]
    public async Task Endpoint_UsesTheAppsProblemDetailsServiceWhenThereIsOne()
    {
        var services = new ServiceCollection();
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = c => c.ProblemDetails.Extensions["customized"] = true);

        var response = await Post("not json", services.BuildServiceProvider());

        Assert.Equal(500, response.Status);
        Assert.Contains("\"customized\":true", response.Body);
    }
}
