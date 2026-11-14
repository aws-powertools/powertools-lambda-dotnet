// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).
//
// Each region is a top-level-statements Lambda function. To compile it inside the snippets library,
// it is wrapped in a namespace and a static method. The line that opens the wrapper class/method
// sits inside the region but carries a "hidden" snippet marker, so pymdownx.snippets drops it.

using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;

namespace Docs.KafkaSnippets.KeyValueDeserialization
{
// --8<-- [start:key_value_deserialization]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;

public static class Function { public static async Task RunAsync() { // --8<-- [start:hidden]
string Handler(ConsumerRecords<CustomerKey, CustomerProfile> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        Logger.LogInformation("Record Value: {@record}", record.Value);
    }

    return "Processed " + records.Count() + " records";
}

await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<CustomerKey, CustomerProfile>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaProtobufSerializer()) // Use PowertoolsKafkaProtobufSerializer for Protobuf serialization
    .Build()
    .RunAsync();
// --8<-- [end:key_value_deserialization]
} }
}

namespace Docs.KafkaSnippets.ValueOnlyDeserialization
{
// --8<-- [start:value_only_deserialization]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
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
        new PowertoolsKafkaProtobufSerializer()) // Use PowertoolsKafkaProtobufSerializer for Protobuf serialization
    .Build()
    .RunAsync();
// --8<-- [end:value_only_deserialization]
} }
}
