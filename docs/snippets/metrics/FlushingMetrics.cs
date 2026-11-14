using AWS.Lambda.Powertools.Metrics;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.FlushingMetrics.flushing_metrics
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
// --8<-- [start:flushing_metrics]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    Metrics.Flush();
    return null;
  }
}
// --8<-- [end:flushing_metrics]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.FlushingMetrics.raise_on_empty_metrics
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
// --8<-- [start:raise_on_empty_metrics]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(RaiseOnEmptyMetrics = true)]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    // ...
// --8<-- [end:raise_on_empty_metrics]
    return null;
}
}

}
