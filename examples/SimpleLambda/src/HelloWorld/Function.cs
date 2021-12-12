using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.PowerTools.Tracing;
using AWS.Lambda.PowerTools.Logging;
using AWS.Lambda.PowerTools.Metrics;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HelloWorld
{
    public class Function
    {
        private static readonly HttpClient client = new HttpClient();

        [Tracing(Namespace = "GetCallingIP", CaptureMode = TracingCaptureMode.Disabled)]
        private static async Task<string> GetCallingIP()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");
            var msg = await client.GetStringAsync("http://checkip.amazonaws.com/").ConfigureAwait(continueOnCapturedContext:false);
            return msg.Replace("\n","");
        }
        
        [Logging(LogEvent = true, SamplingRate = 0.7)]
        [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
        [Metrics(ServiceName = "lambda-example", Namespace = "dotnet-lambdapowertools", CaptureColdStart = true)]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            try
            {
                // Add Metrics
                var watch = Stopwatch.StartNew();
                Metrics.PushSingleMetric(
                    metricName: "CallingIP",
                    value: 1,
                    unit: MetricUnit.COUNT,
                    metricsNamespace: "dotnet-lambdapowertools",
                    serviceName: "lambda-example",
                    defaultDimensions: new Dictionary<string, string>
                    {
                        {"Metric Type", "Single"}
                    });
                var location = await GetCallingIP();
                watch.Stop();
                
                Metrics.AddMetric("ElapsedExecutionTime", watch.ElapsedMilliseconds, MetricUnit.MILLISECONDS);
                Metrics.AddMetric("SuccessfulLocations", 1, MetricUnit.COUNT);
                
                var body = new Dictionary<string, string>
                {
                    { "message", "hello world" },
                    { "location", location }
                };
                
                // Append a log key
                Logger.AppendKey("test", "willBeLogged");
                
                // Log response inside a subsegment scope
                using (var subsegment = Tracing.BeginSubsegmentScope("LoggingResponse"))
                {
                    subsegment.AddAnnotation("Test", "New");
                    Logger.LogInformation("log something out");
                    Logger.LogInformation("{body}", body);
                }

                // Trace parallel tasks
                const int taskNumber = 2;
                var tasks = new Task[taskNumber];
                var entity = Tracing.GetEntity();
                for (var i = 0; i < taskNumber; i++)
                {
                    var name = $"Inline Subsegment {i}";
                    var task = Task.Run(() =>
                    {
                        using (Tracing.BeginSubsegmentScope(name, entity))
                        {
                            Logger.LogInformation($"log something out for inline subsegment {i}");
                        }
                    });
                    tasks[i] = task;
                }
                Task.WaitAll(tasks);

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
    }
}
