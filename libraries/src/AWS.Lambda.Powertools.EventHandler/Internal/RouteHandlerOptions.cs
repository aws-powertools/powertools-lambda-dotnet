using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.EventHandler.Internal;

/// <summary>
/// Options for registering a route handler
/// </summary>
internal class RouteHandlerOptions<TEvent, TResult>
{
    /// <summary>
    /// The path pattern to match against (e.g., "/default/*")
    /// </summary>
    public string Path { get; set; } = "/default/*";

    /// <summary>
    /// The handler function to execute when path matches
    /// </summary>
    public Func<TEvent, ILambdaContext, Task<TResult>> Handler { get; set; }

    /// <summary>
    /// Whether to aggregate all events into a single handler call
    /// </summary>
    public bool Aggregate { get; set; } = false;
}