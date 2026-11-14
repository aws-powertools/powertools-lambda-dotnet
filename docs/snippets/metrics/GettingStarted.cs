using AWS.Lambda.Powertools.Metrics;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace Docs.MetricsSnippets.GettingStarted.metrics_attribute
{
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

namespace Docs.MetricsSnippets.GettingStarted.metrics_methods
{
// --8<-- [start:metrics_methods]
using AWS.Lambda.Powertools.Metrics;

public class Function { // --8<-- [start:hidden]
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

namespace Docs.MetricsSnippets.GettingStarted.metrics_configure
{
// --8<-- [start:metrics_configure]
using AWS.Lambda.Powertools.Metrics;

public class Function { // --8<-- [start:hidden]
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

namespace Docs.MetricsSnippets.GettingStarted.metrics_builder
{
// --8<-- [start:metrics_builder]
using AWS.Lambda.Powertools.Metrics;

public class Function { // --8<-- [start:hidden]
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
