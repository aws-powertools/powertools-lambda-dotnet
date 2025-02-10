using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Logging.Serializers;
using Helpers;

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
        TestHelper.TestMethod(apigwProxyEvent);
        
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