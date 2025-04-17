namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Channel details including path and segments
/// </summary>
public class Channel
{
    /// <summary>
    /// Provides direct access to the 'Path' attribute within the 'Channel' object.
    /// </summary>
    public string? Path { get; set; }
    
    /// <summary>
    /// Provides direct access to the 'Segments' attribute within the 'Channel' object.
    /// </summary>
    public string[]? Segments { get; set; }
}