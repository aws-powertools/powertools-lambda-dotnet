// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).
//
// Each region is a top-level-statements Lambda function. To compile it inside the snippets library,
// it is wrapped in a namespace and a static method. The line that opens the wrapper class/method
// sits inside the region but carries a "hidden" snippet marker, so pymdownx.snippets drops it.

using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;

namespace AWS.Lambda.Powertools.Docs.Snippets.Kafka.TopLevelDeployment
{
// --8<-- [start:top_level_deployment]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Avro;
using AWS.Lambda.Powertools.Logging;

public static class Function { public static async Task RunAsync() { // --8<-- [start:hidden]
string Handler(ConsumerRecords<string, CustomerProfile> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        Logger.LogInformation("Record Value: {@record}", record.Value);
    }

    return "Processed " + records.Count() + " records";
}
await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<string, CustomerProfile>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaAvroSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
    .Build()
    .RunAsync();
// --8<-- [end:top_level_deployment]
} }
}
