using System.Net;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PackageSmoke.Tests;

public static class ValuesApi
{
    public static WebApplication Build()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IMetrics>(_ => new MetricsBuilder()
            .WithNamespace(MetricsFunction.Namespace)
            .WithService(MetricsFunction.Service)
            .Build());

        var app = builder.Build();
        app.UseMetrics();
        app.MapGet("/values", (IMetrics metrics) =>
        {
            metrics.AddMetric("ValuesRequested", 1, MetricUnit.Count);
            metrics.Flush();
            return Results.Ok(new[] { "value1", "value2" });
        }).WithMetrics();

        return app;
    }
}

public class AspNetCoreSmokeTests
{
    [Fact]
    public async Task Minimal_Api_Endpoint_Returns_Values_And_Emits_Metric()
    {
        await using var app = ValuesApi.Build();
        await app.StartAsync();
        using var client = app.GetTestClient();

        using var console = new ConsoleCapture();
        var response = await client.GetAsync("/values");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[\"value1\",\"value2\"]", await response.Content.ReadAsStringAsync());

        var emf = Assert.Single(console.JsonLines(), l => l.TryGetProperty("ValuesRequested", out _));
        Assert.Equal(MetricsFunction.Service, emf.GetProperty("Service").GetString());
        Assert.Equal(MetricsFunction.Namespace,
            emf.GetProperty("_aws").GetProperty("CloudWatchMetrics")[0].GetProperty("Namespace").GetString());
    }
}
