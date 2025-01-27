using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Idempotency;
using Helpers;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Function;

public class Function
{
    public Function()
    {
        var tableName = Environment.GetEnvironmentVariable("IDEMPOTENCY_TABLE_NAME");
        Idempotency.Configure(builder => builder.UseDynamoDb(tableName));
    }
    
    [Idempotent]
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        return TestHelper.TestMethod(apigwProxyEvent);
    }
}