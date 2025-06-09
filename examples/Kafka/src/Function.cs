using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Tests;

// The function handler that will be called for each Lambda event
string Handler(ConsumerRecords<AvroKey, AvroProduct> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        var key = record.Key.id;
        var product = record.Value;
        
        context.Logger.LogInformation($"Processing record with key: {key}, Product: {product.name}, Price: {product.price}");
    }
    
    return "Processed " + records.Count() + " records";
}


await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<AvroKey, AvroProduct>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaAvroSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
    .Build()
    .RunAsync();