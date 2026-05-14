// This file is referenced by docs/utilities/jmespath-functions.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.JmespathFunctions;

// --8<-- [start:transform_json]
var transformer = JsonTransformer.Parse("powertools_json(body).customerId");
using var result = transformer.Transform(doc.RootElement);

Logger.LogInformation(result.RootElement.GetRawText()); // "dd4649e6-2484-4993-acb8-0f9123103394"
// --8<-- [end:transform_json]

// --8<-- [start:idempotency_event_key_jmespath]
Idempotency.Configure(builder =>
        builder
            .WithOptions(optionsBuilder =>
                optionsBuilder.WithEventKeyJmesPath("powertools_json(Body).[\"user_id\", \"product_id\"]"))
            .UseDynamoDb("idempotency_table"));
// --8<-- [end:idempotency_event_key_jmespath]

// --8<-- [start:powertools_base64]
var transformer = JsonTransformer.Parse("powertools_base64(body).customerId");
using var result = transformer.Transform(doc.RootElement);

Logger.LogInformation(result.RootElement.GetRawText()); // "dd4649e6-2484-4993-acb8-0f9123103394"
// --8<-- [end:powertools_base64]

// --8<-- [start:powertools_base64_gzip]
var transformer = JsonTransformer.Parse("powertools_base64_gzip(body).customerId");
using var result = transformer.Transform(doc.RootElement);

Logger.LogInformation(result.RootElement.GetRawText()); // "dd4649e6-2484-4993-acb8-0f9123103394"
// --8<-- [end:powertools_base64_gzip]
