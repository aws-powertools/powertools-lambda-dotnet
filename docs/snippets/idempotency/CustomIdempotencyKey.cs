// This file is referenced by docs/utilities/idempotency.md
// via pymdownx.snippets (mkdocs).

using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using Docs.IdempotencySnippets;

namespace Docs.IdempotencySnippets.CustomIdempotencyKey.EventKeyJmespathPayment
{
internal static class Example
{
    public static void Run()
    {
// --8<-- [start:event_key_jmespath_payment]
    Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder.WithEventKeyJmesPath("powertools_json(Body).[\"user_id\", \"product_id\"]"))
                .UseDynamoDb("idempotency_table"));
// --8<-- [end:event_key_jmespath_payment]
    }
}
}


namespace Docs.IdempotencySnippets.CustomIdempotencyKey.CustomKeyPrefix
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
}


namespace Docs.IdempotencySnippets.CustomIdempotencyKey.ThrowOnNoIdempotencyKey
{
public class App
{
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
      return Task.FromResult(new OrderResult());
    }
// --8<-- [end:throw_on_no_idempotency_key]
}
}
