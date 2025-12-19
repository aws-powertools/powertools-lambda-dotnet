// This file is referenced by docs/utilities/idempotency.md
// via pymdownx.snippets (mkdocs).

// --8<-- [start:event_key_jmespath_payment]
    Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder.WithEventKeyJmesPath("powertools_json(Body).[\"user_id\", \"product_id\"]"))
                .UseDynamoDb("idempotency_table"));
// --8<-- [end:event_key_jmespath_payment]

namespace AWS.Lambda.Powertools.Docs.Snippets.Idempotency
{
// --8<-- [start:custom_key_prefix]
public class Function
{
    public Function()
    {
        var tableName = Environment.GetEnvironmentVariable("IDEMPOTENCY_TABLE_NAME");
        Idempotency.Configure(builder => builder.UseDynamoDb(tableName));
    }

    [Idempotent(KeyPrefix = "MyCustomKeyPrefix")]
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
    {
        return TestHelper.TestMethod(apigwProxyEvent);
    }
}
// --8<-- [end:custom_key_prefix]

// --8<-- [start:throw_on_no_idempotency_key]
    public App() 
    {
      Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder
                        // Requires "user"."uid" and "orderId" to be present
                        .WithEventKeyJmesPath("[user.uid, orderId]")
                        .WithThrowOnNoIdempotencyKey(true))
                .UseDynamoDb("TABLE_NAME"));
    }

    [Idempotent]
    public Task<OrderResult> FunctionHandler(Order input, ILambdaContext context)
    {
      // ...
    }
// --8<-- [end:throw_on_no_idempotency_key]
}
