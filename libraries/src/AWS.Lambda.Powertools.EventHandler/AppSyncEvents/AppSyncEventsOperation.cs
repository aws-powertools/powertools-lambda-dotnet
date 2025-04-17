using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents the operation type for AppSync events.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppSyncEventsOperation
{
    /// <summary>
    /// Represents a subscription operation.
    /// </summary>
    Subscribe,

    /// <summary>
    /// Represents a publish operation.
    /// </summary>
    Publish
}