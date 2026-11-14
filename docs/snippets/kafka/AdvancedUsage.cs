// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).
//
// Each region is a top-level-statements Lambda function. To compile it inside the snippets library,
// it is wrapped in a namespace and a static method. The line that opens the wrapper class/method
// sits inside the region but carries a "hidden" snippet marker, so pymdownx.snippets drops it.

using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using System.Text.Json.Serialization;

namespace Docs.KafkaSnippets.RecordMetadata
{
// --8<-- [start:record_metadata]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;

public static class Function { public static async Task RunAsync() { // --8<-- [start:hidden]
string Handler(ConsumerRecords<string, CustomerProfile> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        // Log record coordinates for tracing
        Logger.LogInformation("Processing message from topic: {topic}", record.Topic);
        Logger.LogInformation("Partition: {partition}, Offset: {offset}", record.Partition, record.Offset);
        Logger.LogInformation("Produced at: {timestamp}", record.Timestamp);

        // Process message headers
        foreach (var header in record.Headers.DecodedValues())
        {
            Logger.LogInformation($"{header.Key}: {header.Value}");
        }

        // Access the Avro deserialized message content
        CustomerProfile customerProfile = record.Value; // CustomerProfile class is auto-generated from Protobuf schema
        Logger.LogInformation("Processing order for: {fullName}", customerProfile.FullName);
    }

    return "Processed " + records.Count() + " records";
}

await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<string, CustomerProfile>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaProtobufSerializer()) // Use PowertoolsKafkaProtobufSerializer for Protobuf serialization
    .Build()
    .RunAsync();
// --8<-- [end:record_metadata]
} }
}

namespace Docs.KafkaSnippets.ErrorHandling
{
// --8<-- [start:error_handling]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;

public static class Function { public static async Task RunAsync() { // --8<-- [start:hidden]
var successfulRecords = 0;
var failedRecords = 0;

string Handler(ConsumerRecords<string, CustomerProfile> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        try
        {
            // Process each record
            Logger.LogInformation("Processing record from topic: {topic}", record.Topic);
            Logger.LogInformation("Partition: {partition}, Offset: {offset}", record.Partition, record.Offset);

            // Access the deserialized message content
            CustomerProfile customerProfile = record.Value; // CustomerProfile class is auto-generated from Protobuf schema
            ProcessOrder(customerProfile);
            successfulRecords ++;
        }
        catch (Exception ex)
        {
            failedRecords ++;

            // Log the error and continue processing other records
            Logger.LogError(ex, "Error processing record from topic: {topic}, partition: {partition}, offset: {offset}",
                record.Topic, record.Partition, record.Offset);

            SendToDeadLetterQueue(record, ex); // Optional: Send to a dead-letter queue for further analysis
        }

        Logger.LogInformation("Record Value: {@record}", record.Value);
    }

    return $"Processed {successfulRecords} records successfully, {failedRecords} records failed";
}

void ProcessOrder(CustomerProfile customerProfile)
{
    Logger.LogInformation("Processing order for: {fullName}", customerProfile.FullName);
    // Your business logic to process the order
    // This could throw exceptions for various reasons (e.g., validation errors, database issues)
}

void SendToDeadLetterQueue(ConsumerRecord<string, CustomerProfile> record, Exception ex)
{
    // Implement your dead-letter queue logic here
    Logger.LogError("Sending record to dead-letter queue: {record}, error: {error}", record, ex.Message);
}

await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<string, CustomerProfile>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaProtobufSerializer()) // Use PowertoolsKafkaProtobufSerializer for Protobuf serialization
    .Build()
    .RunAsync();
// --8<-- [end:error_handling]
} }
}

namespace Docs.KafkaSnippets.CrossLanguageCompatibility
{
// --8<-- [start:cross_language_compatibility]
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
} } // --8<-- [start:hidden]

// Example class that handles Python snake_case field names
public partial class CustomerProfile
{
    [JsonPropertyName("user_id")] public string UserId { get; set; }

    [JsonPropertyName("full_name")] public string FullName { get; set; }

    [JsonPropertyName("age")] public long Age { get; set; }

    [JsonPropertyName("account_status")] public string AccountStatus { get; set; }
}
// --8<-- [end:cross_language_compatibility]
}
