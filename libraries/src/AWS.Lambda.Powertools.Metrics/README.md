# AWS.Lambda.Powertools.Metrics

The `AWS.Lambda.Powertools.Metrics` package is part of **AWS Lambda Powertools for .NET** and allows developers to collect metrics from AWS Lambda functions according to best practices.

The `Metrics` utility captures metrics by using [Embedded Metrics Format (EMF)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html) specification. EMF is a JSON specification that allows CloudWatch to automatically extract metric values embedded in structured event logs. With this approach, your metrics collection process becomes very efficient as it isn't performed on the AWS Lambda function but instead, performed on CloudWatch.

## Examples

**AWS Lambda Powertools for .NET** uses an `Aspect` oriented approach by enabling developers to use `Attributes` to inject a static `Metrics` object to be used for metrics collection.

This optimizes developer experience by reducing repetition for utility object initialization.

Below are some implementation examples:

### Metrics initialization

The [EMF](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html) specification requires that `namespace` and `service name` properties are provided. In our implementation we enforce developers to define the namespace in *code* or as an *environment variable*.

> If you want these properties to be easily updated you should have them as environment variables

If using [SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html) just add the following environment variables to your `template.yaml` file.
```yaml
...
    Environment: 
        Variables:
          POWERTOOLS_SERVICE_NAME: [YOUR-SERVICE-NAME]
          POWERTOOLS_METRICS_NAMESPACE: [YOUR_NAMESPACE]
...
```

Once your environment variables are defined you can initialize your `Metrics` object without parameters.

```csharp
public class Function
{
    [Metrics()]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {                                
        try
        {
            // Add Metrics
            var watch = Stopwatch.StartNew();            
            var location = await GetCallingIP();
            watch.Stop();
            
            Metrics.AddMetric("ElapsedExecutionTime", watch.ElapsedMilliseconds, MetricUnit.Milliseconds);
            Metrics.AddMetric("SuccessfulLocations", 1, MetricUnit.Count);
            
            var body = new Dictionary<string, string>
            {
                { "message", "sample metrics" },
                { "location", location }
            };

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(ex.Message),
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
```

### Override metrics namespace and Service name

As mentioned before, metrics `namespace` and `service name` can be provided as environment variables. Still you can override them in code during initialization.

```csharp
public class Function
{
    [Metrics(Namespace = "dotnet-lambdapowertools", Service = "lambda-example")]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {                                
        try
        {
            // Add Metrics
            var watch = Stopwatch.StartNew();            
            var location = await GetCallingIP();
            watch.Stop();
            
            Metrics.AddMetric("ElapsedExecutionTime", watch.ElapsedMilliseconds, MetricUnit.Milliseconds);
            Metrics.AddMetric("SuccessfulLocations", 1, MetricUnit.Count);
            
            var body = new Dictionary<string, string>
            {
                { "message", "sample metrics" },
                { "location", location }
            };

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(ex.Message),
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
```

### Capture Cold Starts

`Metrics` utility can be configured to capture cold starts, which can be set during initialization. *This feature is by default disabled.*

Enabling this feature will generate one additional metric with the same `namespace` and `service name` defined for the AWS Lambda function.

```csharp
public class Function
{
    [Metrics(CaptureColdStart = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {                                
        try
        {
            // Add Metrics
            var watch = Stopwatch.StartNew();            
            var location = await GetCallingIP();
            watch.Stop();
            
            Metrics.AddMetric("ElapsedExecutionTime", watch.ElapsedMilliseconds, MetricUnit.Milliseconds);
            Metrics.AddMetric("SuccessfulLocations", 1, MetricUnit.Count);
            
            var body = new Dictionary<string, string>
            {
                { "message", "sample metrics" },
                { "location", location }
            };

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(ex.Message),
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
```

### Throw Exception if no metrics are provided

`Metrics` utility can be configured to throw a `SchemaValidationException` in case no metrics were collected before a metrics flush operation - which takes place when the *AWS Lambda function handler* completes execution.

This flag is used to enforce *metrics* collection but it might not apply to all scenarios, and for that reason it is *disabled by default*.

```csharp
public class Function
{
    [Metrics(RaiseOnEmptyMetrics = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {                                
        try
        {
            // Add Metrics
            var watch = Stopwatch.StartNew();            
            var location = await GetCallingIP();
            watch.Stop();
            
            Metrics.AddMetric("ElapsedExecutionTime", watch.ElapsedMilliseconds, MetricUnit.Milliseconds);
            Metrics.AddMetric("SuccessfulLocations", 1, MetricUnit.Count);
            
            var body = new Dictionary<string, string>
            {
                { "message", "sample metrics" },
                { "location", location }
            };

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(ex.Message),
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
```

### Adding Metrics

After `Metrics` object initialization, you can collect metrics by using the `AddMetric` method that takes 3 parameters: *Metric name*, *Metric value* and *Metric unit*.

**Metric unit** is implemented as an enum to improve developer experience and prevent coding errors.

```csharp

Metrics.AddMetric("ElapsedExecutionTime", watch.ElapsedMilliseconds, MetricUnit.Milliseconds);
```

### Add Dimensions

Adding `Dimensions` to your metric collection process allows for metric segmentation and easier filtering which facilitates the process of adding metrics to charts and dashboards in *Amazon CloudWatch*.

*By default, the `service name` is added automatically as a dimension*. You can add your custom dimensions by using the `AddDimension` method as shown in the example below.

```csharp

Metrics.AddDimension("Environment", "Prod");
```

### Add Metadata

You can add high-cardinality data as part of your Metrics log with the `AddMetadata` method. This is useful when you want to search highly contextual information along with your metrics in your logs.

```csharp

Metrics.AddMetadata("BookingId", "e36469c2-524a-4265-af07-214be7ad2297");
```

### Collect single Metric with custom namespace and service name

In the case that you want to capture individual metrics, or capture metrics with different namespace or service name you can use the `PushSingleMetric` method.

```csharp
public class Function
{        
    [Metrics()]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {                                
        try
        {
            // Add Single Metric
            Metrics.PushSingleMetric(
                metricName: "CallingIP",
                value: 1,
                unit: MetricUnit.Count,
                nameSpace: "dotnet-lambdapowertools",
                service: "lambda-example",
                defaultDimensions: new Dictionary<string, string>
                {
                    {"Metric Type", "Single"}
                });
           
            
            var body = new Dictionary<string, string>
            {
                { "message", "hello world" }
            };                   

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(ex.Message),
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
```

## Disclaimer

This is not an extensive list of examples but we tried to cover the most common use cases for the library.

If you would like to see other examples please open an issue on the repo or create a Pull Request with more examples.
