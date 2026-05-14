// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics;

// --8<-- [start:flushing_metrics]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    Metrics.Flush();
  }
}
// --8<-- [end:flushing_metrics]

// --8<-- [start:raise_on_empty_metrics]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(RaiseOnEmptyMetrics = true)]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    ...
// --8<-- [end:raise_on_empty_metrics]
