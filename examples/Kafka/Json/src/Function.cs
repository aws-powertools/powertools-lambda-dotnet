using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Json;
using AWS.Lambda.Powertools.Logging;

string Handler(ConsumerRecords<JsonKey, Payload> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        Logger.LogInformation("Record Value: {@record}", record.Value);
    }
    
    return "Processed " + records.Count() + " records";
}


await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<JsonKey, Payload>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaJsonSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
    .Build()
    .RunAsync();


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