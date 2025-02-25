# AWS Lambda Powertools Metrics for ASP.NET Core

This library provides utilities for capturing and publishing custom metrics from your AWS Lambda functions using ASP.NET Core.

## Getting Started

This library provides utilities for capturing and publishing custom metrics from your AWS Lambda functions using ASP.NET Core.

### Installation

You can install the package via the NuGet package manager just search for `AWS.Lambda.Powertools.Metrics.AspNetCore`. 

You can also install via powershell using the following command.

```shell
dotnet add package AWS.Lambda.Powertools.Metrics.AspNetCore
```

```csharp

using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

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

```

### WithMetrics() filter

The `WithMetrics` method is an extension method for the `RouteHandlerBuilder` class. 

It adds a metrics filter to the specified route handler builder, which captures cold start metrics (if enabled) and flushes metrics on function exit.

Here is the highlighted `WithMetrics` method:

```csharp
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
```

Explanation:
- The method is defined as an extension method for the `RouteHandlerBuilder` class.
- It adds a `MetricsFilter` to the route handler builder using the `AddEndpointFilter` method.
- The `MetricsFilter` captures and records metrics for HTTP endpoints, including cold start metrics if the `CaptureColdStart` option is enabled.
- The method returns the modified `RouteHandlerBuilder` instance with the metrics filter added.


### UseMetrics() Middleware

The `UseMetrics` middleware is an extension method for the `IApplicationBuilder` interface.

It adds a metrics middleware to the specified application builder, which captures cold start metrics (if enabled) and flushes metrics on function exit.

#### Example

```csharp
    
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

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

```

Here is the highlighted `UseMetrics` method:

```csharp
/// <summary>
/// Adds a metrics middleware to the specified application builder.
/// This will capture cold start (if CaptureColdStart is enabled) metrics and flush metrics on function exit.
/// </summary>
/// <param name="app">The application builder to add the metrics middleware to.</param>
/// <returns>The application builder with the metrics middleware added.</returns>
public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
{
    app.UseMiddleware<MetricsMiddleware>();
    return app;
}
```

Explanation:
- The method is defined as an extension method for the `IApplicationBuilder` interface.
- It adds a `MetricsMiddleware` to the application builder using the `UseMiddleware` method.
- The `MetricsMiddleware` captures and records metrics for HTTP requests, including cold start metrics if the `CaptureColdStart` option is enabled.