namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents information about the GraphQL operation being executed.
/// </summary>
public class Information
{
    /// <summary>
    /// Gets or sets the name of the GraphQL field being executed.
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// Gets or sets a list of fields being selected in the GraphQL operation.
    /// </summary>
    public List<string> SelectionSetList { get; set; }

    /// <summary>
    /// Gets or sets the GraphQL selection set for the operation.
    /// </summary>
    public string SelectionSetGraphQL { get; set; }

    /// <summary>
    /// Gets or sets the variables passed to the GraphQL operation.
    /// </summary>
    public Dictionary<string, object> Variables { get; set; }

    /// <summary>
    /// Gets or sets the parent type name for the GraphQL operation.
    /// </summary>
    public string ParentTypeName { get; set; }
    
    public Channel Channel { get; set; }
    public ChannelNamespace ChannelNamespace { get; set; }
    
    /// <summary>
    ///  The operation being performed (e.g., Publish, Subscribe)
    /// </summary>
    public AppsyncEventsOperation Operation { get; set; }
}

public enum AppsyncEventsOperation
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

public class Event
{
    public Dictionary<string,object> Payload { get; set; }
    
    public string Id { get; set; }
}

