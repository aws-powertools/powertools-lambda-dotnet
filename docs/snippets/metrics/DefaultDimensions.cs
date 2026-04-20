// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metrics;

// --8<-- [start:set_default_dimensions]
using AWS.Lambda.Powertools.Metrics;

public class Function {
  private Dictionary<string, string> _defaultDimensions = new Dictionary<string, string>{
        {"Environment", "Prod"},
        {"Another", "One"}
    };

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.SetDefaultDimensions(_defaultDimensions);
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
  }
}
// --8<-- [end:set_default_dimensions]

// --8<-- [start:default_dims_builder_cold_start]
using AWS.Lambda.Powertools.Metrics;

public class Function {
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
// --8<-- [end:default_dims_builder_cold_start]

// --8<-- [start:default_dims_configure_cold_start]
using AWS.Lambda.Powertools.Metrics;

public class Function {

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
// --8<-- [end:default_dims_configure_cold_start]
