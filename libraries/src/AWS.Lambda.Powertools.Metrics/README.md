# AWS.Lambda.Powertools.Metrics

Metrics creates custom metrics asynchronously by logging metrics to standard output following [Amazon CloudWatch Embedded Metric Format (EMF)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format.html).

These metrics can be visualized through [Amazon CloudWatch Console](https://aws.amazon.com/cloudwatch/).

## Key features

* Aggregate up to 100 metrics using a single CloudWatch EMF object (large JSON blob)
* Validate against common metric definitions mistakes (metric unit, values, max dimensions, max metrics, etc)
* Metrics are created asynchronously by CloudWatch service, no custom stacks needed
* Context manager to create a one off metric with a different dimension

## Read the docs

For a full list of features go to [awslabs.github.io/aws-lambda-powertools-dotnet/core/metrics/](awslabs.github.io/aws-lambda-powertools-dotnet/core/metrics/)

GitHub: https://github.com/aws-powertools/lambda-dotnet/

## Sample Function

View the full example here: [https://github.com/aws-powertools/lambda-dotnet/tree/develop/examples/Metrics](https://github.com/aws-powertools/lambda-dotnet/tree/develop/examples/Metrics)

```csharp
public class Function
{
    /// <summary>
    /// Lambda Handler
    /// </summary>
    /// <param name="apigwProxyEvent">API Gateway Proxy event</param>
    /// <param name="context">AWS Lambda context</param>
    /// <returns>API Gateway Proxy response</returns>
    [Logging(LogEvent = true)]
    [Metrics(CaptureColdStart = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigwProxyEvent,
        ILambdaContext context)
    {
        var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;

        Logger.LogInformation("Getting ip address from external service");

        // Add Metric to capture the amount of time 
        Metrics.PushSingleMetric(
            metricName: "CallingIP",
            value: 1,
            unit: MetricUnit.Count,
            service: "lambda-powertools-metrics-example",
            defaultDimensions: new Dictionary<string, string>
            {
                { "Metric Type", "Single" }
            });
        
        var watch = Stopwatch.StartNew();
        var location = await GetCallingIp();
        watch.Stop();
        
        Metrics.AddMetric("ElapsedExecutionTime", watch.ElapsedMilliseconds, MetricUnit.Milliseconds);
        Metrics.AddMetric("SuccessfulLocations", 1, MetricUnit.Count);
        
        var lookupRecord = new LookupRecord(lookupId: requestContextRequestId,
            greeting: "Hello AWS Lambda Powertools for .NET", ipAddress: location);

        try
        {
            Metrics.PushSingleMetric(
                metricName: "RecordsSaved",
                value: 1,
                unit: MetricUnit.Count,
                service: "lambda-powertools-metrics-example",
                defaultDimensions: new Dictionary<string, string>
                {
                    { "Metric Type", "Single" }
                });
            
            await SaveRecordInDynamo(lookupRecord);
            
            Metrics.AddMetric("SuccessfulWrites", 1, MetricUnit.Count);
            
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(lookupRecord),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);
            
            return new APIGatewayProxyResponse
            {
                Body = e.Message,
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
```

## Sample output

```json
{
    "_aws": {
        "Timestamp": 1648181318790,
        "CloudWatchMetrics": [
            {
                "Namespace": "AWSLambdaPowertools",
                "Metrics": [
                    {
                        "Name": "CallingIP",
                        "Unit": "Count"
                    }
                ],
                "Dimensions": [
                    [
                        "Metric Type"
                    ],
                    [
                        "Service"
                    ]
                ]
            }
        ]
    },
    "Metric Type": "Single",
    "Service": "lambda-powertools-metrics-example",
    "CallingIP": 1
}
```
