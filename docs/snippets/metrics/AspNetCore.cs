using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using AWS.Lambda.Powertools.Metrics;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).



namespace Docs.MetricsSnippets.AspNetCore.use_metrics_middleware
{
// --8<-- [start:use_metrics_middleware]

using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

public static class Example { public static void Main(string[] args) { // --8<-- [start:hidden]
var builder = WebApplication.CreateBuilder(args);

// Configure metrics
builder.Services.AddSingleton<IMetrics>(_ => new MetricsBuilder()
    .WithNamespace("MyApi") // Namespace for the metrics
    .WithService("WeatherService") // Service name for the metrics
    .WithCaptureColdStart(true) // Capture cold start metrics
    .WithDefaultDimensions(new Dictionary<string, string> // Default dimensions for the metrics
    {
        {"Environment", "Prod"},
        {"Another", "One"}
    })
    .Build()); // Build the metrics

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

app.UseMetrics(); // Add the metrics middleware

app.MapGet("/powertools", (IMetrics metrics) =>
    {
        // add custom metrics
        metrics.AddMetric("MyCustomMetric", 1, MetricUnit.Count);
        // flush metrics - this is required to ensure metrics are sent to CloudWatch
        metrics.Flush();
    });

app.Run();
// --8<-- [end:use_metrics_middleware]
} }
}

namespace Docs.MetricsSnippets.AspNetCore.with_metrics_filter
{
// --8<-- [start:with_metrics_filter]

using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

public static class Example { public static void Main(string[] args) { // --8<-- [start:hidden]
var builder = WebApplication.CreateBuilder(args);

// Configure metrics
builder.Services.AddSingleton<IMetrics>(_ => new MetricsBuilder()
    .WithNamespace("MyApi") // Namespace for the metrics
    .WithService("WeatherService") // Service name for the metrics
    .WithCaptureColdStart(true) // Capture cold start metrics
    .WithDefaultDimensions(new Dictionary<string, string> // Default dimensions for the metrics
    {
        {"Environment", "Prod"},
        {"Another", "One"}
    })
    .Build()); // Build the metrics

// Add AWS Lambda support. When the application is run in Lambda, Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the web server translating requests and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

app.MapGet("/powertools", (IMetrics metrics) =>
    {
        // add custom metrics
        metrics.AddMetric("MyCustomMetric", 1, MetricUnit.Count);
        // flush metrics - this is required to ensure metrics are sent to CloudWatch
        metrics.Flush();
    })
    .WithMetrics();

app.Run();
// --8<-- [end:with_metrics_filter]
} }
}

namespace Docs.MetricsSnippets.AspNetCore.with_metrics_method
{
public static class Example
{
    // --8<-- [start:with_metrics_method]
/// <summary>
/// Adds a metrics filter to the specified route handler builder.
/// This will capture cold start (if CaptureColdStart is enabled) metrics and flush metrics on function exit.
/// </summary>
/// <param name="builder">The route handler builder to add the metrics filter to.</param>
/// <returns>The route handler builder with the metrics filter added.</returns>
public static RouteHandlerBuilder WithMetrics(this RouteHandlerBuilder builder)
{
    builder.AddEndpointFilter<MetricsFilter>();
    return builder;
}
// --8<-- [end:with_metrics_method]
}
}
