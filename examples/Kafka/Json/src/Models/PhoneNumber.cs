using System.Text.Json.Serialization;

namespace Json.Models;

public partial class PhoneNumber
{
    [JsonPropertyName("number")] public string Number { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }
}