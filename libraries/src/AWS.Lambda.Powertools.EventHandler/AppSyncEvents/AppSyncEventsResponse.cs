namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents the response for AppSync events.
/// </summary>
public class AppSyncEventsResponse
{
    /// <summary>
    /// Collection of event results
    /// </summary>
    public List<AppSyncEvent>? Events { get; set; }
    
    /// <summary>
    /// Used for OnSubscribe to determine if the subscription should be authorized
    /// </summary>
    public bool? Authorized { get; set; }
    
    /// <summary>
    /// When operation fails, this will contain the error message
    /// </summary>
    public string? Error { get; set; }
}