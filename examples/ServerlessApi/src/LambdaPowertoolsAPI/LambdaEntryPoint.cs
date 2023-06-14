using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Logging; // We are adding logging
using AWS.Lambda.Powertools.Tracing; // We are adding tracing
using AWS.Lambda.Powertools.Metrics; // We are adding metrics

namespace LambdaPowertoolsAPI;

/// <summary>
/// This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the 
/// actual Lambda function entry point. The Lambda handler field should be set to
/// 
/// LambdaPowertoolsAPI::LambdaPowertoolsAPI.LambdaEntryPoint::FunctionHandlerAsync
/// </summary>
public class LambdaEntryPoint :

    // The base class must be set to match the AWS service invoking the Lambda function. If not Amazon.Lambda.AspNetCoreServer
    // will fail to convert the incoming request correctly into a valid ASP.NET Core request.
    //
    // API Gateway REST API                         -> Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
    // API Gateway HTTP API payload version 1.0     -> Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
    // API Gateway HTTP API payload version 2.0     -> Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction
    // Application Load Balancer                    -> Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction
    // 
    // Note: When using the AWS::Serverless::Function resource with an event type of "HttpApi" then payload version 2.0
    // will be the default and you must make Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction the base class.

    Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
{
    /// <summary>
    /// The builder has configuration, logging and Amazon API Gateway already configured. The startup class
    /// needs to be configured in this method using the UseStartup<>() method.
    /// </summary>
    /// <param name="builder"></param>
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .UseStartup<Startup>();


        Console.WriteLine("Startup done");
    }


    // We are defining some default dimensions.
    private Dictionary<string, string> _defaultDimensions = new Dictionary<string, string>{
        {"Environment", Environment.GetEnvironmentVariable("ENVIRONMENT") ??  "Unknown"},
        {"Runtime",Environment.Version.ToString()}
    };

    [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
    [Logging(CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest, LogEvent = true)] // we are enabling logging, it needs to be added on method which have Lambda event
    [Tracing] // Adding a tracing attribute here we will see additional function call which might be important in terms of debugging
    [Metrics] // Metrics need to be initialized the best place is entry point in opposite on adding attribute on each controller.
    public override Task<APIGatewayProxyResponse> FunctionHandlerAsync(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
    {
        if (!_defaultDimensions.ContainsKey("Version"))
            _defaultDimensions.Add("Version", lambdaContext.FunctionVersion ?? "Unknown");
            
        // Setting the default dimensions. They will be added to every emitted metric.
        Metrics.SetDefaultDimensions(_defaultDimensions);
        return base.FunctionHandlerAsync(request, lambdaContext);
    }
}