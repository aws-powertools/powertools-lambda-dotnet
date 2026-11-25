using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Metrics;
using Xunit;

namespace PackageSmoke.Tests;

public class MetricsFunction
{
    // Metrics is configured once per process, so every handler in this project shares the namespace and service.
    public const string Namespace = "PackageSmoke";
    public const string Service = "smoke-metrics";

    [Metrics(Namespace = Namespace, Service = Service)]
    public string FunctionHandler(string input, ILambdaContext context)
    {
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
        return input.ToUpperInvariant();
    }
}

public class MetricsSmokeTests
{
    [Fact]
    public void Handler_Flushes_Metric_In_Embedded_Metric_Format()
    {
        var context = new TestLambdaContext { FunctionName = "PackageSmoke-Metrics" };

        using var console = new ConsoleCapture();
        var result = new MetricsFunction().FunctionHandler("hello", context);

        Assert.Equal("HELLO", result);

        var emf = Assert.Single(console.JsonLines(), l => l.TryGetProperty("SuccessfulBooking", out _));
        Assert.Equal(1, emf.GetProperty("SuccessfulBooking").GetDouble());
        Assert.Equal(MetricsFunction.Service, emf.GetProperty("Service").GetString());

        var directive = emf.GetProperty("_aws").GetProperty("CloudWatchMetrics")[0];
        Assert.Equal(MetricsFunction.Namespace, directive.GetProperty("Namespace").GetString());
        var metric = directive.GetProperty("Metrics")[0];
        Assert.Equal("SuccessfulBooking", metric.GetProperty("Name").GetString());
        Assert.Equal("Count", metric.GetProperty("Unit").GetString());
    }
}
