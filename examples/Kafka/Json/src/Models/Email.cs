using System.Text.Json.Serialization;

namespace Json.Models;

public partial class Email
{
    [JsonPropertyName("address")] public string Address { get; set; }

    [JsonPropertyName("verified")] public bool Verified { get; set; }

    [JsonPropertyName("primary")] public bool Primary { get; set; }
}