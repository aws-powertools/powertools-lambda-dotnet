using AWS.Lambda.Powertools.Metrics;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace Docs.MetricsSnippets.AddingMultipleDimensions.adding_multiple_dimensions
{
// --8<-- [start:adding_multiple_dimensions]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddDimensions(
        ("Environment", "Prod"),
        ("Region", "eu-west-1")
    );
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    // ...
    return null;
  }
}
// --8<-- [end:adding_multiple_dimensions]
}
