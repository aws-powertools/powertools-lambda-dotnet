using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Function;

public class Function
{
    [Logging(LogEvent = true, LoggerOutputCase = LoggerOutputCase.PascalCase, Service = "TestService", 
            CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
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