namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

public class AppSyncResolverEventsResult
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
    /// </summary>
    public string Id { get; set; } = string.Empty;
}