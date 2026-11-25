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
    public void MapBedrockFunction_WithEachArity_RegistersANamedParameterTool()
    {
        var app = CreateApp();
        app.MapBedrockFunction("T1", "", (int a) => $"{a}", "a")
            .MapBedrockFunction("T2", "", (int a, int b) => $"{a}{b}", "a", "b")
            .MapBedrockFunction("T3", "", (int a, int b, int c) => $"{a}{b}{c}", "a", "b", "c")
            .MapBedrockFunction("T4", "", (int a, int b, int c, int d) => $"{a}{b}{c}{d}", "a", "b", "c", "d")
            .MapBedrockFunction("T5", "", (int a, int b, int c, int d, int e) => $"{a}{b}{c}{d}{e}",
                "a", "b", "c", "d", "e")
            .MapBedrockFunction("T6", "", (int a, int b, int c, int d, int e, int f) => $"{a}{b}{c}{d}{e}{f}",
                "a", "b", "c", "d", "e", "f")
            .MapBedrockFunction("T7", "", (int a, int b, int c, int d, int e, int f, int g) =>
                $"{a}{b}{c}{d}{e}{f}{g}", "a", "b", "c", "d", "e", "f", "g")
            .MapBedrockFunction("T8", "", (int a, int b, int c, int d, int e, int f, int g, int h) =>
                $"{a}{b}{c}{d}{e}{f}{g}{h}", "a", "b", "c", "d", "e", "f", "g", "h");
        var parameters = "abcdefgh".Select((name, i) => P(name.ToString(), (i + 1).ToString(), "integer")).ToArray();

        var bodies = Enumerable.Range(1, 8).Select(n => Resolve(app, $"T{n}", parameters));

        Assert.Equal(new[] { "1", "12", "123", "1234", "12345", "123456", "1234567", "12345678" }, bodies);
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
    public void MapBedrockFunction_WithoutAResolverInDependencyInjection_DoesNotThrow()
    {
        var app = WebApplication.CreateSlimBuilder().Build();

        var result = app.MapBedrockFunction("GetCity", "", (string city) => city, "city");

        Assert.Same(app, result);
    }

    [Fact]
    public void AotCompatibleMappings_AreNotAnnotated_AndTheDelegateMappingIs()
    {
        var methods = typeof(BedrockMinimalApiExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);
        var named = methods.Where(m => m.Name == nameof(BedrockMinimalApiExtensions.MapBedrockFunction) &&
                                       m.IsGenericMethodDefinition).ToList();
        var toolType = methods.Single(m => m.Name == nameof(BedrockMinimalApiExtensions.MapBedrockToolType));
        var delegateMapping = methods.Single(m => m.Name == nameof(BedrockMinimalApiExtensions.MapBedrockFunction) &&
                                                  !m.IsGenericMethodDefinition);

        Assert.Equal(8, named.Count);
        Assert.All(named.Append(toolType), m =>
        {
            Assert.Null(m.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
            Assert.Null(m.GetCustomAttribute<RequiresDynamicCodeAttribute>());
        });
        Assert.NotNull(delegateMapping.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
        Assert.NotNull(delegateMapping.GetCustomAttribute<RequiresDynamicCodeAttribute>());
    }

    private static async Task<(int Status, string? ContentType, string Body)> Post(string json,
        IServiceProvider? services = null)
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("GetCity", "", (string city) => $"City: {city}", "city");
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
