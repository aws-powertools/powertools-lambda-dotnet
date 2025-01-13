using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;
using Helpers;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Function;

public class Function
{
    [Logging(LogEvent = true, LoggerOutputCase = LoggerOutputCase.PascalCase, Service = "TestService", 
            CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        TestHelper.TestMethod(apigwProxyEvent);

        return new APIGatewayProxyResponse()
        {
            StatusCode = 200,
            Body = apigwProxyEvent.Body.ToUpper()
        };
    }
}