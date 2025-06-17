using System.Diagnostics;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Avro;
using AWS.Lambda.Powertools.Kafka.Tests;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using com.example;

// string Handler(ConsumerRecords<AvroKey, AvroProduct> records, ILambdaContext context)
// {
//     Metrics.SetNamespace("Avro");
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
// await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<AvroKey, AvroProduct>, ILambdaContext, string>?)Handler,
//         new PowertoolsKafkaAvroSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
//     .Build()
//     .RunAsync();

var responseStream = new MemoryStream();
var serializer = new PowertoolsKafkaAvroSerializer();
Task<InvocationResponse> ToUpperAsync(InvocationRequest invocation)
{
    var stopwatch = Stopwatch.StartNew();

    var records = serializer.Deserialize<ConsumerRecords<string, CustomerProfile>>(invocation.InputStream);
    
    foreach (var record in records)
    {
        Console.WriteLine("Record UserId: {0}", record.Value.user_id);
    }

    stopwatch.Stop();

    Metrics.PushSingleMetric("AvroDeserialization-1024",
        stopwatch.ElapsedMilliseconds, MetricUnit.Milliseconds, "kafka-dotnet", "service", null,
        MetricResolution.High);

    Console.WriteLine("Record Count: {0}", records.Count());
    Console.WriteLine("Record UserId: {0}", records.First().Value.user_id);
    Console.WriteLine("JsonDeserialization: {0:F2}", stopwatch.ElapsedMilliseconds);

    responseStream.SetLength(0);
    responseStream.Position = 0;

    return Task.FromResult(new InvocationResponse(responseStream, false));
}

var bootstrap = new LambdaBootstrap(ToUpperAsync);
await bootstrap.RunAsync();