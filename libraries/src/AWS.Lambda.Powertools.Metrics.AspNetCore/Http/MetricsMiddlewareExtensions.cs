using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

/// <summary>
/// Provides extension methods for adding metrics middleware to the application pipeline.
/// </summary>
public static class MetricsMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware to capture and record metrics for HTTP requests, including cold start tracking.
    /// </summary>
    /// <param name="app">The application builder instance used to configure the request pipeline.</param>
    /// <returns>The application builder with the metrics middleware added.</returns>
    /// <remarks>
    /// This middleware tracks cold starts and captures request metrics. To use this middleware, ensure you have registered
    /// the required services using <code>builder.Services.AddSingleton&lt;IMetrics&gt;()</code> in your service configuration.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.UseMetrics();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var metrics = context.RequestServices.GetRequiredService<IMetrics>();
            using var metricsHelper = new ColdStartTracker(metrics);
            metricsHelper.TrackColdStart(context);
            await next();
        });
    }
}
