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
using AWS.Lambda.Powertools.Docs.Snippets.Idempotency;

namespace AWS.Lambda.Powertools.Docs.Snippets.Idempotency.DynamoDbConfiguration.DynamodbPersistenceStoreBuilder
{
using Idempotency = global::AWS.Lambda.Powertools.Idempotency.Idempotency;
internal static class Example
{
    public static void Run()
    {
// --8<-- [start:dynamodb_persistence_store_builder]
new DynamoDBPersistenceStoreBuilder()
    .WithTableName("TABLE_NAME")
    .WithKeyAttr("idempotency_key")
    .WithExpiryAttr("expires_at")
    .WithStatusAttr("current_status")
    .WithDataAttr("result_data")
    .WithValidationAttr("validation_key")
    .WithInProgressExpiryAttr("in_progress_expires_at")
    .Build();
// --8<-- [end:dynamodb_persistence_store_builder]
    }
}
}


namespace AWS.Lambda.Powertools.Docs.Snippets.Idempotency.DynamoDbConfiguration.CustomAmazonDynamodbClient
{
using Idempotency = global::AWS.Lambda.Powertools.Idempotency.Idempotency;
public class Function
{
// --8<-- [start:custom_amazon_dynamodb_client]
    public Function()
    {
        AmazonDynamoDBClient customClient = new AmazonDynamoDBClient(RegionEndpoint.APSouth1);
      
        Idempotency.Configure(builder => 
            builder.UseDynamoDb(storeBuilder => 
                storeBuilder.
                    WithTableName("TABLE_NAME")
                    .WithDynamoDBClient(customClient)
            ));
    }
// --8<-- [end:custom_amazon_dynamodb_client]
}
}


namespace AWS.Lambda.Powertools.Docs.Snippets.Idempotency.DynamoDbConfiguration.DynamodbCompositePrimaryKey
{
using Idempotency = global::AWS.Lambda.Powertools.Idempotency.Idempotency;
internal static class Example
{
    public static void Run()
    {
// --8<-- [start:dynamodb_composite_primary_key]
    Idempotency.Configure(builder => 
        builder.UseDynamoDb(storeBuilder => 
            storeBuilder.
                WithTableName("TABLE_NAME")
                .WithSortKeyAttr("sort_key")
        ));
// --8<-- [end:dynamodb_composite_primary_key]
    }
}
}
