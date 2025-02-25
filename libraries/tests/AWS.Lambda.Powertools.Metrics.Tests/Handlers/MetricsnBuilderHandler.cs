using System.Collections.Generic;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

public class MetricsnBuilderHandler
{
    private readonly IMetrics _metrics;

    // Allow injection of IMetrics for testing
    public MetricsnBuilderHandler(IMetrics metrics = null)
    {
        _metrics = metrics ?? new MetricsBuilder()
            .WithCaptureColdStart(true)
            .WithService("testService")
            .WithNamespace("dotnet-powertools-test")
            .WithDefaultDimensions(new Dictionary<string, string>
            {
                { "Environment", "Prod1" },
                { "Another", "One" }
            }).Build();
    }

    [Metrics]
    public void Handler(ILambdaContext context)
    {
        _metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    }
    
    public void HandlerSingleMetric()
    {
        _metrics.PushSingleMetric("SuccessfulBooking", 1, MetricUnit.Count);
    }
    
    public void HandlerSingleMetricDimensions()
    {
        _metrics.PushSingleMetric("SuccessfulBooking", 1, MetricUnit.Count, defaultDimensions: _metrics.Options.DefaultDimensions);
    }
   
}