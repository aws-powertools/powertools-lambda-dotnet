# AWS.Lambda.Powertools.Metrics

This package contains classes that can be used as....

## Sample Function

Below is a sample class and Lambda function that illustrates how....

```csharp
public class Function
{
    private static readonly HttpClient client = new HttpClient();

    [Logging(LogEvent = true, SamplingRate = 0.7)]
    [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
    [Metrics(Namespace = "dotnet-lambdapowertools", CaptureColdStart = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {                                
        try
        {
            // Add Metrics
            var watch = Stopwatch.StartNew();
            Metrics.PushSingleMetric(
                metricName: "CallingIP",
                value: 1,
                unit: MetricUnit.Count,
                nameSpace: "dotnet-lambdapowertools",
                service: "lambda-example",
                defaultDimensions: new Dictionary<string, string>
                {
                    {"Metric Type", "Single"}
                });
            var location = await GetCallingIP();
            watch.Stop();
            
            Metrics.AddMetric("ElapsedExecutionTime", watch.ElapsedMilliseconds, MetricUnit.Milliseconds);
            Metrics.AddMetric("SuccessfulLocations", 1, MetricUnit.Count);
            
            var body = new Dictionary<string, string>
            {
                { "message", "hello world" },
                { "location", location }
            };
            
            // Append Log Key
            Logger.AppendKey("test", "willBeLogged");

            // Trace Nested Methods
            NestedMethodsParent();

            // Trace Fluent API
            Tracing.WithSubsegment("LoggingResponse", subsegment =>
            {
                subsegment.AddAnnotation("Test", "New");
                Logger.LogInformation("log something out");
                Logger.LogInformation(body);
            });

            // Trace Parallel Tasks
            var entity = Tracing.GetEntity();
            var task = Task.Run(() =>
            {
                Tracing.WithSubsegment("Inline Subsegment 1", entity, _ =>
                {
                    Logger.LogInformation($"log something out for inline subsegment 1");
                });
            });
            var anotherTask = Task.Run(() =>
            {
                Tracing.WithSubsegment("Inline Subsegment 2", entity, _ =>
                {
                    Logger.LogInformation($"log something out for inline subsegment 2");
                });
            });
            Task.WaitAll(task, anotherTask);

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(ex.Message),
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }

    [Tracing(Namespace = "GetCallingIP", CaptureMode = TracingCaptureMode.Disabled)]
    private static async Task<string> GetCallingIP()
    {
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");
        var msg = await client.GetStringAsync("http://checkip.amazonaws.com/")
            .ConfigureAwait(continueOnCapturedContext:false);
        return msg.Replace("\n","");
    }

    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    private static void NestedMethodsParent()
    {
        Logger.LogInformation($"NestedMethodsParent method");
        NestedMethodsChild(1);
        NestedMethodsChild(1);
    }
    
    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    private static void NestedMethodsChild(int childId)
    {
        Logger.LogInformation($"NestedMethodsChild method for child {childId}");
    }
}
```