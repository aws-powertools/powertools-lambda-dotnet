using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Channel details including path and segments
/// </summary>
public class Channel
{
    /// <summary>
    /// Provides direct access to the 'Path' attribute within the 'Channel' object.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }
    
    /// <summary>
    /// Provides direct access to the 'Segments' attribute within the 'Channel' object.
    /// </summary>
    [JsonPropertyName("segments")]
    public string[]? Segments { get; set; }
}