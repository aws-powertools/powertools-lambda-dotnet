// This file is referenced by docs/utilities/idempotency.md
// via pymdownx.snippets (mkdocs).

// --8<-- [start:payload_validation_jmespath]
    Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder
                        .WithEventKeyJmesPath("[userDetail, productId]")
                        .WithPayloadValidationJmesPath("amount"))
                .UseDynamoDb("TABLE_NAME"));
// --8<-- [end:payload_validation_jmespath]
