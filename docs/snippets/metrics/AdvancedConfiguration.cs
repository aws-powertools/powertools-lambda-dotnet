using AWS.Lambda.Powertools.Metrics;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/metrics.md
// via pymdownx.snippets (mkdocs).

namespace Docs.MetricsSnippets.AdvancedConfiguration.adding_metadata
{
// --8<-- [start:adding_metadata]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    Metrics.AddMetadata("BookingId", "683EEB2D-B2F3-4075-96EE-788E6E2EED45");
    // ...
// --8<-- [end:adding_metadata]
    return null;
}
}

}

namespace Docs.MetricsSnippets.AdvancedConfiguration.push_single_metric
{
// --8<-- [start:push_single_metric]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.PushSingleMetric(
                name: "ColdStart",
                value: 1,
                unit: MetricUnit.Count,
                nameSpace: "ExampleApplication",
                service: "Booking");
    // ...
// --8<-- [end:push_single_metric]
    return null;
}
}

}

namespace Docs.MetricsSnippets.AdvancedConfiguration.single_metric_new_dimensions
{
// --8<-- [start:single_metric_new_dimensions]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.PushSingleMetric(
                name: "ColdStart",
                value: 1,
                unit: MetricUnit.Count,
                nameSpace: "ExampleApplication",
                service: "Booking",
                dimensions: new Dictionary<string, string>
                {
                    {"FunctionContext", "$LATEST"}
                });
    // ...
// --8<-- [end:single_metric_new_dimensions]
    return null;
}
}

}

namespace Docs.MetricsSnippets.AdvancedConfiguration.single_metric_default_dimensions_static
{
// --8<-- [start:single_metric_default_dimensions_static]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
     Metrics.SetDefaultDimensions(new Dictionary<string, string>
    {
        { "Default", "SingleMetric" }
    });
    Metrics.PushSingleMetric("SingleMetric", 1, MetricUnit.Count, dimensions: Metrics.DefaultDimensions );
    // ...
// --8<-- [end:single_metric_default_dimensions_static]
    return null;
}
}

}

namespace Docs.MetricsSnippets.AdvancedConfiguration.single_metric_default_dimensions_options
{
public class Function
{
    private readonly IMetrics _metrics;
// --8<-- [start:single_metric_default_dimensions_options]

public Function(IMetrics metrics = null)
{
    _metrics = metrics ?? new MetricsBuilder()
        .WithCaptureColdStart(true)
        .WithService("testService")
        .WithNamespace("dotnet-powertools-test")
        .WithDefaultDimensions(new Dictionary<string, string>
        {
            { "Environment", "Prod1" },
            { "Another", "One" }
        }).Build();
}

public void HandlerSingleMetricDimensions()
{
    _metrics.PushSingleMetric("SuccessfulBooking", 1, MetricUnit.Count, dimensions: _metrics.Options.DefaultDimensions);
}
    // ...
// --8<-- [end:single_metric_default_dimensions_options]
}
}

namespace Docs.MetricsSnippets.AdvancedConfiguration.function_name_decorator
{
// --8<-- [start:function_name_decorator]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  [Metrics(FunctionName = "MyFunctionName", Namespace = "ExampleApplication", Service = "Booking")]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    // ...
    return null;
  }
// --8<-- [end:function_name_decorator]
}

}

namespace Docs.MetricsSnippets.AdvancedConfiguration.function_name_configure
{
// --8<-- [start:function_name_configure]
using AWS.Lambda.Powertools.Metrics;

public class Function {

  public Function()
  {
    Metrics.Configure(options =>
    {
        options.Namespace = "dotnet-powertools-test";
        options.Service = "testService";
        options.CaptureColdStart = true;
        options.FunctionName = "MyFunctionName";
    });
  }

  [Metrics]
  public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
  {
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    // ...
    return null;
  }
// --8<-- [end:function_name_configure]
}

}
