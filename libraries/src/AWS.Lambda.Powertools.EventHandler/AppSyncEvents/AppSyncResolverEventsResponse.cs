namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

public class AppSyncResolverEventsResponse
{
    /// <summary>
    /// Collection of event results
    /// </summary>
    public List<AppSyncResolverEventsResult> Events { get; set; } = new();

    public bool Authorized { get; set; }
}