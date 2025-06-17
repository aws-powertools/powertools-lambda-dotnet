using System.Diagnostics;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using Com.Example;
using TestKafka;

// string Handler(ConsumerRecords<ProtobufKey, ProtobufProduct> records, ILambdaContext context)
// {
//     Metrics.SetNamespace("Proto");
//     Metrics.AddMetric("NumberOfRequests", 1, MetricUnit.Count, MetricResolution.High);
//
//     foreach (var record in records)
//     {
//         foreach (var header in record.Headers)
//         {
//             Console.WriteLine($"{header.Key}: {ToDecimalString(header.Value)}");
//         }
//
//         foreach (var header in record.Headers.DecodedValues())
//         {
//             Console.WriteLine($"{header.Key}: {header.Value}");
//         }
//         
//         Logger.LogInformation("Record Key: {@key}", record.Key);
//         Logger.LogInformation("Record Value: {@record}", record.Value);
//     }
//
//     return "Processed " + records.Count() + " records";
// }
//
// await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<ProtobufKey, ProtobufProduct>, ILambdaContext, string>?)Handler,
//         new PowertoolsKafkaProtobufSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
//     .Build()
//     .RunAsync();

var responseStream = new MemoryStream();
var serializer = new PowertoolsKafkaProtobufSerializer();

Task<InvocationResponse> ToUpperAsync(InvocationRequest invocation)
{
    var stopwatch = Stopwatch.StartNew();

    var records = serializer.Deserialize<ConsumerRecords<string, CustomerProfile>>(invocation.InputStream);

    foreach (var record in records)
    {
        foreach (var header in record.Headers)
        {
            Console.WriteLine($"{header.Key}: {ToDecimalString(header.Value)}");
        }
        
        foreach (var header in record.Headers.DecodedValues())
        {
            Console.WriteLine($"{header.Key}: {header.Value}");
        }
        
        Console.WriteLine("Record UserId: {0}", record.Value);
    }

    stopwatch.Stop();

    Metrics.PushSingleMetric("ProtoDeserialization-512",
        stopwatch.ElapsedMilliseconds, MetricUnit.Milliseconds, "kafka-dotnet", "service", null,
        MetricResolution.High);

    Console.WriteLine("Record Count: {0}", records.Count());
    Console.WriteLine("JsonDeserialization: {0:F2}", stopwatch.ElapsedMilliseconds);

    responseStream.SetLength(0);
    responseStream.Position = 0;

    return Task.FromResult(new InvocationResponse(responseStream, false));
}

static string ToDecimalString(byte[] bytes)
{
    if (bytes == null || bytes.Length == 0)
    {
        return "[]";
    }
            
    return "[" + string.Join(", ", bytes) + "]";
}

var bootstrap = new LambdaBootstrap(ToUpperAsync);
await bootstrap.RunAsync();