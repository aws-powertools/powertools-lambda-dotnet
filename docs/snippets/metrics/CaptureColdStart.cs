// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics;

// --8<-- [start:capture_cold_start_attribute]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(CaptureColdStart = true)]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    ...
// --8<-- [end:capture_cold_start_attribute]

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
  }

  [Metrics]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    _metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    ...
}
// --8<-- [end:capture_cold_start_builder]

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
    ...
}
// --8<-- [end:capture_cold_start_configure]
