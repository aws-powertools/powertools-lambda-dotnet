using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Tests;

/// <summary>
/// UseMetrics() and WithMetrics() give each request its own metrics context, so metrics first
/// added in an awaited service method are seen by the endpoint's Flush.
/// Runs with AWS_LAMBDA_MAX_CONCURRENCY=4 so the single-concurrency fallback context can't make
/// these pass without the per-request context.
/// </summary>
[Collection("Metrics")]
public class RequestContextTests : IDisposable
{
    private const string MaxConcurrencyEnv = "AWS_LAMBDA_MAX_CONCURRENCY";
    private readonly string? _previousMaxConcurrency;
    private readonly List<string> _output = new();
    private readonly IMetrics _metrics;

    public RequestContextTests()
    {
        _previousMaxConcurrency = Environment.GetEnvironmentVariable(MaxConcurrencyEnv);
        Environment.SetEnvironmentVariable(MaxConcurrencyEnv, "4");
        Metrics.ResetForTest();

        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        consoleWrapper.When(c => c.WriteLine(Arg.Any<string>()))
            .Do(call => { lock (_output) _output.Add(call.Arg<string>()); });
        _metrics = new Metrics(Substitute.For<IPowertoolsConfigurations>(), nameSpace: "TestNamespace",
            consoleWrapper: consoleWrapper);
    }

    public void Dispose()
    {
        Metrics.ResetForTest();
        ColdStartTracker.ResetColdStart();
        Environment.SetEnvironmentVariable(MaxConcurrencyEnv, _previousMaxConcurrency);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Endpoint_Flush_Sees_Metric_First_Added_In_Awaited_Helper_And_Requests_Do_Not_Share_Metrics(
        bool useMiddleware, bool useFilter)
    {
        var app = CreateApp();
        if (useMiddleware) app.UseMetrics();
        var endpoint = app.MapGet("/test", async (IMetrics m, bool flush) =>
        {
            await AddFirstMetricInHelper(m, flush ? "Flushed" : "Leftover");
            if (flush) m.Flush();
            return Results.Ok();
        });
        if (useFilter) endpoint.WithMetrics();

        await app.StartAsync();
        var client = app.GetTestClient();

        // The first request adds a metric and never flushes; the second one must not emit it
        Assert.Equal(200, (int)(await client.GetAsync("/test?flush=false")).StatusCode);
        Assert.Equal(200, (int)(await client.GetAsync("/test?flush=true")).StatusCode);
        await app.StopAsync();

        var blob = Assert.Single(_output, line => line.Contains("_aws"));
        Assert.Contains("\"Flushed\":1", blob);
        Assert.DoesNotContain("Leftover", blob);
    }

    [Fact]
    public async Task When_Middleware_And_Filter_Are_Both_Used_The_Filter_Context_Ends_And_The_Request_Context_Is_Still_Used()
    {
        var app = CreateApp();
        app.UseMetrics();
        // Runs inside the UseMetrics request context but outside the endpoint filter's context
        app.Use(async (context, next) =>
        {
            await AddFirstMetricInHelper(_metrics, "FromMiddleware");
            await next();
            _metrics.Flush();
        });
        app.MapGet("/test", async (IMetrics m) =>
        {
            await AddFirstMetricInHelper(m, "FromEndpoint");
            m.Flush();
            return Results.Ok();
        }).WithMetrics();

        await app.StartAsync();
        Assert.Equal(200, (int)(await app.GetTestClient().GetAsync("/test")).StatusCode);
        await app.StopAsync();

        var blobs = _output.Where(line => line.Contains("_aws")).ToList();
        Assert.Equal(2, blobs.Count);
        Assert.Contains("\"FromEndpoint\":1", blobs[0]);
        Assert.DoesNotContain("FromMiddleware", blobs[0]);
        Assert.Contains("\"FromMiddleware\":1", blobs[1]);
        Assert.DoesNotContain("FromEndpoint", blobs[1]);
    }

    private WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(_metrics);
        builder.WebHost.UseTestServer();
        return builder.Build();
    }

    /// <summary>
    /// The first metrics call of the request happens here, after resuming on another thread.
    /// </summary>
    private static async Task AddFirstMetricInHelper(IMetrics metrics, string name)
    {
        await new ResumeOnNewThread();
        metrics.AddMetric(name, 1, MetricUnit.Count);
    }
}
