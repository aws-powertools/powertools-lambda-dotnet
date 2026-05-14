// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics;

// --8<-- [start:creating_metrics]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
  }
}
// --8<-- [end:creating_metrics]

// --8<-- [start:metrics_with_dimensions]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddDimension("Environment","Prod");
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
  }
}
// --8<-- [end:metrics_with_dimensions]

// --8<-- [start:high_resolution_metrics]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    // Publish a metric with standard resolution i.e. StorageResolution = 60
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count, MetricResolution.Standard);

    // Publish a metric with high resolution i.e. StorageResolution = 1
    Metrics.AddMetric("FailedBooking", 1, MetricUnit.Count, MetricResolution.High);

    // The last parameter (storage resolution) is optional
    Metrics.AddMetric("SuccessfulUpgrade", 1, MetricUnit.Count);
  }
}
// --8<-- [end:high_resolution_metrics]
