using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Logging.Serializers;

namespace AOT_Function;

public static class Function
{
    private static async Task Main()
    {
        Func<APIGatewayProxyRequest, ILambdaContext, APIGatewayProxyResponse> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler,
                new PowertoolsSourceGeneratorSerializer<LambdaFunctionJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    [Logging(LogEvent = true, LoggerOutputCase = LoggerOutputCase.PascalCase, Service = "TestService", 
        CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
    public static APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        Logger.LogInformation("Processing request started");
        
        var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;
        var lookupInfo = new Dictionary<string, object>()
        {
            {"LookupInfo", new Dictionary<string, object>{{ "LookupId", requestContextRequestId }}}
        };  
        
        var customKeys = new Dictionary<string, string>
        {
            {"test1", "value1"}, 
            {"test2", "value2"}
        };
        
        Logger.AppendKeys(lookupInfo);
        Logger.AppendKeys(customKeys);
        
        Logger.LogWarning("Warn with additional keys");
        
        Logger.RemoveKeys("test1", "test2");
        
        var error = new InvalidOperationException("Parent exception message",
            new ArgumentNullException(nameof(apigwProxyEvent),
                new Exception("Very important nested inner exception message")));
        Logger.LogError(error, "Oops something went wrong");
        
        return new APIGatewayProxyResponse()
        {
            StatusCode = 200,
            Body = apigwProxyEvent.Body.ToUpper()
        };
    }
}

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{

}