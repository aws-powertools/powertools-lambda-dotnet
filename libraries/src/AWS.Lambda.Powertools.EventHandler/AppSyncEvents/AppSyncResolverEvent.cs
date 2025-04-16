namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents the event payload received from AWS AppSync.
/// </summary>
public class AppSyncResolverEvent
{
    // /// <summary>
    // /// Gets or sets the input arguments for the GraphQL operation.
    // /// </summary>
    // public TArguments Arguments { get; set; }

    /// <summary>
    /// An object that contains information about the caller.
    /// Returns null for API_KEY authorization.
    /// Returns AppSyncIamIdentity for AWS_IAM authorization.
    /// Returns AppSyncCognitoIdentity for AMAZON_COGNITO_USER_POOLS authorization.
    /// For AWS_LAMBDA authorization, returns the object returned by your Lambda authorizer function.
    /// </summary>
    /// <remarks>
    /// The Identity object type depends on the authorization mode:
    /// - For API_KEY: null
    /// - For AWS_IAM: <see cref="AppSyncIamIdentity"/>
    /// - For AMAZON_COGNITO_USER_POOLS: <see cref="AppSyncCognitoIdentity"/>
    /// - For AWS_LAMBDA: <see cref="AppSyncLambdaIdentity"/>
    /// - For OPENID_CONNECT: <see cref="AppSyncOidcIdentity"/>
    /// </remarks>
    public object? Identity { get; set; }

    /// <summary>
    /// Gets or sets information about the data source that originated the event.
    /// </summary>
    public object? Source { get; set; }

    /// <summary>
    /// Gets or sets information about the HTTP request that triggered the event.
    /// </summary>
    public RequestContext Request { get; set; } = new();

    /// <summary>
    /// Gets or sets information about the previous state of the data before the operation was executed.
    /// </summary>
    public object Prev { get; set; }

    /// <summary>
    /// Gets or sets information about the GraphQL operation being executed.
    /// </summary>
    public Information Info { get; set; }

    /// <summary>
    /// Gets or sets additional information that can be passed between Lambda functions during an AppSync pipeline.
    /// </summary>
    public Dictionary<string, object> Stash { get; set; }
    
    public string Error { get; set; }
    
    public object[] OutErrors { get; set; }
    
    public Event[] Events { get; set; }
}