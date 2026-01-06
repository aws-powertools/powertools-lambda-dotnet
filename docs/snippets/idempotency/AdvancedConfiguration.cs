// This file is referenced by docs/utilities/idempotency.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Idempotency;

// --8<-- [start:register_lambda_context]
    public class Function
    {
        public Function()
        {
            Idempotency.Configure(builder => builder.UseDynamoDb("idempotency_table"));
        }
        
        public Task<string> FunctionHandler(string input, ILambdaContext context)
        {
            Idempotency.RegisterLambdaContext(context);
            MyInternalMethod("hello", "world");
            return Task.FromResult(input.ToUpper());
        }

        [Idempotent]
        private string MyInternalMethod(string argOne, [IdempotencyKey] string argTwo) {
            return "something";
        }
    }
// --8<-- [end:register_lambda_context]

// --8<-- [start:exception_not_affecting_record]
    public class Function
    {
        public Function()
        {
            Idempotency.Configure(builder => builder.UseDynamoDb("idempotency_table"));
        }
        
        public Task<string> FunctionHandler(string input, ILambdaContext context)
        {
            Idempotency.RegisterLambdaContext(context);
            // If an exception is thrown here, no idempotent record will ever get created as the
            // idempotent method does not get called

            MyInternalMethod("hello", "world");

            // This exception will not cause the idempotent record to be deleted, since it
            // happens after the decorated method has been successfully called    
            throw new Exception();
        }

        [Idempotent]
        private string MyInternalMethod(string argOne, [IdempotencyKey] string argTwo) {
            return "something";
        }
    }
// --8<-- [end:exception_not_affecting_record]

// --8<-- [start:idempotency_options_builder]
new IdempotencyOptionsBuilder()
    .WithEventKeyJmesPath("id")
    .WithPayloadValidationJmesPath("paymentId")
    .WithThrowOnNoIdempotencyKey(true)
    .WithExpiration(TimeSpan.FromMinutes(1))
    .WithUseLocalCache(true)
    .WithHashFunction("MD5")
    .Build();
// --8<-- [end:idempotency_options_builder]

// --8<-- [start:enable_local_cache]
    new IdempotencyOptionsBuilder()
        .WithUseLocalCache(true)
        .Build();
// --8<-- [end:enable_local_cache]

// --8<-- [start:response_hook]
Idempotency.Config()
    .WithConfig(IdempotencyOptions.Builder()
        .WithEventKeyJmesPath("powertools_json(body).address")
        .WithResponseHook((responseData, dataRecord) => {
            if (responseData is APIGatewayProxyResponse proxyResponse)
            {
                proxyResponse.Headers ??= new Dictionary<string, string>();
                proxyResponse.Headers["x-idempotency-response"] = "true";
                proxyResponse.Headers["x-idempotency-expiration"] = dataRecord.ExpiryTimestamp.ToString();
                return proxyResponse;
            }
            return responseData;
        })
        .Build())
    .WithPersistenceStore(DynamoDBPersistenceStore.Builder()
        .WithTableName(Environment.GetEnvironmentVariable("IDEMPOTENCY_TABLE"))
        .Build())
    .Configure();
// --8<-- [end:response_hook]

// --8<-- [start:with_json_serialization_context]
Idempotency.Configure(builder =>
    builder.WithJsonSerializationContext(LambdaFunctionJsonSerializerContext.Default));
// --8<-- [end:with_json_serialization_context]

// --8<-- [start:with_json_serialization_context_full_example]
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
        return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(response, typeof(Response), LambdaFunctionJsonSerializerContext.Default),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
    }
}

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(Response))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
}
// --8<-- [end:with_json_serialization_context_full_example]
