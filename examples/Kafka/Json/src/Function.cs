using System.Diagnostics;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Json;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;

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

var responseStream = new MemoryStream();
var serializer = new PowertoolsKafkaJsonSerializer();

Task<InvocationResponse> ToUpperAsync(InvocationRequest invocation)
{
    var stopwatch = Stopwatch.StartNew();

    var records = serializer.Deserialize<ConsumerRecords<JsonKey, Payload>>(invocation.InputStream);
    
    foreach (var record in records)
    {
        Console.WriteLine("Record UserId: {0}", record.Value.UserId);
    }
    
    stopwatch.Stop();

    Metrics.PushSingleMetric("JsonDeserialization-1024",
        stopwatch.ElapsedMilliseconds, MetricUnit.Milliseconds, "kafka-dotnet", "service", null,
        MetricResolution.High);

    Console.WriteLine("Record Count: {0}", records.Count());
    Console.WriteLine("Record UserId: {0}", records.First().Value.UserId);
    Console.WriteLine("JsonDeserialization: {0:F2}", stopwatch.ElapsedMilliseconds);

    responseStream.SetLength(0);
    responseStream.Position = 0;

    return Task.FromResult(new InvocationResponse(responseStream, false));
}

var bootstrap = new LambdaBootstrap(ToUpperAsync);
await bootstrap.RunAsync();


public record JsonKey
{
    public int Id { get; set; }
}

public partial class Payload
{
    [JsonPropertyName("user_id")] public string UserId { get; set; }

    [JsonPropertyName("full_name")] public string FullName { get; set; }

    [JsonPropertyName("email")] public Email Email { get; set; }

    [JsonPropertyName("age")] public long Age { get; set; }

    [JsonPropertyName("address")] public Address Address { get; set; }

    [JsonPropertyName("phone_numbers")] public List<PhoneNumber> PhoneNumbers { get; set; }

    [JsonPropertyName("preferences")] public Preferences Preferences { get; set; }

    [JsonPropertyName("account_status")] public string AccountStatus { get; set; }
}

public partial class Address
{
    [JsonPropertyName("street")] public string Street { get; set; }

    [JsonPropertyName("city")] public string City { get; set; }

    [JsonPropertyName("state")] public string State { get; set; }

    [JsonPropertyName("country")] public string Country { get; set; }

    [JsonPropertyName("zip_code")] public string ZipCode { get; set; }
}

public partial class Email
{
    [JsonPropertyName("address")] public string Address { get; set; }

    [JsonPropertyName("verified")] public bool Verified { get; set; }

    [JsonPropertyName("primary")] public bool Primary { get; set; }
}

public partial class PhoneNumber
{
    [JsonPropertyName("number")] public string Number { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }
}

public partial class Preferences
{
    [JsonPropertyName("language")] public string Language { get; set; }

    [JsonPropertyName("notifications")] public string Notifications { get; set; }

    [JsonPropertyName("timezone")] public string Timezone { get; set; }
}