using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Idempotency;

namespace AOT_Function;

public static class Function
{
    private static async Task Main()
    {
        var tableName = Environment.GetEnvironmentVariable("IDEMPOTENCY_TABLE_NAME");
        Idempotency.Configure(builder =>
            builder
                .WithJsonSerializationContext(LambdaFunctionJsonSerializerContext.Default)
                .WithOptions(optionsBuilder => optionsBuilder
                    .WithExpiration(TimeSpan.FromHours(1)))
                .UseDynamoDb(storeBuilder => storeBuilder
                    .WithTableName(tableName)
                ));

        Func<APIGatewayProxyRequest, ILambdaContext, APIGatewayProxyResponse> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler,
                new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    [Idempotent]
    public static APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent,
        ILambdaContext context)
    {
        return TestHelperAot.TestMethod(apigwProxyEvent);
    }
}

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(Response))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
}