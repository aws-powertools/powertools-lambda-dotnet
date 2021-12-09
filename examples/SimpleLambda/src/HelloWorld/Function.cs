using System;
using System.Collections.Generic;
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
        private static readonly HttpClient Client = new HttpClient();

        private static async Task<string> GetCallingIp()
        {    
            Metrics.PushSingleMetric(
                metricName: "CallingIP", 
                value: 1, 
                unit: MetricUnit.COUNT,
                metricsNamespace: "dotnet-lambdapowertools",
                serviceName: "lambda-example",
                defaultDimensions: new Dictionary<string, string>{
                    {"Metric Type", "Single"}
                });                
            
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

            var msg = await Client.GetStringAsync("https://checkip.amazonaws.com/").ConfigureAwait(continueOnCapturedContext: false);

            return msg.Replace("\n", "");
        }

        [Logging(LogEvent = true, SamplingRate = 0.7)]
        [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
        [Metrics(ServiceName = "lambda-example", MetricsNamespace = "dotnet-lambdapowertools", CaptureColdStart = true)]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            try
            {
                Logger.AppendKey("test", "willBeLogged");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var location = await GetCallingIp();
                watch.Stop();   

                Metrics.AddMetric("ElapsedExecutionTime", watch.ElapsedMilliseconds, MetricUnit.MILLISECONDS);
                Metrics.AddMetric("SuccessfulLocations", 1, MetricUnit.COUNT);
                
                return new APIGatewayProxyResponse
                {
                    Body = JsonSerializer.Serialize(new { location }),
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
