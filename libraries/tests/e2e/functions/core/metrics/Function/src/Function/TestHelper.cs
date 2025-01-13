using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Metrics;

namespace Helpers;

public class TestHelper
{
    private static readonly Dictionary<string, string> DefaultDimensions = new()
    {
        {"Environment", "Prod"},
        {"Another", "One"}
    };

    public static void TestMethod(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        Metrics.SetDefaultDimensions(DefaultDimensions);
        Metrics.AddMetric("Invocation", 1, MetricUnit.Count);
     
        Metrics.AddDimension("Memory","MemoryLimitInMB");
        Metrics.AddMetric("Memory with Environment dimension", context.MemoryLimitInMB, MetricUnit.Megabytes);
        
        // Publish a metric with standard resolution i.e. StorageResolution = 60
        Metrics.AddMetric("Standard resolution", 1, MetricUnit.Count, MetricResolution.Standard);

        // Publish a metric with high resolution i.e. StorageResolution = 1
        Metrics.AddMetric("High resolution", 1, MetricUnit.Count, MetricResolution.High);

        // The last parameter (storage resolution) is optional
        Metrics.AddMetric("Default resolution", 1, MetricUnit.Count);
        
        Metrics.AddMetadata("RequestId", apigwProxyEvent.RequestContext.RequestId);
        
        Metrics.PushSingleMetric(
            metricName: "SingleMetric",
            value: 1,
            unit: MetricUnit.Count,
            nameSpace: "Test",
            service: "Test",
            defaultDimensions: new Dictionary<string, string>
            {
                {"FunctionContext", "$LATEST"}
            });
    }
}