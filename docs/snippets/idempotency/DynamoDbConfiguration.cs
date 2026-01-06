// This file is referenced by docs/utilities/idempotency.md
// via pymdownx.snippets (mkdocs).

// --8<-- [start:dynamodb_persistence_store_builder]
new DynamoDBPersistenceStoreBuilder()
    .WithTableName("TABLE_NAME")
    .WithKeyAttr("idempotency_key")
    .WithExpiryAttr("expires_at")
    .WithStatusAttr("current_status")
    .WithDataAttr("result_data")
    .WithValidationAttr("validation_key")
    .WithInProgressExpiryAttr("in_progress_expires_at")
    .Build()
// --8<-- [end:dynamodb_persistence_store_builder]

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

// --8<-- [start:dynamodb_composite_primary_key]
    Idempotency.Configure(builder => 
        builder.UseDynamoDb(storeBuilder => 
            storeBuilder.
                WithTableName("TABLE_NAME")
                .WithSortKeyAttr("sort_key")
        ));
// --8<-- [end:dynamodb_composite_primary_key]
