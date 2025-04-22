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

First, let's install the .NET SDK:

### macOS

```bash
# Using Homebrew
brew install dotnet

# Alternatively, download from Microsoft's website
# https://dotnet.microsoft.com/download
```

Verify installation:

```bash
dotnet --version
```

You should see output like `8.0.100` or similar (the version number may vary).

## 2. Installing AWS Lambda Tools for .NET CLI

Install the AWS Lambda .NET CLI tools:

```bash
dotnet tool install -g Amazon.Lambda.Tools
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

Enter your:
- AWS Access Key ID
- AWS Secret Access Key
- Default region (e.g., us-east-1)
- Default output format (json)

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

Add the AWS Lambda Powertools Logger package:

```bash
dotnet add package AWS.Lambda.Powertools.Logging
```

## 6. Implementing the Lambda Function with Logger

Let's modify the Function.cs file to implement our function with Powertools Logger:

```csharp
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
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
            Logger.LogInformation("Processing greeting request");

            // Log info about the request
            Logger.LogDebug("Request path: {path}", request.Path);
            
            // You can append additional keys to your logs
            Logger.AppendKey("requestMethod", request.HttpMethod);

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
  "profile": "default",
  "region": "us-east-1",
  "configuration": "Release",
  "function-runtime": "dotnet8",
  "function-memory-size": 256,
  "function-timeout": 30,
  "function-handler": "PowertoolsLoggerDemo::PowertoolsLoggerDemo.Function::FunctionHandler",
  "function-name": "powertools-logger-demo"
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
Logger.LogInformation("Custom name provided: {name}", name);
```

This creates structured logs where `name` becomes a separate field in the JSON log output.

### Additional Context

You can add custom fields to all subsequent logs:

```csharp
Logger.AppendKey("requestMethod", request.HttpMethod);
```

This adds the HTTP method used to all logs during the function's execution.

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

```bash
aws lambda invoke \
  --function-name powertools-logger-demo \
  --payload '{"queryStringParameters": {"name": "Powertools"}}' \
  response.json
```

Check the response:

```bash
cat response.json
```

You should see: `Hello, Powertools!`

## 11. Checking the Logs

Visit the AWS CloudWatch console to see your structured logs. You'll notice:

- JSON-formatted logs with consistent structure
- Service name "greeting-service" in all logs
- Additional fields like "requestMethod"
- Cold start information automatically included
- Lambda context information (function name, memory, etc.)

Here's an example of what your logs will look like:

```json
{
  "level": "Information",
  "message": "Custom name provided: Powertools",
  "timestamp": "2023-04-10T15:32:22.5774262Z",
  "service": "greeting-service",
  "cold_start": true,
  "function_name": "powertools-logger-demo",
  "function_memory_size": 256,
  "function_arn": "arn:aws:lambda:us-east-1:123456789012:function:powertools-logger-demo",
  "function_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72",
  "name": "AWS.Lambda.Powertools.Logging.Logger",
  "request_method": "GET"
}
```

## Advanced Logger Features

### Log Sampling

You can enable sampling of debug logs to reduce verbosity while still collecting detailed logs when needed:

```csharp
[Logging(Service = "greeting-service", SamplingRate = 0.1)]
```

This will elevate 10% of requests to DEBUG level for more detailed logging.

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

Powertools Logger provides structured logging that makes it easier to search, analyze, and monitor your Lambda functions. The key benefits are:

- JSON-formatted logs for better machine readability
- Consistent structure across all logs
- Automatic inclusion of Lambda context information
- Ability to add custom fields for better context
- Integration with AWS CloudWatch for centralized log management

!!! tip "Next Steps"
Explore more advanced features like custom log formatters, log buffering, and integration with other Powertools utilities like Tracing and Metrics.