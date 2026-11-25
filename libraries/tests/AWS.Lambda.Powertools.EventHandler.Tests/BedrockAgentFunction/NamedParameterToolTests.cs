using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.EventHandler.BedrockAgentFunction;

/// <summary>
/// Tools registered with the Tool overloads that take a typed Func and the names of its parameters.
/// These don't read the delegate with reflection, so they also work under Native AOT.
/// </summary>
public class NamedParameterToolTests
{
    private static Parameter P(string name, string value, string type = "string") =>
        new() { Name = name, Value = value, Type = type };

    private static BedrockFunctionRequest Request(string function, params Parameter[] parameters) =>
        new() { Function = function, ActionGroup = "TestGroup", Parameters = parameters.ToList() };

    private static string Body(BedrockFunctionResponse response) =>
        response.Response.FunctionResponse.ResponseBody.Text.Body;

    [Fact]
    public void Tool_WithOneNamedParameter_BindsTheRequestParameterByName()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("GetCity", "Echoes the city", (string city) => $"City: {city}", "city");

        var result = resolver.Resolve(Request("GetCity", P("other", "x"), P("city", "Lisbon")));

        Assert.Equal("City: Lisbon", Body(result));
        Assert.Equal("TestGroup", result.Response.ActionGroup);
        Assert.Equal("GetCity", result.Response.Function);
    }

    public enum TempUnit
    {
        Celsius,
        Fahrenheit
    }

    private static readonly Parameter[] ForecastParameters =
    {
        P("city", "Lisbon"), P("days", "3", "integer"), P("metric", "true", "boolean"), P("unit", "Fahrenheit"),
        P("limit", "7", "integer")
    };

    private static string Forecast(string city, int days, bool metric, TempUnit unit, int? limit) =>
        $"{city} {days} {metric} {unit} limit={limit?.ToString() ?? "null"}";

    [Fact]
    public void Tool_WithNamedParameters_ConvertsValuesLikeDelegateTools()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Forecast", "Forecast", (string city, int days, bool metric, TempUnit unit, int? limit) =>
            Forecast(city, days, metric, unit, limit), "city", "days", "metric", "unit", "limit");
        // Reference behavior: the reflection-based Delegate overload.
        resolver.Tool("ForecastDelegate", "Forecast", (string city, int days, bool metric, TempUnit unit, int? limit) =>
            Forecast(city, days, metric, unit, limit));

        var named = Body(resolver.Resolve(Request("Forecast", ForecastParameters)));
        var reference = Body(resolver.Resolve(Request("ForecastDelegate", ForecastParameters)));

        // Nullable parameters aren't Bedrock parameter types, so neither path binds them.
        Assert.Equal("Lisbon 3 True Fahrenheit limit=null", named);
        Assert.Equal(reference, named);
    }

    [Fact]
    public void Tool_WithNamedParameters_UsesDefaultValuesForMissingParameters()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Forecast", "Forecast", (string city, int days, bool metric, TempUnit unit, int? limit) =>
            Forecast(city, days, metric, unit, limit), "city", "days", "metric", "unit", "limit");

        var result = resolver.Resolve(Request("Forecast", P("city", "Porto")));

        Assert.Equal("Porto 0 False Celsius limit=null", Body(result));
    }

    [Fact]
    public void Tool_WithNamedParameters_MatchesNamesCaseInsensitively()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Add", "Adds", (long a, double b, decimal c) => (a + b + (double)c).ToString(
            System.Globalization.CultureInfo.InvariantCulture), "A", "B", "C");

        var result = resolver.Resolve(Request("Add", P("a", "1", "integer"), P("b", "1.5", "number"),
            P("c", "2.25", "number")));

        Assert.Equal("4.75", Body(result));
    }

    [Fact]
    public void Tool_WithNamedParameters_AwaitsAsyncHandlers()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Async", "Async", async (string city, int days) =>
        {
            await Task.Yield();
            return $"{city} in {days} days";
        }, "city", "days");

        var result = resolver.Resolve(Request("Async", ForecastParameters));

        Assert.Equal("Lisbon in 3 days", Body(result));
    }

    [Fact]
    public void Tool_WithNamedParameters_ReturnsTheHandlersResponse()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Fail", "Fails", (string city) =>
        {
            var response = BedrockFunctionResponse.WithText($"No forecast for {city}");
            response.Response.FunctionResponse.ResponseState = ResponseState.FAILURE;
            return response;
        }, "city");

        var result = resolver.Resolve(Request("Fail", P("city", "Lisbon")));

        Assert.Equal("No forecast for Lisbon", Body(result));
        Assert.Equal(ResponseState.FAILURE, result.Response.FunctionResponse.ResponseState);
        Assert.Equal("TestGroup", result.Response.ActionGroup);
        Assert.Equal("Fail", result.Response.Function);
    }

    [Fact]
    public void Tool_WithNamedParameters_ReturnsErrorResponseWithTheToolsExceptionMessage()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Throws", "Throws", (string city) =>
            city.Length > 0
                ? throw new InvalidOperationException($"No data for {city}", new Exception("inner"))
                : "", "city");
        resolver.Tool("ThrowsAsync", "Throws", async (string city) =>
        {
            await Task.Yield();
            throw new InvalidOperationException($"No async data for {city}");
#pragma warning disable CS0162
            return "";
#pragma warning restore CS0162
        }, "city");

        var sync = resolver.Resolve(Request("Throws", P("city", "Lisbon")), new TestLambdaContext());
        var async = resolver.Resolve(Request("ThrowsAsync", P("city", "Lisbon")), new TestLambdaContext());

        Assert.Equal("Error when invoking tool: No data for Lisbon", Body(sync));
        Assert.Equal("Error when invoking tool: No async data for Lisbon", Body(async));
        Assert.Equal("ThrowsAsync", async.Response.Function);
    }

    [Fact]
    public void Tool_WithNamedParameters_ReturnsErrorResponseForAnInvalidEnumValue()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Unit", "Unit", (TempUnit unit) => unit.ToString(), "unit");

        var result = resolver.Resolve(Request("Unit", P("unit", "Kelvin")));

        Assert.StartsWith("Error when invoking tool: Requested value 'Kelvin' was not found.", Body(result));
    }

    [Fact]
    public void Tool_WithNamedParameters_BindsRequestAndContextByType()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Ctx", "Context", (string city, BedrockFunctionRequest request, ILambdaContext context) =>
            $"{city} {request.Function} {context.FunctionName}", "city", "ignored", "ignored");

        var result = resolver.Resolve(Request("Ctx", P("city", "Lisbon")),
            new TestLambdaContext { FunctionName = "MyLambda" });

        Assert.Equal("Lisbon Ctx MyLambda", Body(result));
    }

    public interface IForecastService
    {
        string Get(string city);
    }

    private sealed class ForecastService : IForecastService
    {
        public string Get(string city) => $"Sunny in {city}";
    }

    [Fact]
    public void Tool_WithNamedParameters_ResolvesServicesFromDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForecastService, ForecastService>();
        services.AddBedrockResolver();
        var resolver = services.BuildServiceProvider().GetRequiredService<BedrockAgentFunctionResolver>();
        resolver.Tool("Service", "Service", (string city, IForecastService service) => service.Get(city),
            "city", "service");

        var result = resolver.Resolve(Request("Service", P("city", "Lisbon")));

        Assert.Equal("Sunny in Lisbon", Body(result));
    }

    [Fact]
    public void Tool_WithNamedParameters_BindsArrayParameters()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Arrays", "Arrays", (string[] tags, int[] numbers) =>
            $"{string.Join("|", tags)} {numbers.Sum()}", "tags", "numbers");

        var result = resolver.Resolve(Request("Arrays", P("tags", "[\"a\",\"b\"]", "array"),
            P("numbers", "[1,2,3]", "array")));

        Assert.Equal("a|b 6", Body(result));
    }

    [Fact]
    public void Tool_WithEightNamedParameters_BindsEachByName()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Eight", "Eight",
            (string a, string b, string c, string d, string e, string f, string g, string h) =>
                string.Concat(a, b, c, d, e, f, g, h),
            "a", "b", "c", "d", "e", "f", "g", "h");

        var result = resolver.Resolve(Request("Eight", P("h", "8"), P("g", "7"), P("f", "6"), P("e", "5"),
            P("d", "4"), P("c", "3"), P("b", "2"), P("a", "1")));

        Assert.Equal("12345678", Body(result));
    }

    [Fact]
    public void Tool_WithEachArity_BindsEveryParameterInOrder()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("T1", "", (int a) => $"{a}", "a");
        resolver.Tool("T2", "", (int a, int b) => $"{a}{b}", "a", "b");
        resolver.Tool("T3", "", (int a, int b, int c) => $"{a}{b}{c}", "a", "b", "c");
        resolver.Tool("T4", "", (int a, int b, int c, int d) => $"{a}{b}{c}{d}", "a", "b", "c", "d");
        resolver.Tool("T5", "", (int a, int b, int c, int d, int e) => $"{a}{b}{c}{d}{e}",
            "a", "b", "c", "d", "e");
        resolver.Tool("T6", "", (int a, int b, int c, int d, int e, int f) => $"{a}{b}{c}{d}{e}{f}",
            "a", "b", "c", "d", "e", "f");
        resolver.Tool("T7", "", (int a, int b, int c, int d, int e, int f, int g) => $"{a}{b}{c}{d}{e}{f}{g}",
            "a", "b", "c", "d", "e", "f", "g");
        resolver.Tool("T8", "", (int a, int b, int c, int d, int e, int f, int g, int h) =>
            $"{a}{b}{c}{d}{e}{f}{g}{h}", "a", "b", "c", "d", "e", "f", "g", "h");
        var parameters = "abcdefgh".Select((name, i) => P(name.ToString(), (i + 1).ToString(), "integer")).ToArray();

        var bodies = Enumerable.Range(1, 8).Select(n => Body(resolver.Resolve(Request($"T{n}", parameters))));

        Assert.Equal(new[] { "1", "12", "123", "1234", "12345", "123456", "1234567", "12345678" }, bodies);
    }

    [Fact]
    public void Tool_WithNamedParameters_AndNullHandler_Throws()
    {
        var resolver = new BedrockAgentFunctionResolver();

        Assert.Throws<ArgumentNullException>(() =>
            resolver.Tool("Null", "Null", (Func<string, string>)null!, "city"));
    }

    [Fact]
    public void Tool_WithNamedParameters_OverwritesAToolWithTheSameName()
    {
        var resolver = new BedrockAgentFunctionResolver();
        resolver.Tool("Echo", "First", (string city) => $"first {city}", "city");
        resolver.Tool("Echo", "Second", (string city) => $"second {city}", "city");

        var result = resolver.Resolve(Request("Echo", P("city", "Lisbon")));

        Assert.Equal("second Lisbon", Body(result));
    }
}
