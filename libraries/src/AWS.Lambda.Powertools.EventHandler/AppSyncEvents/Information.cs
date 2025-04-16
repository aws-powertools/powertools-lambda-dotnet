namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents information about the AppSync event.
/// </summary>
public class Information
{
    /// <summary>
    ///  The channel being used for the operation
    /// </summary>
    public required Channel Channel { get; set; }
    
    /// <summary>
    ///  The namespace of the channel
    /// </summary>
    public required ChannelNamespace ChannelNamespace { get; set; }
    
    /// <summary>
    ///  The operation being performed (e.g., Publish, Subscribe)
    /// </summary>
    public required AppSyncEventsOperation Operation { get; set; }
}