using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;

namespace AWS.Lambda.Powertools.EventHandler.BedrockAgentFunction;

public enum ForecastUnit
{
    Celsius,
    Fahrenheit
}

[BedrockFunctionType]
public class ForecastTools
{
    [BedrockFunctionTool(Name = "Plan", Description = "Multi-parameter tool")]
    public static string Plan(string city, int days, bool metric, ForecastUnit unit, int? limit) =>
        $"{city} {days} {metric} {unit} limit={limit?.ToString() ?? "null"}";

    [BedrockFunctionTool(Description = "Tool named after the method")]
    public static long Double(long n) => n * 2;

    [BedrockFunctionTool(Name = "PlanAsync", Description = "Async tool")]
    public static async Task<string> PlanAsync(string city, ILambdaContext context)
    {
        await Task.Yield();
        return $"{city} from {context.FunctionName}";
    }

    [BedrockFunctionTool(Name = "Throws", Description = "Throws")]
    public static string Throws(string city) =>
        throw new InvalidOperationException($"No data for {city}", new Exception("inner"));

    // Not a tool: no BedrockFunctionTool attribute
    public static string NotATool() => "not a tool";
}

public class NotMarkedTools
{
    [BedrockFunctionTool(Name = "Hidden")]
    public static string Hidden() => "hidden";
}

public class RegisterToolTests
{
    private static Parameter P(string name, string value, string type = "string") =>
        new() { Name = name, Value = value, Type = type };

    private static string Resolve(BedrockAgentFunctionResolver resolver, string function,
        params Parameter[] parameters) =>
        resolver.Resolve(new BedrockFunctionRequest { Function = function, Parameters = parameters.ToList() },
            new TestLambdaContext { FunctionName = "MyLambda" }).Response.FunctionResponse.ResponseBody.Text.Body;

    [Fact]
    public void RegisterTool_BindsMultipleParametersByName()
    {
        var resolver = new BedrockAgentFunctionResolver().RegisterTool<ForecastTools>();

        var body = Resolve(resolver, "Plan", P("unit", "Fahrenheit"), P("days", "3", "integer"),
            P("metric", "true", "boolean"), P("city", "Lisbon"), P("limit", "7", "integer"));

        // Nullable parameters aren't Bedrock parameter types, so they aren't bound (same as Delegate tools).
        Assert.Equal("Lisbon 3 True Fahrenheit limit=null", body);
    }

    [Fact]
    public void RegisterTool_UsesDefaultValuesForMissingParameters()
    {
        var resolver = new BedrockAgentFunctionResolver().RegisterTool<ForecastTools>();

        Assert.Equal("Porto 0 False Celsius limit=null", Resolve(resolver, "Plan", P("city", "Porto")));
    }

    [Fact]
    public void RegisterTool_UsesTheMethodNameWhenTheAttributeHasNoName()
    {
        var resolver = new BedrockAgentFunctionResolver().RegisterTool<ForecastTools>();

        Assert.Equal("10", Resolve(resolver, "Double", P("n", "5", "integer")));
        Assert.Equal("Error: Tool NotATool has not been registered in handler", Resolve(resolver, "NotATool"));
    }

    [Fact]
    public void RegisterTool_AwaitsAsyncToolsAndBindsTheLambdaContext()
    {
        var resolver = new BedrockAgentFunctionResolver().RegisterTool<ForecastTools>();

        Assert.Equal("Lisbon from MyLambda", Resolve(resolver, "PlanAsync", P("city", "Lisbon")));
    }

    [Fact]
    public void RegisterTool_ReturnsErrorResponseWithTheToolsExceptionMessage()
    {
        var resolver = new BedrockAgentFunctionResolver().RegisterTool<ForecastTools>();

        Assert.Equal("Error when invoking tool: No data for Lisbon", Resolve(resolver, "Throws", P("city", "Lisbon")));
    }

    [Fact]
    public void RegisterTool_IgnoresTypesWithoutTheBedrockFunctionTypeAttribute()
    {
        var resolver = new BedrockAgentFunctionResolver().RegisterTool<NotMarkedTools>();

        Assert.Equal("Error: Tool Hidden has not been registered in handler", Resolve(resolver, "Hidden"));
    }
}
