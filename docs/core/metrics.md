---
title: Metrics
description: Core utility
---

Metrics creates custom metrics asynchronously by logging metrics to standard output following [Amazon CloudWatch Embedded Metric Format (EMF)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format.html).

These metrics can be visualized through [Amazon CloudWatch Console](https://console.aws.amazon.com/cloudwatch/).

## Key features

* Aggregate up to 100 metrics using a single CloudWatch EMF object (large JSON blob)
* Validate against common metric definitions mistakes (metric unit, values, max dimensions, max metrics, etc)
* Metrics are created asynchronously by CloudWatch service, no custom stacks needed
* Context manager to create a one off metric with a different dimension

## Terminologies

If you're new to Amazon CloudWatch, there are two terminologies you must be aware of before using this utility:

* **Namespace**. It's the highest level container that will group multiple metrics from multiple services for a given application, for example `MyCompanyEcommerce`.
* **Dimensions**. Metrics metadata in key-value format. They help you slice and dice metrics visualization, for example `ColdStart` metric by Payment `service`.

<figure>
  <img src="../../media/metrics_terminology.png" />
  <figcaption>Metric terminology, visually explained</figcaption>
</figure>


## Getting started

Metric has two global settings that will be used across all metrics emitted:

Setting | Description | Environment variable | Constructor parameter
------------------------------------------------- | ------------------------------------------------- | ------------------------------------------------- | -------------------------------------------------
**Metric namespace** | Logical container where all metrics will be placed e.g. `MyCompanyEcommerce` |  `POWERTOOLS_METRICS_NAMESPACE` | `Namespace`
**Service** | Optionally, sets **service** metric dimension across all metrics e.g. `payment` | `POWERTOOLS_SERVICE_NAME` | `Service`

!!! tip "Use your application or main service as the metric namespace to easily group all metrics"

> Example using AWS Serverless Application Model (AWS SAM)

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
      Metrics(Namespace = "MyCompanyEcommerce", Service = "ShoppingCartService", CaptureColdStart = true, RaiseOnEmptyMetrics = true)]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        ...
      }
    }
    ```

!!! Warning "Metrics Attribute placement"
    **`Metrics`** is implemented as a Singleton to keep track of your aggregate metrics in memory and make them accessible anywhere in your code. To guarantee that metrics are flushed properly the **`MetricsAttribute`** must be added on the lambda handler.

!!! info "Autocomplete Metric Units"
    All parameters in **`Metrics Attribute`** are optional. Following rules apply:
      
      - **Namespace:** **`Empty`** string by default. You can either specify it in code or environment variable. If not present before flushing metrics, a **`SchemaValidationException`** will be thrown.
      - **Service:** **`service_undefined`** by default. You can either specify it in code or environment variable.
      - **CaptureColdStart:** **`false`** by default. 
      - **RaiseOnEmptyMetrics:** **`false`** by default.

### Creating metrics

You can create metrics using **`AddMetric`**, and you can create dimensions for all your aggregate metrics using **`AddDimension`** method.

=== "Metrics"

    ```csharp hl_lines="8"
    using AWS.Lambda.Powertools.Metrics;

    public class Function {
      
      [Metrics(Namespace = "ExampleApplication", Service = "Booking")]
      public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
      {
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.COUNT);
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
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.COUNT);
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
    Metrics or dimensions added in the global scope will only be added during cold start. Disregard if that's the intended behaviour.

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
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.COUNT);
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
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.COUNT);
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

!!! info "We do not emit 0 as a value for ColdStart metric for cost reasons. [Let us know](https://github.com/awslabs/aws-lambda-powertools-python/issues/new?assignees=&labels=feature-request%2C+triage&template=feature_request.md&title=) if you'd prefer a flag to override it"

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
        Metrics.AddMetric("SuccessfulBooking", 1, MetricUnit.COUNT);
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
                    unit: MetricUnit.COUNT,
                    nameSpace: "ExampleApplication",
                    service: "Booking",
                    defaultDimensions: new Dictionary<string, string>
                    {
                        {"FunctionContext", "$LATEST"}
                    });
        ...
    ```
