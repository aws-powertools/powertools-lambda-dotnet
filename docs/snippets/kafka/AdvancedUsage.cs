// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Kafka;

// --8<-- [start:record_metadata]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;

string Handler(ConsumerRecords<string, CustomerProfile> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        // Log record coordinates for tracing
        Logger.LogInformation("Processing messagem from topic: {topic}", record.Topic);
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
}

await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<string, CustomerProfile>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaProtobufSerializer()) // Use PowertoolsKafkaProtobufSerializer for Protobuf serialization
    .Build()
    .RunAsync();
// --8<-- [end:record_metadata]

// --8<-- [start:error_handling]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;

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

private void ProcessOrder(CustomerProfile customerProfile)
{
    Logger.LogInformation("Processing order for: {fullName}", customerProfile.FullName);
    // Your business logic to process the order
    // This could throw exceptions for various reasons (e.g., validation errors, database issues)
}

private void SendToDeadLetterQueue(ConsumerRecord<string, CustomerProfile> record, Exception ex)
{
    // Implement your dead-letter queue logic here
    Logger.LogError("Sending record to dead-letter queue: {record}, error: {error}", record, ex.Message);
}

await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<string, CustomerProfile>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaProtobufSerializer()) // Use PowertoolsKafkaProtobufSerializer for Protobuf serialization
    .Build()
    .RunAsync();
// --8<-- [end:error_handling]

// --8<-- [start:idempotent_processing]
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Idempotency;

[assembly: LambdaSerializer(typeof(PowertoolsKafkaProtobufSerializer))]

namespace ProtoBufClassLibrary;

public class Function
{
    public Function()
    {
        Idempotency.Configure(builder => builder.UseDynamoDb("idempotency_table"));
    }

    public string FunctionHandler(ConsumerRecords<string, Payment> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            ProcessPayment(record.Key, record.Value);
        }

        return "Processed " + records.Count() + " records";
    }

    [Idempotent]
    private void ProcessPayment(Payment payment)
    {
        Logger.LogInformation("Processing payment {paymentId} for customer {customerName}",
            payment.Id, payment.CustomerName);

        // Your payment processing logic here
        // This could involve calling an external payment service, updating a database, etc.
    }
}
// --8<-- [end:idempotent_processing]

// --8<-- [start:cross_language_compatibility]
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

// Example class that handles Python snake_case field names
public partial class CustomerProfile
{
    [JsonPropertyName("user_id")] public string UserId { get; set; }

    [JsonPropertyName("full_name")] public string FullName { get; set; }

    [JsonPropertyName("age")] public long Age { get; set; }

    [JsonPropertyName("account_status")] public string AccountStatus { get; set; }
}
// --8<-- [end:cross_language_compatibility]
