using System.Text.Json.Serialization;

namespace Json.Models;

public partial class Preferences
{
    [JsonPropertyName("language")] public string Language { get; set; }

    [JsonPropertyName("notifications")] public string Notifications { get; set; }

    [JsonPropertyName("timezone")] public string Timezone { get; set; }
}