namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents an event from AWS AppSync.
/// </summary>
public class AppSyncEvent
{
    /// <summary>
    /// Payload data when operation succeeds
    /// </summary>
    public Dictionary<string, object>? Payload { get; set; }
        
    /// <summary>
    /// Error message when operation fails
    /// </summary>
    public string? Error { get; set; }
        
    /// <summary>
    /// Unique identifier for the event
    /// This Id is provided by AppSync and needs to be preserved.
    /// </summary>
    public required string Id { get; set; }
}