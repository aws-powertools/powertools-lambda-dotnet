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
* Ahead-of-Time compilation to native code support [AOT](https://docs.aws.amazon.com/lambda/latest/dg/dotnet-native-aot.html) from version 1.7.0
* Support for AspNetCore middleware and filters to capture metrics for HTTP requests

!!! warning "Migrating to v3"

    If you're upgrading to v3, please review the [Migration Guide v3](../migration-guide-v3.md) for important breaking changes including .NET 8 requirement and AWS SDK v4 migration.

## Installation

Powertools for AWS Lambda (.NET) are available as NuGet packages. You can install the packages from [NuGet Gallery](https://www.nuget.org/packages?q=AWS+Lambda+Powertools*){target="_blank"} or from Visual Studio editor by searching `AWS.Lambda.Powertools*` to see various utilities available.

* [AWS.Lambda.Powertools.Metrics](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Metrics):

    `dotnet add package AWS.Lambda.Powertools.Metrics`

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

Metrics has three global settings that will be used across all metrics emitted. Use your application or main service as the metric namespace to easily group all metrics:

 Setting                       | Description                                                                     | Environment variable | Decorator parameter 
-------------------------------|---------------------------------------------------------------------------------| ------------------------------------------------- |-----------------------
 **Metric namespace**          | Logical container where all metrics will be placed e.g. `MyCompanyEcommerce`    |  `POWERTOOLS_METRICS_NAMESPACE` | `Namespace`           
 **Service**                   | Optionally, sets **Service** metric dimension across all metrics e.g. `payment` | `POWERTOOLS_SERVICE_NAME` | `Service`             
**Disable Powertools Metrics** | Optionally, disables all Powertools metrics                                     |`POWERTOOLS_METRICS_DISABLED`                           | N/A                   |

???+ info
    `POWERTOOLS_METRICS_DISABLED` will not disable default metrics created by AWS services.

!!! info "Autocomplete Metric Units"
    All parameters in **`Metrics Attribute`** are optional. Following rules apply:

      - **Namespace:** **`Empty`** string by default. You can either specify it in code or environment variable. If not present before flushing metrics, a **`SchemaValidationException`** will be thrown.
      - **Service:** **`service_undefined`** by default. You can either specify it in code or environment variable.
      - **CaptureColdStart:** **`false`** by default. 
      - **RaiseOnEmptyMetrics:** **`false`** by default.

### Metrics object

#### Attribute

The **`MetricsAttribute`** is a class-level attribute that can be used to set the namespace and service for all metrics emitted by the lambda handler.

```csharp hl_lines="1"
--8<-- "docs/snippets/metrics/GettingStarted.cs:metrics_attribute"
```

#### Methods

The **`Metrics`** class provides methods to add metrics, dimensions, and metadata to the metrics object.

```csharp hl_lines="5-7"
--8<-- "docs/snippets/metrics/GettingStarted.cs:metrics_methods"
```

#### Initialization

The **`Metrics`** object is initialized as a Singleton and can be accessed anywhere in your code.

But can also be initialize with `Configure` or `Builder` patterns in your Lambda constructor, this the best option for testing.

Configure:

```csharp
--8<-- "docs/snippets/metrics/GettingStarted.cs:metrics_configure"
```

Builder:

```csharp
--8<-- "docs/snippets/metrics/GettingStarted.cs:metrics_builder"
```


### Creating metrics

You can create metrics using **`AddMetric`**, and you can create dimensions for all your aggregate metrics using **`AddDimension`** method.

=== "Metrics"

    ```csharp hl_lines="5 8"
    --8<-- "docs/snippets/metrics/CreatingMetrics.cs:creating_metrics"
    ```
=== "Metrics with custom dimensions"

    ```csharp hl_lines="8-9"
    --8<-- "docs/snippets/metrics/CreatingMetrics.cs:metrics_with_dimensions"
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
    --8<-- "docs/snippets/metrics/CreatingMetrics.cs:high_resolution_metrics"
    ```

!!! tip "Autocomplete Metric Resolutions"
    Use the `MetricResolution` enum to easily find a supported metric resolution by CloudWatch.

### Adding default dimensions

You can use **`SetDefaultDimensions`** method to persist dimensions across Lambda invocations.

=== "SetDefaultDimensions method"

    ```csharp hl_lines="4 5 6 7 12"
    --8<-- "docs/snippets/metrics/DefaultDimensions.cs:set_default_dimensions"
    ```

### Adding default dimensions with cold start metric

You can use the Builder or Configure patterns in your Lambda class constructor to set default dimensions.

=== "Builder pattern"

    ```csharp hl_lines="12-16"
    --8<-- "docs/snippets/metrics/DefaultDimensions.cs:default_dims_builder_cold_start"
    ```
=== "Configure pattern"

    ```csharp hl_lines="12-16"
    --8<-- "docs/snippets/metrics/DefaultDimensions.cs:default_dims_configure_cold_start"
    ```
### Adding dimensions

You can add a dimension to your metrics using the **`AddDimension`** method.

=== "Function.cs"

    ```csharp hl_lines="8"
    --8<-- "docs/snippets/metrics/AddingDimensions.cs:adding_dimensions"
    ```
=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="11 24"
    {
        "SuccessfulBooking": 1,
        "_aws": {
            "Timestamp": 1592234975665,
            "CloudWatchMetrics": [
                {
                    "Namespace": "ExampleApplication",
                    "Dimensions": [
                        [
                            "Service",
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
        "Service": "Booking",
        "Environment": "Prod"
    }
    ```

You can also add multiple dimensions at once using the **`AddDimensions`** method.

=== "Function.cs"

    ```csharp hl_lines="8-11"
    --8<-- "docs/snippets/metrics/AddingMultipleDimensions.cs:adding_multiple_dimensions"
    ```
=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="11 12 25 26"
    {
        "SuccessfulBooking": 1,
        "_aws": {
            "Timestamp": 1592234975665,
            "CloudWatchMetrics": [
                {
                    "Namespace": "ExampleApplication",
                    "Dimensions": [
                        [
                            "Service",
                            "Environment",
                            "Region"
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
        "Environment": "Prod",
        "Region": "eu-west-1"
    }
    ```

!!! info "Both methods produce the same result"
    `AddDimension` and `AddDimensions` both merge dimensions into the same dimension set in the EMF output. The only difference is ergonomics - multiple individual `AddDimension` calls vs. a single `AddDimensions` call with tuples.

    The resulting CloudWatch metric is aggregated with all dimensions combined - default dimensions plus any dimensions added via either method.

### Flushing metrics

With **`MetricsAttribute`** all your metrics are validated, serialized and flushed to standard output when lambda handler completes execution or when you had the 100th metric to memory.

You can also flush metrics manually by calling **`Flush`** method.

During metrics validation, if no metrics are provided then a warning will be logged, but no exception will be raised.

=== "Function.cs"

    ```csharp hl_lines="9"
    --8<-- "docs/snippets/metrics/FlushingMetrics.cs:flushing_metrics"
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

    ```csharp hl_lines="5"
    --8<-- "docs/snippets/metrics/FlushingMetrics.cs:raise_on_empty_metrics"
    ```

### Capturing cold start metric

You can optionally capture cold start metrics by setting **`CaptureColdStart`** parameter to `true`.

=== "Function.cs"

    ```csharp hl_lines="5"
    --8<-- "docs/snippets/metrics/CaptureColdStart.cs:capture_cold_start_attribute"
    ```
=== "Builder pattern"

    ```csharp hl_lines="9"
    --8<-- "docs/snippets/metrics/CaptureColdStart.cs:capture_cold_start_builder"
    ```
=== "Configure pattern"

    ```csharp hl_lines="11"
    --8<-- "docs/snippets/metrics/CaptureColdStart.cs:capture_cold_start_configure"
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
    --8<-- "docs/snippets/metrics/AdvancedConfiguration.cs:adding_metadata"
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
    --8<-- "docs/snippets/metrics/AdvancedConfiguration.cs:push_single_metric"
    ```

By default it will skip all previously defined dimensions including default dimensions. Use `dimensions` argument if you want to reuse default dimensions or specify custom dimensions from a dictionary.

- `Metrics.DefaultDimensions`: Reuse default dimensions when using static Metrics
- `Options.DefaultDimensions`: Reuse default dimensions when using Builder or Configure patterns

=== "New Default Dimensions.cs"

    ```csharp hl_lines="8-17"
    --8<-- "docs/snippets/metrics/AdvancedConfiguration.cs:single_metric_new_dimensions"
    ```
=== "Default Dimensions static.cs"

    ```csharp hl_lines="8-12"
    --8<-- "docs/snippets/metrics/AdvancedConfiguration.cs:single_metric_default_dimensions_static"
    ```
=== "Default Dimensions Options / Builder patterns"

    ```csharp hl_lines="9-13 18"
    --8<-- "docs/snippets/metrics/AdvancedConfiguration.cs:single_metric_default_dimensions_options"
    ```

### Cold start Function Name dimension

In cases where you want to customize the `FunctionName` dimension in Cold Start metrics.

This is useful where you want to maintain the same name in case of auto generated handler names (cdk, top-level statement functions, etc.)

Example:

=== "In decorator"
    
    ```csharp hl_lines="5"
    --8<-- "docs/snippets/metrics/AdvancedConfiguration.cs:function_name_decorator"
    ```
=== "Configure / Builder patterns"

    ```csharp hl_lines="12"
    --8<-- "docs/snippets/metrics/AdvancedConfiguration.cs:function_name_configure"
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

```csharp hl_lines="22"
--8<-- "docs/snippets/metrics/AspNetCore.cs:use_metrics_middleware"
```

Here is the highlighted `UseMetrics` method:

```csharp
--8<-- "docs/snippets/metrics/AspNetCore.cs:use_metrics_method"
```

Explanation:

- The method is defined as an extension method for the `IApplicationBuilder` interface.
- It adds a `MetricsMiddleware` to the application builder using the `UseMiddleware` method.
- The `MetricsMiddleware` captures and records metrics for HTTP requests, including cold start metrics if the `CaptureColdStart` option is enabled.

### WithMetrics() filter

The `WithMetrics` method is an extension method for the `RouteHandlerBuilder` class.

It adds a metrics filter to the specified route handler builder, which captures cold start metrics (if enabled) and flushes metrics on function exit.

#### Example

```csharp hl_lines="32"
--8<-- "docs/snippets/metrics/AspNetCore.cs:with_metrics_filter"
```

Here is the highlighted `WithMetrics` method:

```csharp
--8<-- "docs/snippets/metrics/AspNetCore.cs:with_metrics_method"
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
--8<-- "docs/snippets/metrics/Testing.cs:lambda_function_testing"
```
#### Unit Tests


```csharp
--8<-- "docs/snippets/metrics/Testing.cs:unit_tests"
```

### Environment variables

???+ tip
	Ignore this section, if:

    * You are explicitly setting namespace/default dimension via `namespace` and `service` parameters
    * You're not instantiating `Metrics` in the global namespace

	For example, `Metrics(namespace="ExampleApplication", service="booking")`

Make sure to set `POWERTOOLS_METRICS_NAMESPACE` and `POWERTOOLS_SERVICE_NAME` before running your tests to prevent failing on `SchemaValidation` exception. You can set it before you run tests by adding the environment variable.

```csharp title="Injecting Metric Namespace before running tests"
--8<-- "docs/snippets/metrics/Testing.cs:inject_metric_namespace"
```
