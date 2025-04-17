using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents information about the AppSync event.
/// </summary>
public class Information
{
    /// <summary>
    ///  The channel being used for the operation
    /// </summary>
    public Channel Channel { get; set; }
    
    /// <summary>
    ///  The namespace of the channel
    /// </summary>
    public ChannelNamespace ChannelNamespace { get; set; }
    
    /// <summary>
    ///  The operation being performed (e.g., Publish, Subscribe)
    /// </summary>
    public AppSyncEventsOperation Operation { get; set; }
}