using System.Text.Json.Serialization;

namespace Json.Models;

public partial class CustomerProfile
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