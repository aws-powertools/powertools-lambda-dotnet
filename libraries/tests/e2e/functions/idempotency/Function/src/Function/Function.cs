using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Idempotency;
using Helpers;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Function
{
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
}

namespace IdempotencyAttributeTest
{
    public class Function
    {
        public Function()
        {
            var tableName = Environment.GetEnvironmentVariable("IDEMPOTENCY_TABLE_NAME");
            Idempotency.Configure(builder => builder.UseDynamoDb(tableName));
        }
    
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
        {
            return new APIGatewayProxyResponse
            {
                Body = MyInternalMethod("dummy", apigwProxyEvent.RequestContext.RequestId),
                StatusCode = 200
            };
        }
        
        [Idempotent]
        private string MyInternalMethod(string argOne, [IdempotencyKey] string argTwo) {
            return Guid.NewGuid().ToString();
        }
    }
}

namespace IdempotencyPayloadSubsetTest
{
    public class Function
    {
        public Function()
        {
            var tableName = Environment.GetEnvironmentVariable("IDEMPOTENCY_TABLE_NAME");
            Idempotency.Configure(builder =>
                builder
                    .WithOptions(optionsBuilder =>
                        optionsBuilder.WithEventKeyJmesPath("powertools_json(Body).[\"user_id\", \"product_id\"]"))
                    .UseDynamoDb(tableName));
        }
    
        [Idempotent]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
        {
            return TestHelper.TestMethod(apigwProxyEvent);
        }
    }
}
