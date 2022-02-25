---
title: Logging
description: Core utility
---

Logging provides an opinionated logger with output structured as JSON.

**Key features**

* Capture key fields from Lambda context, cold start and structures logging output as JSON
* Log Lambda event when instructed (disabled by default)
* Log sampling enables DEBUG log level for a percentage of requests (disabled by default)
* Append additional keys to structured log at any point in time

## Getting started

Logging requires two settings:

Setting | Description | Environment variable | Attribute parameter
------------------------------------------------- | ------------------------------------------------- | ------------------------------------------------- | -------------------------------------------------
**Logging level** | Sets how verbose Logger should be (Information, by default) |  `LOG_LEVEL` | `LogLevel`
**Service** | Sets **Service** key that will be present across all log statements | `POWERTOOLS_SERVICE_NAME` | `Service`


> Example using AWS Serverless Application Model (SAM)

You can also override log level by setting **`POWERTOOLS_LOG_LEVEL`** env var. Here is an example using AWS Serverless Application Model (SAM)

=== "template.yaml"

    ```yaml hl_lines="8 9"
    Resources:
        HelloWorldFunction:
            Type: AWS::Serverless::Function
            Properties:
            ...
            Environment:
                Variables:
                    POWERTOOLS_LOG_LEVEL: Debug
                    POWERTOOLS_SERVICE_NAME: example
    ```

You can also explicitly set a service name via **`POWERTOOLS_SERVICE_NAME`** env var. This sets **service** key that will be present across all log statements.

## Standard structured keys

Your logs will always include the following keys to your structured logging:

Key | Type | Example | Description
------------------------------------------------- | ------------------------------------------------- | --------------------------------------------------------------------------------- | -------------------------------------------------
**Timestamp** | string | "2020-05-24 18:17:33,774" | Timestamp of actual log statement
**Level** | string | "Information" | Logging level
**Name** | string | "Powertools Logger" | Logger name
**ColdStart** | bool | true| ColdStart value.
**Service** | string | "payment" | Service name defined. "service_undefined" will be used if unknown
**SamplingRate** | int |  0.1 | Debug logging sampling rate in percentage e.g. 10% in this case
**Message** | string |  "Collecting payment" | Log statement value. Unserializable JSON values will be casted to string
**FunctionName**| string | "example-powertools-HelloWorldFunction-1P1Z6B39FLU73"
**FunctionVersion**| string | "12"
**FunctionMemorySize**| string | "128"
**FunctionArn**| string | "arn:aws:lambda:eu-west-1:012345678910:function:example-powertools-HelloWorldFunction-1P1Z6B39FLU73"
**XRayTraceId**| string | "1-5759e988-bd862e3fe1be46a994272793" | X-Ray Trace ID when Lambda function has enabled Tracing
**FunctionRequestId**| string | "899856cb-83d1-40d7-8611-9e78f15f32f4" | AWS Request ID from lambda context

## Logging incoming event

When debugging in non-production environments, you can instruct Logger to log the incoming event with `LogEvent` parameter or via POWERTOOLS_LOGGER_LOG_EVENT environment variable.

!!! warning
    Log event is disabled by default to prevent sensitive info being logged.

=== "Function.cs"

    ```c# hl_lines="6"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(LogEvent = true)]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
        }
    }
    ```

## Setting a Correlation ID

You can set a Correlation ID using `CorrelationIdPath` parameter by passing a [JSON Pointer expression](https://datatracker.ietf.org/doc/html/draft-ietf-appsawg-json-pointer-03){target="_blank"}.

=== "Function.cs"

    ```c# hl_lines="6"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(CorrelationIdPath = "/headers/my_request_id_header")]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
        }
    }
    ```
=== "Example Event"

    ```json hl_lines="3"
    {
        "headers": {
            "my_request_id_header": "correlation_id_value"
        }
    }
    ```

=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="15"
    {
        "cold_start": true,
        "xray_trace_id": "1-61b7add4-66532bb81441e1b060389429",
        "function_name": "test",
        "function_version": "$LATEST",
        "function_memory_size": 128,
        "function_arn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
        "function_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72",
        "timestamp": "2021-12-13T20:32:22.5774262Z",
        "level": "Information",
        "service": "lambda-example",
        "name": "AWS.Lambda.Powertools.Logging.Logger",
        "message": "Collecting payment",
        "sampling_rate": 0.7,
        "correlation_id": "correlation_id_value",
    }
    ```
We provide [built-in JSON Pointer expression](https://datatracker.ietf.org/doc/html/draft-ietf-appsawg-json-pointer-03){target="_blank"}
for known event sources, where either a request ID or X-Ray Trace ID are present.

=== "Function.cs"

    ```c# hl_lines="6"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(CorrelationIdPath = CorrelationIdPaths.API_GATEWAY_REST)]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
        }
    }
    ```

=== "Example Event"

    ```json hl_lines="3"
    {
        "RequestContext": {
            "RequestId": "correlation_id_value"
        }
    }
    ```

=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="15"
    {
        "cold_start": true,
        "xray_trace_id": "1-61b7add4-66532bb81441e1b060389429",
        "function_name": "test",
        "function_version": "$LATEST",
        "function_memory_size": 128,
        "function_arn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
        "function_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72",
        "timestamp": "2021-12-13T20:32:22.5774262Z",
        "level": "Information",
        "service": "lambda-example",
        "name": "AWS.Lambda.Powertools.Logging.Logger",
        "message": "Collecting payment",
        "sampling_rate": 0.7,
        "correlation_id": "correlation_id_value",
    }
    ```

## Appending additional keys

!!! info "Custom keys are persisted across warm invocations"
        Always set additional keys as part of your handler to ensure they have the latest value, or explicitly clear them with [`ClearState=true`](#clearing-all-state).

You can append your own keys to your existing logs via `AppendKey`.

=== "Function.cs"

    ```c# hl_lines="11 19"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(LogEvent = true)]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
            Logger.AppendKey("test", "willBeLogged");
            ...
            var customKeys = new Dictionary<string, string>
            {
                {"test1", "value1"}, 
                {"test2", "value2"}
            };
            
            Logger.AppendKeys(customKeys);
            ...
        }
    }
    ```

### Removing additional keys

You can remove any additional key from entry using `Logger.RemoveKeys()`.

=== "Function.cs"

    ```c# hl_lines="21 22"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(LogEvent = true)]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
            Logger.AppendKey("test", "willBeLogged");
            ...
            var customKeys = new Dictionary<string, string>
            {
                {"test1", "value1"}, 
                {"test2", "value2"}
            };
            
            Logger.AppendKeys(customKeys);
            ...
            Logger.RemoveKeys("test");
            Logger.RemoveKeys("test1", "test2");
            ...
        }
    }
    ```

## Extra Keys

Extra keys argument is available for all log levels' methods, as implemented in the standard logging library - e.g. Logger.Information, Logger.Warning.

It accepts any dictionary, and all keyword arguments will be added as part of the root structure of the logs for that log statement.

!!! info
        Any keyword argument added using extra keys will not be persisted for subsequent messages.

=== "Function.cs"

    ```c# hl_lines="16"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(LogEvent = true)]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
            var extraKeys = new Dictionary<string, string>
            {
                {"extraKey1", "value1"}
            };
            
            Logger.LogInformation(extraKeys, "Collecting payment");
            ...
        }
    }
    ```

### Clearing all state

Logger is commonly initialized in the global scope. Due to [Lambda Execution Context reuse](https://docs.aws.amazon.com/lambda/latest/dg/runtimes-context.html), this means that custom keys can be persisted across invocations. If you want all custom keys to be deleted, you can use `ClearState=true` attribute on `[Logging]` attribute.

=== "Function.cs"

    ```cs hl_lines="6 13"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(ClearState = true)]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
            if (apigProxyEvent.Headers.ContainsKey("SomeSpecialHeader"))
            {
                Logger.AppendKey("SpecialKey", "value");
            }

            Logger.LogInformation("Collecting payment");
            ...
        }
    }
    ```
=== "#1 Request"

    ```json hl_lines="11"
    {
        "level": "Information",
        "message": "Collecting payment",
        "timestamp": "2021-12-13T20:32:22.5774262Z",
        "service": "payment",
        "cold_start": true,
        "function_name": "test",
        "function_memory_size": 128,
        "function_arn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
        "function_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72",
        "special_key": "value"
    }
    ```

=== "#2 Request"

    ```json
    {
        "level": "Information",
        "message": "Collecting payment",
        "timestamp": "2021-12-13T20:32:22.5774262Z",
        "service": "payment",
        "cold_start": true,
        "function_name": "test",
        "function_memory_size": 128,
        "function_arn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
        "function_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72"
    }
    ```

## Sampling debug logs

You can dynamically set a percentage of your logs to **DEBUG** level via env var `POWERTOOLS_LOGGER_SAMPLE_RATE` or
via `SamplingRate` parameter on attribute.

!!! info
    Configuration on environment variable is given precedence over sampling rate configuration on attribute, provided it's in valid value range.

=== "Sampling via attribute parameter"

    ```c# hl_lines="6"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(SamplingRate = 0.5)]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
        }
    }
    ```

=== "Sampling via environment variable"

    ```yaml hl_lines="8"
    Resources:
        HelloWorldFunction:
            Type: AWS::Serverless::Function
            Properties:
            ...
            Environment:
                Variables:
                    POWERTOOLS_LOGGER_SAMPLE_RATE: 0.5
    ```

## Configure Log Output Casing

By definition *AWS Lambda Powertools for .NET* outputs logging keys using **snake case** (e.g. *"function_memory_size": 128*). This allows developers using different AWS Lambda Powertools runtimes, to search logs across services written in languages such as Python or TypeScript.

If you want to override the default behaviour you can either set the desired casing through attributes, as described in the example below, or by setting the `POWERTOOLS_LOGGER_CASE` environment variable on your AWS Lambda function. Allowed values are: `CamelCase`, `PascalCase` and `SnakeCase`.

=== "Output casing via attribute parameter"

    ```c# hl_lines="6"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(LoggerOutputCase = LoggerOutputCase.CamelCase)]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
        }
    }
    ```

Below are some output examples for different casing.

=== "Camel Case"

    ```json
    {
        "level": "Information",
        "message": "Collecting payment",
        "timestamp": "2021-12-13T20:32:22.5774262Z",
        "service": "payment",
        "coldStart": true,
        "functionName": "test",
        "functionMemorySize": 128,
        "functionArn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
        "functionRequestId": "52fdfc07-2182-154f-163f-5f0f9a621d72"
    }
    ```

=== "Pascal Case"

    ```json
    {
        "Level": "Information",
        "Message": "Collecting payment",
        "Timestamp": "2021-12-13T20:32:22.5774262Z",
        "Service": "payment",
        "ColdStart": true,
        "FunctionName": "test",
        "FunctionMemorySize": 128,
        "FunctionArn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
        "FunctionRequestId": "52fdfc07-2182-154f-163f-5f0f9a621d72"
    }
    ```

=== "Snake Case"

    ```json
    {
        "level": "Information",
        "message": "Collecting payment",
        "timestamp": "2021-12-13T20:32:22.5774262Z",
        "service": "payment",
        "cold_start": true,
        "function_name": "test",
        "function_memory_size": 128,
        "function_arn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
        "function_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72"
    }
    ```
