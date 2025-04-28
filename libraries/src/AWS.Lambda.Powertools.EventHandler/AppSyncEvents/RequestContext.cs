namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents information about the HTTP request that triggered the event.
/// </summary>
public class RequestContext
{
    /// <summary>
    /// Gets or sets the headers of the HTTP request.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the domain name associated with the request.
    /// </summary>
    public string? DomainName { get; set; }
}