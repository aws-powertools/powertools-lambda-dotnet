using AWS.Lambda.Powertools.Metrics;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.CaptureColdStart.capture_cold_start_attribute
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
// --8<-- [start:capture_cold_start_attribute]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(CaptureColdStart = true)]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    // ...
// --8<-- [end:capture_cold_start_attribute]
    return null;
}
}

}

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.CaptureColdStart.capture_cold_start_builder
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
// --8<-- [start:capture_cold_start_builder]
using AWS.Lambda.Powertools.Metrics;

public class Function {
  private readonly IMetrics _metrics;

  public Function()
  {
    _metrics = new MetricsBuilder()
        .WithCaptureColdStart(true)
        .WithService("testService")
        .WithNamespace("dotnet-powertools-test")
        .Build();
  }

  [Metrics]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    _metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    // ...
    return null;
}
// --8<-- [end:capture_cold_start_builder]
}

}

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.CaptureColdStart.capture_cold_start_configure
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
// --8<-- [start:capture_cold_start_configure]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  public Function()
  {
    Metrics.Configure(options =>
    {
        options.Namespace = "dotnet-powertools-test";
        options.Service = "testService";
        options.CaptureColdStart = true;
    });
  }

  [Metrics]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    // ...
    return null;
}
// --8<-- [end:capture_cold_start_configure]
}

}
