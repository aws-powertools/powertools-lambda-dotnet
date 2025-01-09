using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Metrics;

namespace AOT_Function;

public class Function
{
    private static Dictionary<string, string> _defaultDimensions = new()
    {
        {"Environment", "Prod"},
        {"Another", "One"}
    }; 
    
    private static async Task Main()
    {
        Func<APIGatewayProxyRequest, ILambdaContext, APIGatewayProxyResponse> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler,
                new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    [Metrics(Namespace = "Test", Service = "Test", CaptureColdStart = true)]
    public static APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        Metrics.SetDefaultDimensions(_defaultDimensions);
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
        
        return new APIGatewayProxyResponse()
        {
            StatusCode = 200,
            Body = apigwProxyEvent.Body.ToUpper()
        };
    }
}

[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{

}