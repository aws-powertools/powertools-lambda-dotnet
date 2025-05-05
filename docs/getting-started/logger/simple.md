---
title: Simple Logging
description: Getting started with Logging
---

# Getting Started with AWS Lambda Powertools for .NET Logger

This tutorial shows you how to set up a new AWS Lambda project with Powertools for .NET Logger from scratch - covering the installation of required tools through to deployment.

## Prerequisites

- An AWS account with appropriate permissions
- A code editor (we'll use Visual Studio Code in this tutorial)

## 1. Installing .NET SDK

First, let's download and install the .NET SDK. 
You can find the latest version on the [.NET download page](https://dotnet.microsoft.com/download/dotnet).
Make sure to install the latest version of the .NET SDK (8.0 or later).

Verify installation:

```bash
dotnet --version
```

You should see output like `8.0.100` or similar (the version number may vary).

## 2. Installing AWS Lambda Tools for .NET CLI

Install the AWS Lambda .NET CLI tools:

```bash
dotnet tool install -g Amazon.Lambda.Tools
dotnet new install Amazon.Lambda.Templates
```

Verify installation:

```bash
dotnet lambda --help
```

You should see AWS Lambda CLI command help displayed.

## 3. Setting up AWS CLI credentials

Ensure your AWS credentials are configured:

```bash
aws configure
```

Enter your AWS Access Key ID, Secret Access Key, default region, and output format.

## 4. Creating a New Lambda Project

Create a directory for your project:

```bash
mkdir powertools-logger-demo
cd powertools-logger-demo
```

Create a new Lambda project using the AWS Lambda template:

```bash
dotnet new lambda.EmptyFunction --name PowertoolsLoggerDemo
cd PowertoolsLoggerDemo/src/PowertoolsLoggerDemo
```

## 5. Adding the Powertools Logger Package

Add the AWS.Lambda.Powertools.Logging and Amazon.Lambda.APIGatewayEvents packages:

```bash
dotnet add package AWS.Lambda.Powertools.Logging
dotnet add package Amazon.Lambda.APIGatewayEvents
```

## 6. Implementing the Lambda Function with Logger

Let's modify the Function.cs file to implement our function with Powertools Logger:

```csharp
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PowertoolsLoggerDemo
{
    public class Function
    {
        /// <summary>
        /// A simple function that returns a greeting
        /// </summary>
        /// <param name="request">API Gateway request object</param>
        /// <param name="context">Lambda context</param>
        /// <returns>API Gateway response object</returns>
        [Logging(Service = "greeting-service", LogLevel = Microsoft.Extensions.Logging.LogLevel.Information)]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            // you can {@} serialize objects to log them
            Logger.LogInformation("Processing request {@request}", request);
    
            // You can append additional keys to your logs
            Logger.AppendKey("QueryString", request.QueryStringParameters);
    
            // Simulate processing
            string name = "World";
            if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("name"))
            {
                name = request.QueryStringParameters["name"];
                Logger.LogInformation("Custom name provided: {name}", name);
            }
            else
            {
                Logger.LogInformation("Using default name");
            }
    
            // Create response
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = $"Hello, {name}!",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
    
            Logger.LogInformation("Response successfully created");
    
            return response;
        }
    }
}
```

## 7. Configuring the Lambda Project

Let's update the aws-lambda-tools-defaults.json file with specific settings:

```json
{
  "profile": "",
  "region": "",
  "configuration": "Release",
  "function-runtime": "dotnet8",
  "function-memory-size": 256,
  "function-timeout": 30,
  "function-handler": "PowertoolsLoggerDemo::PowertoolsLoggerDemo.Function::FunctionHandler",
  "function-name": "powertools-logger-demo",
  "function-role": "arn:aws:iam::123456789012:role/your_role_here"
}
```

## 8. Understanding Powertools Logger Features

Let's examine some of the key features we've implemented:

### Service Attribute

The `[Logging]` attribute configures the logger for our Lambda function:

```csharp
[Logging(Service = "greeting-service", LogLevel = Microsoft.Extensions.Logging.LogLevel.Information)]
```

This sets:
- The service name that will appear in all logs
- The minimum logging level

### Structured Logging

Powertools Logger supports structured logging with named placeholders:

```csharp
Logger.LogInformation("Processing request {@request}", request);
```

This creates structured logs where `request` becomes a separate field in the JSON log output.

### Additional Context

You can add custom fields to all subsequent logs:

```csharp
Logger.AppendKey("QueryString", request.QueryStringParameters);
```

This adds the QueryString field with the key and value from the QueryStringParameters property.
This can be an object like in the example or a simple value type.

## 9. Building and Deploying the Lambda Function

Build your function:

```bash
dotnet build
```

Deploy the function using the AWS Lambda CLI tools:

```bash
dotnet lambda deploy-function
```

The tool will use the settings from aws-lambda-tools-defaults.json. If prompted, confirm the deployment settings.

## 10. Testing the Function

Test your Lambda function using the AWS CLI:
You should see: `Hello, Powertools!` and the logs in JSON format.

```bash
Payload:
{"statusCode":200,"headers":{"Content-Type":"text/plain"},"body":"Hello, Powertools!","isBase64Encoded":false}

Log Tail:
{"level":"Information","message":"Processing request Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest","timestamp":"2025-04-23T15:16:42.7473327Z","service":"greeting-service","cold_start":true,"function_name":"powertools-logger-demo","function_memory_size":512,"function_arn":"","function_request_id":"93f07a79-6146-4ed2-80d3-c0a06a5739e0","function_version":"$LATEST","xray_trace_id":"1-68090459-2c2aa3377cdaa9476348236a","name":"AWS.Lambda.Powertools.Logging.Logger","request":{"resource":null,"path":null,"http_method":null,"headers":null,"multi_value_headers":null,"query_string_parameters":{"name":"Powertools"},"multi_value_query_string_parameters":null,"path_parameters":null,"stage_variables":null,"request_context":null,"body":null,"is_base64_encoded":false}}
{"level":"Information","message":"Custom name provided: Powertools","timestamp":"2025-04-23T15:16:42.9064561Z","service":"greeting-service","cold_start":true,"function_name":"powertools-logger-demo","function_memory_size":512,"function_arn":"","function_request_id":"93f07a79-6146-4ed2-80d3-c0a06a5739e0","function_version":"$LATEST","xray_trace_id":"1-68090459-2c2aa3377cdaa9476348236a","name":"AWS.Lambda.Powertools.Logging.Logger","query_string":{"name":"Powertools"}}
{"level":"Information","message":"Response successfully created","timestamp":"2025-04-23T15:16:42.9082709Z","service":"greeting-service","cold_start":true,"function_name":"powertools-logger-demo","function_memory_size":512,"function_arn":"","function_request_id":"93f07a79-6146-4ed2-80d3-c0a06a5739e0","function_version":"$LATEST","xray_trace_id":"1-68090459-2c2aa3377cdaa9476348236a","name":"AWS.Lambda.Powertools.Logging.Logger","query_string":{"name":"Powertools"}}
END RequestId: 98e69b78-f544-4928-914f-6c0902ac8678
REPORT RequestId: 98e69b78-f544-4928-914f-6c0902ac8678  Duration: 547.66 ms     Billed Duration: 548 ms Memory Size: 512 MB     Max Memory Used: 81 MB  Init Duration: 278.70 ms 
```

## 11. Checking the Logs

Visit the AWS CloudWatch console to see your structured logs. You'll notice:

- JSON-formatted logs with consistent structure
- Service name "greeting-service" in all logs
- Additional fields like "query_string"
- Cold start information automatically included
- Lambda context information (function name, memory, etc.)

Here's an example of what your logs will look like:

```bash
{
  "level": "Information",
  "message": "Processing request Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest",
  "timestamp": "2025-04-23T15:16:42.7473327Z",
  "service": "greeting-service",
  "cold_start": true,
  "function_name": "powertools-logger-demo",
  "function_memory_size": 512,
  "function_arn": "",
  "function_request_id": "93f07a79-6146-4ed2-80d3-c0a06a5739e0",
  "function_version": "$LATEST",
  "xray_trace_id": "1-68090459-2c2aa3377cdaa9476348236a",
  "name": "AWS.Lambda.Powertools.Logging.Logger",
  "request": {
    "resource": null,
    "path": null,
    "http_method": null,
    "headers": null,
    "multi_value_headers": null,
    "query_string_parameters": {
      "name": "Powertools"
    },
    "multi_value_query_string_parameters": null,
    "path_parameters": null,
    "stage_variables": null,
    "request_context": null,
    "body": null,
    "is_base64_encoded": false
  }
}
{
  "level": "Information",
  "message": "Response successfully created",
  "timestamp": "2025-04-23T15:16:42.9082709Z",
  "service": "greeting-service",
  "cold_start": true,
  "function_name": "powertools-logger-demo",
  "function_memory_size": 512,
  "function_arn": "",
  "function_request_id": "93f07a79-6146-4ed2-80d3-c0a06a5739e0",
  "function_version": "$LATEST",
  "xray_trace_id": "1-68090459-2c2aa3377cdaa9476348236a",
  "name": "AWS.Lambda.Powertools.Logging.Logger",
  "query_string": {
    "name": "Powertools"
  }
}
```

## Advanced Logger Features

### Correlation IDs

Track requests across services by extracting correlation IDs:

```csharp
[Logging(CorrelationIdPath = "/headers/x-correlation-id")]
```

### Customizing Log Output Format

You can change the casing style of the logs:

```csharp
[Logging(LoggerOutputCase = LoggerOutputCase.CamelCase)]
```

Options include `CamelCase`, `PascalCase`, and `SnakeCase` (default).

## Summary

In this tutorial, you've:

1. Installed the .NET SDK and AWS Lambda tools
2. Created a new Lambda project
3. Added and configured Powertools Logger
4. Deployed and tested your function

Powertools for AWS Logger provides structured logging that makes it easier to search, analyze, and monitor your Lambda functions. The key benefits are:

- JSON-formatted logs for better machine readability
- Consistent structure across all logs
- Automatic inclusion of Lambda context information
- Ability to add custom fields for better context
- Integration with AWS CloudWatch for centralized log management

!!! tip "Next Steps"
    Explore more advanced features like custom log formatters, log buffering, and integration with other Powertools utilities like Tracing and Metrics.