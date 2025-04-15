namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents an AWS AppSync authorization event that is sent to a Lambda authorizer
/// for evaluating access permissions to the GraphQL API.
/// </summary>
public class AppSyncAuthorizerEvent
{
    /// <summary>
    /// Gets or sets the authorization token received from the client request.
    /// This token is used to make authorization decisions.
    /// </summary>
    public string AuthorizationToken { get; set; }

    /// <summary>
    /// Gets or sets the headers from the client request.
    /// Contains key-value pairs of HTTP header names and their values.
    /// </summary>
    public Dictionary<string, string> RequestHeaders { get; set; }

    /// <summary>
    /// Gets or sets the context information about the AppSync request.
    /// Contains metadata about the API and the GraphQL operation being executed.
    /// </summary>
    public AppSyncRequestContext RequestContext { get; set; }
}