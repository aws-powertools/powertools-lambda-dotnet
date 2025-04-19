using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Namespace configuration for the channel
/// </summary>
public class ChannelNamespace
{
    /// <summary>
    /// Name of the channel namespace
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}