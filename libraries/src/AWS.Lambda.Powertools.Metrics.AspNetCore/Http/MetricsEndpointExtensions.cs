using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

/// <summary>
/// Provides extension methods for adding metrics to route handlers.
/// </summary>
public static class MetricsEndpointExtensions
{
    /// <summary>
    /// Adds a metrics filter to the specified route handler builder.
    /// This will capture cold start (if CaptureColdStart is enabled) metrics and give each request its own
    /// metrics context. Call Flush to publish the request's metrics.
    /// </summary>
    /// <param name="builder">The route handler builder to add the metrics filter to.</param>
    /// <returns>The route handler builder with the metrics filter added.</returns>
    public static RouteHandlerBuilder WithMetrics(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter<MetricsFilter>();
        return builder;
    }
}
