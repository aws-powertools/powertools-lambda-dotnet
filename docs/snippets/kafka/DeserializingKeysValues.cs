// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Kafka;

// --8<-- [start:key_value_deserialization]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;

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

// --8<-- [start:value_only_deserialization]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;

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
