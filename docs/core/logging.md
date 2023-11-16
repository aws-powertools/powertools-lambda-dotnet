---
title: Logging
description: Core utility
---

The logging utility provides a Lambda optimized logger with output structured as JSON.

## Key features

* Capture key fields from Lambda context, cold start and structures logging output as JSON
* Log Lambda event when instructed (disabled by default)
* Log sampling enables DEBUG log level for a percentage of requests (disabled by default)
* Append additional keys to structured log at any point in time

## Installation

Powertools for AWS Lambda (.NET) are available as NuGet packages. You can install the packages from [NuGet Gallery](https://www.nuget.org/packages?q=AWS+Lambda+Powertools*){target="_blank"} or from Visual Studio editor by searching `AWS.Lambda.Powertools*` to see various utilities available.

* [AWS.Lambda.Powertools.Logging](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Logging):

    `dotnet add package AWS.Lambda.Powertools.Logging`

## Getting started

Logging requires two settings:

Setting | Description | Environment variable | Attribute parameter
------------------------------------------------- | ------------------------------------------------- | ------------------------------------------------- | -------------------------------------------------
**Service** | Sets **Service** key that will be present across all log statements | `POWERTOOLS_SERVICE_NAME` | `Service`
**Logging level** | Sets how verbose Logger should be (Information, by default) |  `POWERTOOLS_LOG_LEVEL` | `LogLevel`

!!! warning "When using Lambda Log Format JSON"
    - And Powertools Logger output is set to `PascalCase` **`Level`**  property name will be replaced by **`LogLevel`** as a property name.
    - The Lambda Application log level setting will control what is sent to CloudWatch. It takes precedence over **`POWERTOOLS_LOG_LEVEL`** and when setting it in code using **`[Logging(LogLevel = )]`**

### Example using AWS Serverless Application Model (AWS SAM)

You can override log level by setting **`POWERTOOLS_LOG_LEVEL`** environment variable in the AWS SAM template.

You can also explicitly set a service name via **`POWERTOOLS_SERVICE_NAME`** environment variable. This sets **Service** key that will be present across all log statements.

Here is an example using the AWS SAM [Globals section](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-specification-template-anatomy-globals.html).

=== "template.yaml"

    ```yaml hl_lines="13 14"
    # Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
    # SPDX-License-Identifier: MIT-0
    AWSTemplateFormatVersion: "2010-09-09"
    Transform: AWS::Serverless-2016-10-31
    Description: >
    Example project for Powertools for AWS Lambda (.NET) Logging utility

    Globals:
        Function:
            Timeout: 10
            Environment:
            Variables:
                POWERTOOLS_SERVICE_NAME: powertools-dotnet-logging-sample
                POWERTOOLS_LOG_LEVEL: Debug
                POWERTOOLS_LOGGER_LOG_EVENT: true
                POWERTOOLS_LOGGER_CASE: PascalCase # Allowed values are: CamelCase, PascalCase and SnakeCase
                POWERTOOLS_LOGGER_SAMPLE_RATE: 0
    ```

### Full list of environment variables

| Environment variable | Description | Default |
| ------------------------------------------------- | --------------------------------------------------------------------------------- |  ------------------------------------------------- |
| **POWERTOOLS_SERVICE_NAME** | Sets service name used for tracing namespace, metrics dimension and structured logging | `"service_undefined"` |
| **POWERTOOLS_LOG_LEVEL** | Sets logging level |  `Information` |
| **POWERTOOLS_LOGGER_CASE** | Override the default casing for log keys | `SnakeCase` |
| **POWERTOOLS_LOGGER_LOG_EVENT** | Logs incoming event |  `false` |
| **POWERTOOLS_LOGGER_SAMPLE_RATE** | Debug log sampling |  `0` |

## Standard structured keys

Your logs will always include the following keys to your structured logging:

Key | Type | Example | Description
------------------------------------------------- | ------------------------------------------------- | --------------------------------------------------------------------------------- | -------------------------------------------------
**Timestamp** | string | "2020-05-24 18:17:33,774" | Timestamp of actual log statement
**Level** | string | "Information" | Logging level
**Name** | string | "Powertools for AWS Lambda (.NET) Logger" | Logger name
**ColdStart** | bool | true| ColdStart value.
**Service** | string | "payment" | Service name defined. "service_undefined" will be used if unknown
**SamplingRate** | int |  0.1 | Debug logging sampling rate in percentage e.g. 10% in this case
**Message** | string |  "Collecting payment" | Log statement value. Unserializable JSON values will be cast to string
**FunctionName**| string | "example-powertools-HelloWorldFunction-1P1Z6B39FLU73"
**FunctionVersion**| string | "12"
**FunctionMemorySize**| string | "128"
**FunctionArn**| string | "arn:aws:lambda:eu-west-1:012345678910:function:example-powertools-HelloWorldFunction-1P1Z6B39FLU73"
**XRayTraceId**| string | "1-5759e988-bd862e3fe1be46a994272793" | X-Ray Trace ID when Lambda function has enabled Tracing
**FunctionRequestId**| string | "899856cb-83d1-40d7-8611-9e78f15f32f4" | AWS Request ID from lambda context

## Logging incoming event

When debugging in non-production environments, you can instruct Logger to log the incoming event with `LogEvent` parameter or via `POWERTOOLS_LOGGER_LOG_EVENT` environment variable.

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

You can append your own keys to your existing logs via `AppendKey`. Typically this value would be passed into the function via the event. Appended keys are added to all subsequent log entries in the current execution from the point the logger method is called. To ensure the key is added to all log entries, call this method as early as possible in the Lambda handler.

=== "Function.cs"

    ```c# hl_lines="21"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        [Logging(LogEvent = true)]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigwProxyEvent,
            ILambdaContext context)
        {
            var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;
            
        var lookupInfo = new Dictionary<string, object>()
        {
            {"LookupInfo", new Dictionary<string, object>{{ "LookupId", requestContextRequestId }}}
        };  

        // Appended keys are added to all subsequent log entries in the current execution.
        // Call this method as early as possible in the Lambda handler.
        // Typically this is value would be passed into the function via the event.
        // Set the ClearState = true to force the removal of keys across invocations,
        Logger.AppendKeys(lookupInfo);

        Logger.LogInformation("Getting ip address from external service");

    }
    ```
=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="4 5 6"
    {
        "cold_start": false,
        "xray_trace_id": "1-622eede0-647960c56a91f3b071a9fff1",
        "lookup_info": {
            "lookup_id": "4c50eace-8b1e-43d3-92ba-0efacf5d1625"
        },
        "function_name": "PowertoolsLoggingSample-HelloWorldFunction-hm1r10VT3lCy",
        "function_version": "$LATEST",
        "function_memory_size": 256,
        "function_arn": "arn:aws:lambda:ap-southeast-2:538510314095:function:PowertoolsLoggingSample-HelloWorldFunction-hm1r10VT3lCy",
        "function_request_id": "96570b2c-f00e-471c-94ad-b25e95ba7347",
        "timestamp": "2022-03-14T07:25:20.9418065Z",
        "level": "Information",
        "service": "powertools-dotnet-logging-sample",
        "name": "AWS.Lambda.Powertools.Logging.Logger",
        "message": "Getting ip address from external service"
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

Extra keys allow you to append additional keys to a log entry. Unlike `AppendKey`, extra keys will only apply to the current log entry.

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
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigwProxyEvent,
            ILambdaContext context)
        {
            var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;
            
            var lookupId = new Dictionary<string, object>()
            {
                { "LookupId", requestContextRequestId }
            };

            // Appended keys are added to all subsequent log entries in the current execution.
            // Call this method as early as possible in the Lambda handler.
            // Typically this is value would be passed into the function via the event.
            // Set the ClearState = true to force the removal of keys across invocations,
            Logger.AppendKeys(lookupId);
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

By definition Powertools for AWS Lambda (.NET) outputs logging keys using **snake case** (e.g. *"function_memory_size": 128*). This allows developers using different Powertools for AWS Lambda (.NET) runtimes, to search logs across services written in languages such as Python or TypeScript.

If you want to override the default behavior you can either set the desired casing through attributes, as described in the example below, or by setting the `POWERTOOLS_LOGGER_CASE` environment variable on your AWS Lambda function. Allowed values are: `CamelCase`, `PascalCase` and `SnakeCase`.

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

## Custom Log formatter (Bring Your Own Formatter)

You can customize the structure (keys and values) of your log entries by implementing a custom log formatter and override default log formatter using ``Logger.UseFormatter`` method. You can implement a custom log formatter by inheriting the ``ILogFormatter`` class and implementing the ``object FormatLogEntry(LogEntry logEntry)`` method.

=== "Function.cs"

    ```c# hl_lines="11"
    /**
     * Handler for requests to Lambda function.
     */
    public class Function
    {
        /// <summary>
        /// Function constructor
        /// </summary>
        public Function()
        {
            Logger.UseFormatter(new CustomLogFormatter());
        }

        [Logging(CorrelationIdPath = "/headers/my_request_id_header", SamplingRate = 0.7)]
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            ...
        }
    }
    ```
=== "CustomLogFormatter.cs"

    ```c#
    public class CustomLogFormatter : ILogFormatter
    {
        public object FormatLogEntry(LogEntry logEntry)
        {
            return new
            {
                Message = logEntry.Message,
                Service = logEntry.Service,
                CorrelationIds = new 
                {
                    AwsRequestId = logEntry.LambdaContext?.AwsRequestId,
                    XRayTraceId = logEntry.XRayTraceId,
                    CorrelationId = logEntry.CorrelationId
                },
                LambdaFunction = new
                {
                    Name = logEntry.LambdaContext?.FunctionName,
                    Arn = logEntry.LambdaContext?.InvokedFunctionArn,
                    MemoryLimitInMB = logEntry.LambdaContext?.MemoryLimitInMB,
                    Version = logEntry.LambdaContext?.FunctionVersion,
                    ColdStart = logEntry.ColdStart,
                },
                Level = logEntry.Level.ToString(),
                Timestamp = logEntry.Timestamp.ToString("o"),
                Logger = new
                {
                    Name = logEntry.Name,
                    SampleRate = logEntry.SamplingRate
                },
            };
        }
    }
    ```

=== "Example CloudWatch Logs excerpt"

    ```json
    {
        "Message": "Test Message",
        "Service": "lambda-example",
        "CorrelationIds": {
            "AwsRequestId": "52fdfc07-2182-154f-163f-5f0f9a621d72",
            "XRayTraceId": "1-61b7add4-66532bb81441e1b060389429",
            "CorrelationId": "correlation_id_value"
        },
        "LambdaFunction": {
            "Name": "test",
            "Arn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
            "MemorySize": 128,
            "Version": "$LATEST",
            "ColdStart": true
        },
        "Level": "Information",
        "Timestamp": "2021-12-13T20:32:22.5774262Z",
        "Logger": {
            "Name": "AWS.Lambda.Powertools.Logging.Logger",
            "SampleRate": 0.7
        }
    }
    ```
