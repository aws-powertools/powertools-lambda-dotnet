---
title: Metrics V2
description: Core utility
---

Metrics creates custom metrics asynchronously by logging metrics to standard output following [Amazon CloudWatch Embedded Metric Format (EMF)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format.html).

These metrics can be visualized through [Amazon CloudWatch Console](https://aws.amazon.com/cloudwatch/).

## Key features

* Aggregate up to 100 metrics using a single [CloudWatch EMF](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html){target="_blank"} object (large JSON blob)
* Validating your metrics against common metric definitions mistakes (for example, metric unit, values, max dimensions, max metrics)
* Metrics are created asynchronously by the CloudWatch service. You do not need any custom stacks, and there is no impact to Lambda function latency
* Context manager to create a one off metric with a different dimension
* Ahead-of-Time compilation to native code support [AOT](https://docs.aws.amazon.com/lambda/latest/dg/dotnet-native-aot.html) from version 1.7.0
* Support for AspNetCore middleware and filters to capture metrics for HTTP requests

## Breaking changes from V1

* **`Dimensions`** outputs as an array of arrays instead of an array of objects. Example: `Dimensions: [["service", "Environment"]]` instead of `Dimensions: ["service", "Environment"]`
* **`FunctionName`** is not added as default dimension and only to cold start metric.
* **`Default Dimensions`** can now be included in Cold Start metrics, this is a potential breaking change if you were relying on the absence of default dimensions in Cold Start metrics when searching.

<br />

<figure>
  <img src="../../media/metrics_utility_showcase.png" loading="lazy" alt="Screenshot of the Amazon CloudWatch Console showing an example of business metrics in the Metrics Explorer" />
  <figcaption>Metrics showcase - Metrics Explorer</figcaption>
</figure>

## Installation

Powertools for AWS Lambda (.NET) are available as NuGet packages. You can install the packages from [NuGet Gallery](https://www.nuget.org/packages?q=AWS+Lambda+Powertools*){target="_blank"} or from Visual Studio editor by searching `AWS.Lambda.Powertools*` to see various utilities available.

* [AWS.Lambda.Powertools.Metrics](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Metrics):

    `dotnet nuget add AWS.Lambda.Powertools.Metrics`

## Terminologies

If you're new to Amazon CloudWatch, there are two terminologies you must be aware of before using this utility:

* **Namespace**. It's the highest level container that will group multiple metrics from multiple services for a given application, for example `ServerlessEcommerce`.
* **Dimensions**. Metrics metadata in key-value format. They help you slice and dice metrics visualization, for example `ColdStart` metric by Payment `service`.
* **Metric**. It's the name of the metric, for example: SuccessfulBooking or UpdatedBooking.
* **Unit**. It's a value representing the unit of measure for the corresponding metric, for example: Count or Seconds.
* **Resolution**. It's a value representing the storage resolution for the corresponding metric. Metrics can be either Standard or High resolution. Read more [here](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/cloudwatch_concepts.html#Resolution_definition).

Visit the AWS documentation for a complete explanation for [Amazon CloudWatch concepts](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/cloudwatch_concepts.html).

<figure>
  <img src="../../media/metrics_terminology.png" />
  <figcaption>Metric terminology, visually explained</figcaption>
</figure>

## Getting started

**`Metrics`** is implemented as a Singleton to keep track of your aggregate metrics in memory and make them accessible anywhere in your code. To guarantee that metrics are flushed properly the **`MetricsAttribute`** must be added on the lambda handler.

Metrics has two global settings that will be used across all metrics emitted. Use your application or main service as the metric namespace to easily group all metrics:

Setting | Description | Environment variable | Constructor parameter
------------------------------------------------- | ------------------------------------------------- | ------------------------------------------------- | -------------------------------------------------
**Service** | Optionally, sets **service** metric dimension across all metrics e.g. `payment` | `POWERTOOLS_SERVICE_NAME` | `Service`
**Metric namespace** | Logical container where all metrics will be placed e.g. `MyCompanyEcommerce` |  `POWERTOOLS_METRICS_NAMESPACE` | `Namespace`

!!! info "Autocomplete Metric Units"
    All parameters in **`Metrics Attribute`** are optional. Following rules apply:

      - **Namespace:** **`Empty`** string by default. You can either specify it in code or environment variable. If not present before flushing metrics, a **`SchemaValidationException`** will be thrown.
      - **Service:** **`service_undefined`** by default. You can either specify it in code or environment variable.
      - **CaptureColdStart:** **`false`** by default. 
      - **RaiseOnEmptyMetrics:** **`false`** by default.

### Full list of environment variables

| Environment variable | Description | Default |
| ------------------------------------------------- | --------------------------------------------------------------------------------- |  ------------------------------------------------- |
| **POWERTOOLS_SERVICE_NAME** | Sets service name used for tracing namespace, metrics dimension and structured logging | `"service_undefined"` |
| **POWERTOOLS_METRICS_NAMESPACE** | Sets namespace used for metrics | `None` |

### Metrics object

#### Attribute

The **`MetricsAttribute`** is a class-level attribute that can be used to set the namespace and service for all metrics emitted by the lambda handler.

```csharp hl_lines="3"
using AWS.Lambda.Powertools.Metrics;
    
[Metrics(Namespace = "ExampleApplication", Service = "Booking")]
public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
{
    ...
}
```

#### Methods

The **`Metrics`** class provides methods to add metrics, dimensions, and metadata to the metrics object.

```csharp hl_lines="5-7"
using AWS.Lambda.Powertools.Metrics;
    
public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
{
    Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    Metrics.AddDimension("Environment", "Prod");
    Metrics.AddMetadata("BookingId", "683EEB2D-B2F3-4075-96EE-788E6E2EED45");
    ...
}
```

#### Initialization

The **`Metrics`** object is initialized as a Singleton and can be accessed anywhere in your code.

But can also be initialize with `Configure` or `Builder` patterns in your Lambda constructor, this the best option for testing.

Configure:

```csharp
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
```

Builder:

```csharp
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
```


### Creating metrics

You can create metrics using **`AddMetric`**, and you can create dimensions for all your aggregate metrics using **`AddDimension`** method.

=== "Metrics"

    ```csharp hl_lines="5 8"
    using AWS.Lambda.Powertools.Metrics;

    public class Function {
      
      [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
      }
    }
    ```
=== "Metrics with custom dimensions"

    ```csharp hl_lines="8-9"
    using AWS.Lambda.Powertools.Metrics;

    public class Function {
      
      [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        Metrics.AddDimension("Environment","Prod");
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
      }
    }
    ```

!!! tip "Autocomplete Metric Units"
    `MetricUnit` enum facilitates finding a supported metric unit by CloudWatch.

!!! note "Metrics overflow"
    CloudWatch EMF supports a max of 100 metrics per batch. Metrics utility will flush all metrics when adding the 100th metric. Subsequent metrics, e.g. 101th, will be aggregated into a new EMF object, for your convenience.

!!! warning "Metric value must be a positive number"
    Metric values must be a positive number otherwise an `ArgumentException` will be thrown.

!!! warning "Do not create metrics or dimensions outside the handler"
    Metrics or dimensions added in the global scope will only be added during cold start. Disregard if that's the intended behavior.

### Adding high-resolution metrics

You can create [high-resolution metrics](https://aws.amazon.com/about-aws/whats-new/2023/02/amazon-cloudwatch-high-resolution-metric-extraction-structured-logs/) passing `MetricResolution` as parameter to `AddMetric`.

!!! tip "When is it useful?"
    High-resolution metrics are data with a granularity of one second and are very useful in several situations such as telemetry, time series, real-time incident management, and others.

=== "Metrics with high resolution"

    ```csharp hl_lines="9 12 15"
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
    ```

!!! tip "Autocomplete Metric Resolutions"
    Use the `MetricResolution` enum to easily find a supported metric resolution by CloudWatch.

### Adding default dimensions

You can use **`SetDefaultDimensions`** method to persist dimensions across Lambda invocations.

=== "SetDefaultDimensions method"

    ```csharp hl_lines="4 5 6 7 12"
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
    ```

### Adding default dimensions with cold start metric

You can use the Builder or Configure patterns in your Lambda class constructor to set default dimensions.

=== "Builder pattern"

    ```csharp hl_lines="12-16"
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
    ```
=== "Configure pattern"

    ```csharp hl_lines="12-16"
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
    ```
### Adding dimensions

You can add dimensions to your metrics using **`AddDimension`** method.

=== "Function.cs"

    ```csharp hl_lines="8"
    using AWS.Lambda.Powertools.Metrics;

    public class Function {
      
      [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        Metrics.AddDimension("Environment","Prod");
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
      }
    }
    ```
=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="11 24"
    {
        "SuccessfulBooking": 1.0,
        "_aws": {
            "Timestamp": 1592234975665,
            "CloudWatchMetrics": [
                {
            "Namespace": "ExampleApplication",
            "Dimensions": [
                [
                    "service",
                    "Environment"
                ]
            ],
            "Metrics": [
                {
                    "Name": "SuccessfulBooking",
                    "Unit": "Count"
                }
                ]
            }
        ]
    },
    "service": "ExampleService",
    "Environment": "Prod"
    }
    ```

### Flushing metrics

With **`MetricsAttribute`** all your metrics are validated, serialized and flushed to standard output when lambda handler completes execution or when you had the 100th metric to memory.

You can also flush metrics manually by calling **`Flush`** method.

During metrics validation, if no metrics are provided then a warning will be logged, but no exception will be raised.

=== "Function.cs"

    ```csharp hl_lines="9"
    using AWS.Lambda.Powertools.Metrics;

    public class Function {
      
      [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
        Metrics.Flush();
      }
    }
    ```
=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="2 7 10 15 22"
    {
    "BookingConfirmation": 1.0,
    "_aws": {
        "Timestamp": 1592234975665,
        "CloudWatchMetrics": [
            {
        "Namespace": "ExampleApplication",
        "Dimensions": [
            [
            "service"
            ]
        ],
        "Metrics": [
            {
            "Name": "BookingConfirmation",
            "Unit": "Count"
            }
        ]
            }
        ]
        },
    "service": "ExampleService"
    }
    ```

!!! tip "Metric validation"
    If metrics are provided, and any of the following criteria are not met, **`SchemaValidationException`** will be raised:

    * Maximum of 30 dimensions
    * Namespace is set
    * Metric units must be [supported by CloudWatch](https://docs.aws.amazon.com/AmazonCloudWatch/latest/APIReference/API_MetricDatum.html)

!!! info "We do not emit 0 as a value for ColdStart metric for cost reasons. [Let us know](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/new?assignees=&labels=feature-request%2Ctriage&template=feature_request.yml&title=Feature+request%3A+TITLE) if you'd prefer a flag to override it"

### Raising SchemaValidationException on empty metrics

If you want to ensure that at least one metric is emitted, you can pass **`RaiseOnEmptyMetrics`** to the Metrics attribute:

=== "Function.cs"

    ```python hl_lines="5"
    using AWS.Lambda.Powertools.Metrics;

    public class Function {
      
      [Metrics(RaiseOnEmptyMetrics = true)]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        ...
    ```

### Capturing cold start metric

You can optionally capture cold start metrics by setting **`CaptureColdStart`** parameter to `true`.

=== "Function.cs"

    ```csharp hl_lines="5"
    using AWS.Lambda.Powertools.Metrics;

    public class Function {
      
      [Metrics(CaptureColdStart = true)]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        ...
    ```
=== "Builder pattern"

    ```csharp hl_lines="9"
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
    ```
=== "Configure pattern"

    ```csharp hl_lines="11"
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
    ```

If it's a cold start invocation, this feature will:

* Create a separate EMF blob solely containing a metric named `ColdStart`
* Add `FunctionName` and `Service` dimensions

This has the advantage of keeping cold start metric separate from your application metrics, where you might have unrelated dimensions.

## Advanced

### Adding metadata

You can add high-cardinality data as part of your Metrics log with `AddMetadata` method. This is useful when you want to search highly contextual information along with your metrics in your logs.

!!! info
    **This will not be available during metrics visualization** - Use **dimensions** for this purpose

!!! info
    Adding metadata with a key that is the same as an existing metric will be ignored

=== "Function.cs"

    ```csharp hl_lines="9"
    using AWS.Lambda.Powertools.Metrics;
    
    public class Function {
      
      [Metrics(Namespace = ExampleApplication, Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
        Metrics.AddMetadata("BookingId", "683EEB2D-B2F3-4075-96EE-788E6E2EED45");
        ...
    ```

=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="23"
    {
      "SuccessfulBooking": 1.0,
      "_aws": {
      "Timestamp": 1592234975665,
      "CloudWatchMetrics": [
        {
      "Namespace": "ExampleApplication",
      "Dimensions": [
        [
        "service"
        ]
      ],
      "Metrics": [
        {
        "Name": "SuccessfulBooking",
        "Unit": "Count"
        }
      ]
        }
      ]
      },
      "Service": "Booking",
      "BookingId": "683EEB2D-B2F3-4075-96EE-788E6E2EED45"
    }
    ```

### Single metric with a different dimension

CloudWatch EMF uses the same dimensions across all your metrics. Use **`PushSingleMetric`** if you have a metric that should have different dimensions.

!!! info
    Generally, this would be an edge case since you [pay for unique metric](https://aws.amazon.com/cloudwatch/pricing). Keep the following formula in mind:

    **unique metric = (metric_name + dimension_name + dimension_value)**

=== "Function.cs"

    ```csharp hl_lines="8-13"
    using AWS.Lambda.Powertools.Metrics;
    
    public class Function {
      
      [Metrics(Namespace = ExampleApplication, Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        Metrics.PushSingleMetric(
                    metricName: "ColdStart",
                    value: 1,
                    unit: MetricUnit.Count,
                    nameSpace: "ExampleApplication",
                    service: "Booking");
        ...
    ```

By default it will skip all previously defined dimensions including default dimensions. Use default_dimensions keyword argument if you want to reuse default dimensions or specify custom dimensions from a dictionary.

- `Metrics.DefaultDimensions`: Reuse default dimensions when using static Metrics
- `Options.DefaultDimensions`: Reuse default dimensions when using Builder or Configure patterns

=== "New Default Dimensions.cs"

    ```csharp hl_lines="8-17"
    using AWS.Lambda.Powertools.Metrics;
    
    public class Function {
      
      [Metrics(Namespace = ExampleApplication, Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        Metrics.PushSingleMetric(
                    metricName: "ColdStart",
                    value: 1,
                    unit: MetricUnit.Count,
                    nameSpace: "ExampleApplication",
                    service: "Booking",
                    dimensions: new Dictionary<string, string>
                    {
                        {"FunctionContext", "$LATEST"}
                    });
        ...
    ```
=== "Default Dimensions static.cs"

    ```csharp hl_lines="8-12"
    using AWS.Lambda.Powertools.Metrics;
    
    public class Function {
      
      [Metrics(Namespace = ExampleApplication, Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
         Metrics.SetDefaultDimensions(new Dictionary<string, string> 
        {
            { "Default", "SingleMetric" }
        });
        Metrics.PushSingleMetric("SingleMetric", 1, MetricUnit.Count, defaultDimensions: Metrics.DefaultDimensions );
        ...
    ```
=== "Default Dimensions Options / Builder patterns .cs"

    ```csharp hl_lines="9-13 18"
    using AWS.Lambda.Powertools.Metrics;
    
    public MetricsnBuilderHandler(IMetrics metrics = null)
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
        _metrics.PushSingleMetric("SuccessfulBooking", 1, MetricUnit.Count, defaultDimensions: _metrics.Options.DefaultDimensions);
    }
        ...
    ```

## AspNetCore

### Installation

To use the Metrics middleware in an ASP.NET Core application, you need to install the `AWS.Lambda.Powertools.Metrics.AspNetCore` NuGet package.

```bash
dotnet add package AWS.Lambda.Powertools.Metrics.AspNetCore
```

### UseMetrics() Middleware

The `UseMetrics` middleware is an extension method for the `IApplicationBuilder` interface.

It adds a metrics middleware to the specified application builder, which captures cold start metrics (if enabled) and flushes metrics on function exit.

#### Example

```csharp hl_lines="21"
    
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure metrics
builder.Services.AddSingleton<IMetrics>(_ => new MetricsBuilder()
    .WithNamespace("MyApi") // Namespace for the metrics
    .WithService("WeatherService") // Service name for the metrics
    .WithCaptureColdStart(true) // Capture cold start metrics
    .WithDefaultDimensions(new Dictionary<string, string> // Default dimensions for the metrics
    {
        {"Environment", "Prod"},
        {"Another", "One"}
    })
    .Build()); // Build the metrics

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

app.UseMetrics(); // Add the metrics middleware

app.MapGet("/powertools", (IMetrics metrics) => 
    {
        // add custom metrics
        metrics.AddMetric("MyCustomMetric", 1, MetricUnit.Count);
        // flush metrics - this is required to ensure metrics are sent to CloudWatch
        metrics.Flush();
    });
    
app.Run();

```

Here is the highlighted `UseMetrics` method:

```csharp
/// <summary>
/// Adds a metrics middleware to the specified application builder.
/// This will capture cold start (if CaptureColdStart is enabled) metrics and flush metrics on function exit.
/// </summary>
/// <param name="app">The application builder to add the metrics middleware to.</param>
/// <returns>The application builder with the metrics middleware added.</returns>
public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
{
    app.UseMiddleware<MetricsMiddleware>();
    return app;
}
```

Explanation:

- The method is defined as an extension method for the `IApplicationBuilder` interface.
- It adds a `MetricsMiddleware` to the application builder using the `UseMiddleware` method.
- The `MetricsMiddleware` captures and records metrics for HTTP requests, including cold start metrics if the `CaptureColdStart` option is enabled.

### WithMetrics() filter

The `WithMetrics` method is an extension method for the `RouteHandlerBuilder` class.

It adds a metrics filter to the specified route handler builder, which captures cold start metrics (if enabled) and flushes metrics on function exit.

#### Example

```csharp hl_lines="31"

using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Metrics.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure metrics
builder.Services.AddSingleton<IMetrics>(_ => new MetricsBuilder()
    .WithNamespace("MyApi") // Namespace for the metrics
    .WithService("WeatherService") // Service name for the metrics
    .WithCaptureColdStart(true) // Capture cold start metrics
    .WithDefaultDimensions(new Dictionary<string, string> // Default dimensions for the metrics
    {
        {"Environment", "Prod"},
        {"Another", "One"}
    })
    .Build()); // Build the metrics

// Add AWS Lambda support. When the application is run in Lambda, Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the web server translating requests and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

app.MapGet("/powertools", (IMetrics metrics) => 
    {
        // add custom metrics
        metrics.AddMetric("MyCustomMetric", 1, MetricUnit.Count);
        // flush metrics - this is required to ensure metrics are sent to CloudWatch
        metrics.Flush();
    })
    .WithMetrics();

app.Run();

```

Here is the highlighted `WithMetrics` method:

```csharp
/// <summary>
/// Adds a metrics filter to the specified route handler builder.
/// This will capture cold start (if CaptureColdStart is enabled) metrics and flush metrics on function exit.
/// </summary>
/// <param name="builder">The route handler builder to add the metrics filter to.</param>
/// <returns>The route handler builder with the metrics filter added.</returns>
public static RouteHandlerBuilder WithMetrics(this RouteHandlerBuilder builder)
{
    builder.AddEndpointFilter<MetricsFilter>();
    return builder;
}
```

Explanation:

- The method is defined as an extension method for the `RouteHandlerBuilder` class.
- It adds a `MetricsFilter` to the route handler builder using the `AddEndpointFilter` method.
- The `MetricsFilter` captures and records metrics for HTTP endpoints, including cold start metrics if the `CaptureColdStart` option is enabled.
- The method returns the modified `RouteHandlerBuilder` instance with the metrics filter added.


## Testing your code

### Unit testing

To test your code that uses the Metrics utility, you can use the `TestLambdaContext` class from the `Amazon.Lambda.TestUtilities` package.

You can also use the `IMetrics` interface to mock the Metrics utility in your tests.

Here is an example of how you can test a Lambda function that uses the Metrics utility:

#### Lambda Function

```csharp
using System.Collections.Generic;
using Amazon.Lambda.Core;

public class MetricsnBuilderHandler
{
    private readonly IMetrics _metrics;

    // Allow injection of IMetrics for testing
    public MetricsnBuilderHandler(IMetrics metrics = null)
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

    [Metrics]
    public void Handler(ILambdaContext context)
    {
        _metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    }
}

```
#### Unit Tests


```csharp
[Fact]
    public void Handler_With_Builder_Should_Configure_In_Constructor()
    {
        // Arrange
        var handler = new MetricsnBuilderHandler();

        // Act
        handler.Handler(new TestLambdaContext
        {
            FunctionName = "My_Function_Name"
        });

        // Get the output and parse it
        var metricsOutput = _consoleOut.ToString();

        // Assert cold start
        Assert.Contains(
            "\"CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\",\"Environment\",\"Another\",\"FunctionName\"]]}]},\"Service\":\"testService\",\"Environment\":\"Prod1\",\"Another\":\"One\",\"FunctionName\":\"My_Function_Name\",\"ColdStart\":1}",
            metricsOutput);
        // Assert successful Memory metrics
        Assert.Contains(
            "\"CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"SuccessfulBooking\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\",\"Environment\",\"Another\",\"FunctionName\"]]}]},\"Service\":\"testService\",\"Environment\":\"Prod1\",\"Another\":\"One\",\"FunctionName\":\"My_Function_Name\",\"SuccessfulBooking\":1}",
            metricsOutput);
    }
    
    [Fact]
    public void Handler_With_Builder_Should_Configure_In_Constructor_Mock()
    {
        var metricsMock = Substitute.For<IMetrics>();

        metricsMock.Options.Returns(new MetricsOptions
        {
            CaptureColdStart = true,
            Namespace = "dotnet-powertools-test",
            Service = "testService",
            DefaultDimensions = new Dictionary<string, string>
            {
                { "Environment", "Prod" },
                { "Another", "One" }
            }
        });

        Metrics.UseMetricsForTests(metricsMock);
        
        var sut = new MetricsnBuilderHandler(metricsMock);

        // Act
        sut.Handler(new TestLambdaContext
        {
            FunctionName = "My_Function_Name"
        });

        metricsMock.Received(1).PushSingleMetric("ColdStart", 1, MetricUnit.Count, "dotnet-powertools-test",
            service: "testService", Arg.Any<Dictionary<string, string>>());
        metricsMock.Received(1).AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
    }
```

### Environment variables

???+ tip
	Ignore this section, if:

    * You are explicitly setting namespace/default dimension via `namespace` and `service` parameters
    * You're not instantiating `Metrics` in the global namespace

	For example, `Metrics(namespace="ExampleApplication", service="booking")`

Make sure to set `POWERTOOLS_METRICS_NAMESPACE` and `POWERTOOLS_SERVICE_NAME` before running your tests to prevent failing on `SchemaValidation` exception. You can set it before you run tests by adding the environment variable.

```csharp title="Injecting Metric Namespace before running tests"
Environment.SetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE","AWSLambdaPowertools");
```
