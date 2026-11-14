// This file is referenced by docs/utilities/jmespath-functions.md
// via pymdownx.snippets (mkdocs).


using System.Text.Json;
using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.JMESPath;
using AWS.Lambda.Powertools.Logging;

namespace Docs.JmespathSnippets.ExtractingData.TransformJson
{
internal static class Example
{
    public static void Run()
    {
using var doc = JsonDocument.Parse("{\"body\":\"{\\\"customerId\\\":\\\"123\\\"}\"}");
// --8<-- [start:transform_json]
var transformer = JsonTransformer.Parse("powertools_json(body).customerId");
using var result = transformer.Transform(doc.RootElement);

Logger.LogInformation(result.RootElement.GetRawText()); // "dd4649e6-2484-4993-acb8-0f9123103394"
// --8<-- [end:transform_json]
    }
}
}


namespace Docs.JmespathSnippets.ExtractingData.IdempotencyEventKeyJmespath
{
internal static class Example
{
    public static void Run()
    {
// --8<-- [start:idempotency_event_key_jmespath]
Idempotency.Configure(builder =>
        builder
            .WithOptions(optionsBuilder =>
                optionsBuilder.WithEventKeyJmesPath("powertools_json(Body).[\"user_id\", \"product_id\"]"))
            .UseDynamoDb("idempotency_table"));
// --8<-- [end:idempotency_event_key_jmespath]
    }
}
}


namespace Docs.JmespathSnippets.ExtractingData.PowertoolsBase64
{
internal static class Example
{
    public static void Run()
    {
using var doc = JsonDocument.Parse("{\"body\":\"eyJjdXN0b21lcklkIjoiMTIzIn0=\"}");
// --8<-- [start:powertools_base64]
var transformer = JsonTransformer.Parse("powertools_base64(body).customerId");
using var result = transformer.Transform(doc.RootElement);

Logger.LogInformation(result.RootElement.GetRawText()); // "dd4649e6-2484-4993-acb8-0f9123103394"
// --8<-- [end:powertools_base64]
    }
}
}


namespace Docs.JmespathSnippets.ExtractingData.PowertoolsBase64Gzip
{
internal static class Example
{
    public static void Run()
    {
using var doc = JsonDocument.Parse("{\"body\":\"H4sIAAAAAAAA/6tWSi0qyi9SslIyNDJWqgUAQnzs8Q8AAAA=\"}");
// --8<-- [start:powertools_base64_gzip]
var transformer = JsonTransformer.Parse("powertools_base64_gzip(body).customerId");
using var result = transformer.Transform(doc.RootElement);

Logger.LogInformation(result.RootElement.GetRawText()); // "dd4649e6-2484-4993-acb8-0f9123103394"
// --8<-- [end:powertools_base64_gzip]
    }
}
}
