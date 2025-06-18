using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Avro;
using AWS.Lambda.Powertools.Kafka.Tests;
using AWS.Lambda.Powertools.Logging;

string Handler(ConsumerRecords<AvroKey, AvroProduct> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        Logger.LogInformation("Record Value: {@record}", record.Value);
    }
    
    return "Processed " + records.Count() + " records";
}


await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<AvroKey, AvroProduct>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaAvroSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
    .Build()
    .RunAsync();