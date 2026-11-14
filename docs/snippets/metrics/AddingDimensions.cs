using AWS.Lambda.Powertools.Metrics;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics.AddingDimensions.adding_dimensions
{
using Metrics = global::AWS.Lambda.Powertools.Metrics.Metrics;
// --8<-- [start:adding_dimensions]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddDimension("Environment","Prod");
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    // ...
    return null;
  }
}
// --8<-- [end:adding_dimensions]
}
