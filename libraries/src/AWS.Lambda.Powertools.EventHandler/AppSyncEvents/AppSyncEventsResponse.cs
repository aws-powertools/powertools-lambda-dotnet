using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents the response for AppSync events.
/// </summary>
public class AppSyncEventsResponse
{
    /// <summary>
    /// Collection of event results
    /// </summary>
    [JsonPropertyName("events")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<AppSyncEvent>? Events { get; set; }
    
    /// <summary>
    /// When operation fails, this will contain the error message
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Error { get; set; }
}