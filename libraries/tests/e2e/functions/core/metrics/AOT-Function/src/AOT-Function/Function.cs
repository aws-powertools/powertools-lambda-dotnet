using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Metrics;
using Helpers;

namespace AOT_Function;

public static class Function
{
    private static async Task Main()
    {
        Func<APIGatewayProxyRequest, ILambdaContext, APIGatewayProxyResponse> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler,
                new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    [Metrics(Namespace = "Test", Service = "Test", CaptureColdStart = true)]
    public static APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        TestHelper.TestMethod(apigwProxyEvent, context);
        
        return new APIGatewayProxyResponse()
        {
            StatusCode = 200,
            Body = apigwProxyEvent.Body.ToUpper()
        };
    }
}

[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{

}