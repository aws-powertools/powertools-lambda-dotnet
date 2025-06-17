using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Json;
using AWS.Lambda.Powertools.Kafka.Avro;
using AWS.Lambda.Powertools.Kafka.Tests;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using TestKafka;

string Handler(ConsumerRecords<AvroKey, AvroProduct> records, ILambdaContext context)
{
    Metrics.SetNamespace("Avro");
    Metrics.AddMetric("NumberOfRequests", 1, MetricUnit.Count, MetricResolution.High);
    
    foreach (var record in records)
    {
        Logger.LogInformation("Record Key: {@key}", record.Key);
        Logger.LogInformation("Record Value: {@record}", record.Value);
    }
    
    return "Processed " + records.Count() + " records";
}


await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<AvroKey, AvroProduct>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaAvroSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
    .Build()
    .RunAsync();


//
// string Handler(ConsumerRecords<ProtobufKey, ProtobufProduct> records, ILambdaContext context)
// {
//     Metrics.SetNamespace("Proto");
//     Metrics.AddMetric("NumberOfRequests", 1, MetricUnit.Count, MetricResolution.High);
//     
//     foreach (var record in records)
//     {
//         Logger.LogInformation("Record Key: {@key}", record.Key);
//         Logger.LogInformation("Record Value: {@record}", record.Value);
//     }
//     
//     return "Processed " + records.Count() + " records";
// }
//
//
// await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<ProtobufKey, ProtobufProduct>, ILambdaContext, string>?)Handler,
//         new PowertoolsKafkaProtobufSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
//     .Build()
//     .RunAsync();

//
// string Handler(ConsumerRecords<JsonKey, JsonProduct> records, ILambdaContext context)
// {
//     Metrics.SetNamespace("Json");
//     Metrics.AddMetric("NumberOfRequests", 1, MetricUnit.Count, MetricResolution.High);
//     
//     foreach (var record in records)
//     {
//         Logger.LogInformation("Record Key: {@record.Key}", record.Key);
//         Logger.LogInformation("Record Value: {@record}", record.Value);
//     }
//     
//     return "Processed " + records.Count() + " records";
// }
//
//
// await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<JsonKey, JsonProduct>, ILambdaContext, string>?)Handler,
//         new PowertoolsKafkaJsonSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
//     .Build()
//     .RunAsync();
//     
//     
// public record JsonKey
// {
//     public int Id { get; set; }
// }
//     
// public record JsonProduct
// {
//     public int Id { get; set; }
//     public string Name { get; set; } = string.Empty;
//     public decimal Price { get; set; }
// }