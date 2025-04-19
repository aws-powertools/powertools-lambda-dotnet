using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Represents the event payload received from AWS AppSync.
/// </summary>
public class AppSyncEventsRequest
{
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
    [JsonPropertyName("source")]
    public object? Source { get; set; }

    /// <summary>
    /// Gets or sets information about the HTTP request that triggered the event.
    /// </summary>
    [JsonPropertyName("request")]
    public RequestContext? Request { get; set; }

    /// <summary>
    /// Gets or sets information about the previous state of the data before the operation was executed.
    /// </summary>
    [JsonPropertyName("prev")]
    public object? Prev { get; set; }

    /// <summary>
    /// Gets or sets information about the GraphQL operation being executed.
    /// </summary>
    [JsonPropertyName("info")]
    public Information? Info { get; set; }

    /// <summary>
    /// Gets or sets additional information that can be passed between Lambda functions during an AppSync pipeline.
    /// </summary>
    [JsonPropertyName("stash")]
    public Dictionary<string, object>? Stash { get; set; }
    
    /// <summary>
    /// The error message when the operation fails.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    /// <summary>
    /// The list of error message when the operation fails.
    /// </summary>
    public object[]? OutErrors { get; set; }
    
    /// <summary>
    /// The list of events sent.
    /// </summary>
    [JsonPropertyName("events")]
    public AppSyncEvent[]? Events { get; set; }
}