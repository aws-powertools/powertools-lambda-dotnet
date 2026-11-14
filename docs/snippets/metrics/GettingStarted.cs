using AWS.Lambda.Powertools.Metrics;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.GettingStarted.metrics_attribute
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
public class Function
{
// --8<-- [start:metrics_attribute]
[Metrics(Namespace = "ExampleApplication", Service = "Booking")]
public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
{
    // ...
    return null;
}
// --8<-- [end:metrics_attribute]
}
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.GettingStarted.metrics_methods
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
public class Function
{
// --8<-- [start:metrics_methods]

public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
{
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    Metrics.AddDimension("Environment", "Prod");
    Metrics.AddMetadata("BookingId", "683EEB2D-B2F3-4075-96EE-788E6E2EED45");
    // ...
    return null;
}
// --8<-- [end:metrics_methods]
}
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.GettingStarted.metrics_configure
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
public class Function
{
// --8<-- [start:metrics_configure]

public Function()
{
    Metrics.Configure(options =>
    {
        options.Namespace = "dotnet-powertools-test";
        options.Service = "testService";
        options.CaptureColdStart = true;
        options.DefaultDimensions = new Dictionary<string, string>
        {
            { "Environment", "Prod" },
            { "Another", "One" }
        };
    });
}

[Metrics]
public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
{
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    // ...
    return null;
}
// --8<-- [end:metrics_configure]
}
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.GettingStarted.metrics_builder
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
public class Function
{
// --8<-- [start:metrics_builder]

private readonly IMetrics _metrics;

public Function()
{
    _metrics = new MetricsBuilder()
        .WithCaptureColdStart(true)
        .WithService("testService")
        .WithNamespace("dotnet-powertools-test")
        .WithDefaultDimensions(new Dictionary<string, string>
        {
            { "Environment", "Prod1" },
            { "Another", "One" }
        }).Build();
}

[Metrics]
public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
{
    _metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    // ...
    return null;
}
// --8<-- [end:metrics_builder]
}
}
