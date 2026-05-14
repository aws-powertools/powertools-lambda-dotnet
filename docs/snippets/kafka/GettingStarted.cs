// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Kafka;

// --8<-- [start:class_library_deployment]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Avro;
using AWS.Lambda.Powertools.Logging;

[assembly: LambdaSerializer(typeof(PowertoolsKafkaAvroSerializer))] // Use PowertoolsKafkaAvroSerializer for Avro serialization

namespace MyKafkaConsumer;

public class Function
{
    public string FunctionHandler(ConsumerRecords<string, CustomerProfile> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            Logger.LogInformation("Record Value: {@record}", record.Value);
        }

        return "Processed " + records.Count() + " records";
    }
}
// --8<-- [end:class_library_deployment]

// --8<-- [start:top_level_deployment]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Avro;
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
        new PowertoolsKafkaAvroSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
    .Build()
    .RunAsync();
// --8<-- [end:top_level_deployment]
