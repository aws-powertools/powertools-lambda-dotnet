// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics;

// --8<-- [start:metrics_attribute]
[Metrics(Namespace = "ExampleApplication", Service = "Booking")]
public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
{
    ...
}
// --8<-- [end:metrics_attribute]

// --8<-- [start:metrics_methods]
using AWS.Lambda.Powertools.Metrics;

public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
{
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    Metrics.AddDimension("Environment", "Prod");
    Metrics.AddMetadata("BookingId", "683EEB2D-B2F3-4075-96EE-788E6E2EED45");
    ...
}
// --8<-- [end:metrics_methods]

// --8<-- [start:metrics_configure]
using AWS.Lambda.Powertools.Metrics;

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
    ...
}
// --8<-- [end:metrics_configure]

// --8<-- [start:metrics_builder]
using AWS.Lambda.Powertools.Metrics;

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
    ...
}
// --8<-- [end:metrics_builder]
