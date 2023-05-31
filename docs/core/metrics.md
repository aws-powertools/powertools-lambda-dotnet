---
title: Metrics
description: Core utility
---

Metrics creates custom metrics asynchronously by logging metrics to standard output following [Amazon CloudWatch Embedded Metric Format (EMF)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format.html).

These metrics can be visualized through [Amazon CloudWatch Console](https://aws.amazon.com/cloudwatch/).

## Key features

* Aggregate up to 100 metrics using a single [CloudWatch EMF](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html){target="_blank"} object (large JSON blob)
* Validating your metrics against common metric definitions mistakes (for example, metric unit, values, max dimensions, max metrics)
* Metrics are created asynchronously by the CloudWatch service. You do not need any custom stacks, and there is no impact to Lambda function latency
* Context manager to create a one off metric with a different dimension

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

### Example using AWS Serverless Application Model (AWS SAM)

=== "template.yml"

    ```yaml hl_lines="9 10"
    Resources:
    HelloWorldFunction:
        Type: AWS::Serverless::Function 
        Properties:
        ...
        Environment: 
        Variables:
            POWERTOOLS_SERVICE_NAME: ShoppingCartService
            POWERTOOLS_METRICS_NAMESPACE: MyCompanyEcommerce
    ```

=== "Function.cs"

    ```csharp hl_lines="4"
    using AWS.Lambda.Powertools.Metrics;

    public class Function {
      [Metrics(Namespace = "MyCompanyEcommerce", Service = "ShoppingCartService", CaptureColdStart = true, RaiseOnEmptyMetrics = true)]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        ...
      }
    }
    ```

### Full list of environment variables

| Environment variable | Description | Default |
| ------------------------------------------------- | --------------------------------------------------------------------------------- |  ------------------------------------------------- |
| **POWERTOOLS_SERVICE_NAME** | Sets service name used for tracing namespace, metrics dimension and structured logging | `"service_undefined"` |
| **POWERTOOLS_METRICS_NAMESPACE** | Sets namespace used for metrics | `None` |

### Creating metrics

You can create metrics using **`AddMetric`**, and you can create dimensions for all your aggregate metrics using **`AddDimension`** method.

=== "Metrics"

    ```csharp hl_lines="8"
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

### Flushing metrics

With **`MetricsAttribute`** all your metrics are validated, serialized and flushed to standard output when lambda handler completes execution or when you had the 100th metric to memory.

During metrics validation, if no metrics are provided then a warning will be logged, but no exception will be raised.

=== "Function.cs"

    ```csharp hl_lines="8"
    using AWS.Lambda.Powertools.Metrics;

    public class Function {
      
      [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.Count);
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

    * Maximum of 9 dimensions
    * Namespace is set
    * Metric units must be [supported by CloudWatch](https://docs.aws.amazon.com/AmazonCloudWatch/latest/APIReference/API_MetricDatum.html)

!!! info "We do not emit 0 as a value for ColdStart metric for cost reasons. [Let us know](https://github.com/aws-powertools/lambda-dotnet/issues/new?assignees=&labels=feature-request%2Ctriage&template=feature_request.yml&title=Feature+request%3A+TITLE) if you'd prefer a flag to override it"

#### Raising SchemaValidationException on empty metrics

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

If it's a cold start invocation, this feature will:

* Create a separate EMF blob solely containing a metric named `ColdStart`
* Add `function_name` and `service` dimensions

This has the advantage of keeping cold start metric separate from your application metrics, where you might have unrelated dimensions.

## Advanced

### Adding metadata

You can add high-cardinality data as part of your Metrics log with `AddMetadata` method. This is useful when you want to search highly contextual information along with your metrics in your logs.

!!! info
    **This will not be available during metrics visualization** - Use **dimensions** for this purpose

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
                    defaultDimensions: new Dictionary<string, string>
                    {
                        {"FunctionContext", "$LATEST"}
                    });
        ...
    ```

## Testing your code

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
