using System.Text.Json.Serialization;

namespace Json.Models;

public partial class Address
{
    [JsonPropertyName("street")] public string Street { get; set; }

    [JsonPropertyName("city")] public string City { get; set; }

    [JsonPropertyName("state")] public string State { get; set; }

    [JsonPropertyName("country")] public string Country { get; set; }

    [JsonPropertyName("zip_code")] public string ZipCode { get; set; }
}