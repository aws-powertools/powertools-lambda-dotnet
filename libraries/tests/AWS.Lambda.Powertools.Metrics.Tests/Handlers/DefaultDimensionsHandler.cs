using System.Collections.Generic;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

public class DefaultDimensionsHandler
{
    public DefaultDimensionsHandler()
    {
        Metrics.SetDefaultDimensions(new Dictionary<string, string>
        {
            {"Environment", "Prod"},
            {"Another", "One"}
        });
        // Metrics.SetNamespace("dotnet-powertools-test");
        // Metrics.PushSingleMetric("SingleMetric", 1, MetricUnit.Count);
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