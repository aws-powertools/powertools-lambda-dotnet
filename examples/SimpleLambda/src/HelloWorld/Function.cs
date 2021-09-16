using System.Collections.Generic;
using System.Net.Http;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using AWS.Lambda.PowerTools.Metrics;
using AWS.Lambda.PowerTools.Metrics.Model;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HelloWorld
{
    public partial class Function
    {
        private static readonly HttpClient client = new HttpClient();
        
        //private static Metrics _metricsLogger = new Metrics("dotnet-lambdapowertools", "lambda-example");
        
        //private static async Task<string> GetCallingIP()
        //{
        //    client.DefaultRequestHeaders.Accept.Clear();
        //    client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

        //    var msg = await client.GetStringAsync("http://checkip.amazonaws.com/").ConfigureAwait(continueOnCapturedContext: false);

        //    return msg.Replace("\n", "");
        //}
        
        //[Metrics("hello-world", "lambda-example")]
        [Metrics]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            
            // CAPTURE METHOD EXECUTION METRICS FOR GETCALLINGIP (TWICE - TO REPRESENT THIS SPECIFIC METRIC AS AN ARRAY)
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            //var location = await GetCallingIP();
            //watch.Stop();
            
            using (var logger = new Metrics("dotnet-lambdapowertools-single", "lambda-example"))
            {
                logger.AddDimension("Metric Type", "Single");
                logger.AddMetric("SingleExecution", 1, MetricUnit.COUNT);
            }
            
            Metrics.AddDimension("Metric Type", "Aggregate");
            Metrics.AddDimension("Method Execution Metrics", "getCallingIP");
            Metrics.AddMetric("ElapsedExecutionTime", 1234, MetricUnit.MILLISECONDS);

            //watch = System.Diagnostics.Stopwatch.StartNew();
            //location = await GetCallingIP();
            //watch.Stop();

            Metrics.AddMetric("ElapsedExecutionTime", 456124, MetricUnit.MILLISECONDS);


            Metrics.AddMetric("SuccessfulLocations", 1, MetricUnit.COUNT);

            Metrics.Flush();

            return new APIGatewayProxyResponse
            {
                Body = $"",
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
