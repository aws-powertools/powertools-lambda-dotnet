using System.Collections.Generic;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

public class DefaultDimensionsHandler
{
    public DefaultDimensionsHandler()
    {
        Metrics.Configure(options =>
        {
            options.DefaultDimensions = new Dictionary<string, string>
            {
                { "Environment", "Prod" },
                { "Another", "One" }
            };
        });
    }

    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService", CaptureColdStart = true)]
    public void Handler()
    {
        // Default dimensions are already set
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    }

    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService", CaptureColdStart = true)]
    public void HandlerWithContext(ILambdaContext context)
    {
        // Default dimensions are already set and adds FunctionName dimension
        Metrics.AddMetric("Memory", 10, MetricUnit.Megabytes);
    }
}

public class MetricsDependencyInjectionOptionsHandler
{
    private readonly IMetrics _metrics;

    // Allow injection of IMetrics for testing
    public MetricsDependencyInjectionOptionsHandler(IMetrics metrics = null)
    {
        _metrics = metrics ?? Metrics.Configure(options =>
        {
            options.DefaultDimensions = new Dictionary<string, string>
            {
                { "Environment", "Prod" },
                { "Another", "One" }
            };
        });
    }

    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService", CaptureColdStart = true)]
    public void Handler()
    {
        _metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    }
}