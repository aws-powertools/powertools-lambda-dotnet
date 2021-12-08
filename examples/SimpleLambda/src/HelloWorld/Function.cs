using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.PowerTools.Logging;
using AWS.Lambda.PowerTools.Metrics;
using Microsoft.Extensions.Logging;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HelloWorld
{
    public class MyClass
    {
        public string FirsName { get; set; }
        public string LastName { get; set; }
        public List<MyClass> Children { get; set; }
    }
    
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

        private static List<MyClass> GetLogData()
        {
            return new()
            {
                new MyClass
                {
                    FirsName = "Amir",
                    LastName = "Khairalomoum",
                    Children = new List<MyClass>()
                    {
                        new() {FirsName = "Ava", LastName = "Khairalomoum"},
                        new() {FirsName = "Ayden", LastName = "Khairalomoum"}
                    }
                },
                new MyClass
                {
                    FirsName = "Angelique",
                    LastName = "Terrier",
                    Children = new List<MyClass>()
                    {
                        new() {FirsName = "Ava", LastName = "Khairalomoum"},
                        new() {FirsName = "Ayden", LastName = "Khairalomoum"}
                    }
                }
            };
        }

        [Metrics(serviceName: "lambda-example", metricsNamespace: "dotnet-lambdapowertools", captureColdStart: true)]
        [Logging(LogEvent = true, SamplingRate = 0.7)]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            try
            {
                Logger.AppendKey("test", "willBeLogged");
                Logger.LogInformation("{people}", GetLogData());
                
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
