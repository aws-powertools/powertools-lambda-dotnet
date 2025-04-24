using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents an event from AWS AppSync.
/// </summary>
public class AppSyncEvent
{
    /// <summary>
    /// Payload data when operation succeeds
    /// </summary>
    [JsonPropertyName("payload")]
    public Dictionary<string, object>? Payload { get; set; }
        
    /// <summary>
    /// Error message when operation fails
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("error")]
    public string? Error { get; set; }
        
    /// <summary>
    /// Unique identifier for the event
    /// This Id is provided by AppSync and needs to be preserved.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}