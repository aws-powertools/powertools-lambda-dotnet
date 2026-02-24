---
title: ASP.NET Core Minimal API Logging
description: Getting started with Logging in ASP.NET Core Minimal APIs
---

# Getting Started with AWS Lambda Powertools for .NET Logger in ASP.NET Core Minimal APIs

This tutorial shows you how to set up an ASP.NET Core Minimal API project with AWS Lambda Powertools for .NET Logger - covering installation of required tools through deployment and advanced logging features.

## Prerequisites

- An AWS account with appropriate permissions
- A code editor (we'll use Visual Studio Code in this tutorial)
- .NET 8 SDK or later

## 1. Installing Required Tools

First, ensure you have the .NET SDK installed. If not, you can download it from the [.NET download page](https://dotnet.microsoft.com/download/dotnet).

```bash
dotnet --version
```

You should see output like `8.0.100` or similar.

Next, install the AWS Lambda .NET CLI tools:

```bash
dotnet tool install -g Amazon.Lambda.Tools
dotnet new install Amazon.Lambda.Templates
```

Verify installation:

```bash
dotnet lambda --help
```

## 2. Setting up AWS CLI credentials

Ensure your AWS credentials are configured:

```bash
aws configure
```

Enter your AWS Access Key ID, Secret Access Key, default region, and output format.

## 3. Creating a New ASP.NET Core Minimal API Lambda Project

Create a directory for your project:

```bash
mkdir powertools-aspnet-logger-demo
cd powertools-aspnet-logger-demo
```

Create a new ASP.NET Minimal API project using the AWS Lambda template:

```bash
dotnet new serverless.AspNetCoreMinimalAPI --name PowertoolsAspNetLoggerDemo
cd PowertoolsAspNetLoggerDemo/src/PowertoolsAspNetLoggerDemo
```

## 4. Adding the Powertools Logger Package

Add the AWS.Lambda.Powertools.Logging package:

```bash
dotnet add package AWS.Lambda.Powertools.Logging
```

## 5. Implementing the Minimal API with Powertools Logger

Let's modify the Program.cs file to implement our Minimal API with Powertools Logger:

```csharp
using Microsoft.Extensions.Logging;
using AWS.Lambda.Powertools.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure AWS Lambda
// This is what connects the Events from API Gateway to the ASP.NET Core pipeline
// In this case we are using HttpApi
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// Add Powertools Logger
var logger = LoggerFactory.Create(builder =>
{
    builder.AddPowertoolsLogger(config =>
    {
        config.Service = "powertools-aspnet-demo";
        config.MinimumLogLevel = LogLevel.Debug;
        config.LoggerOutputCase = LoggerOutputCase.CamelCase;
        config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    });
}).CreatePowertoolsLogger();

var app = builder.Build();

app.MapGet("/", () => {
    logger.LogInformation("Processing root request");
    return "Hello from Powertools ASP.NET Core Minimal API!";
});

app.MapGet("/users/{id}", (string id) => {
    logger.LogInformation("Getting user with ID: {userId}", id);
    
    // Log a structured object
    var user = new User {
        Id = id,
        Name = "John Doe",
        Email = "john.doe@example.com"
    };
    
    logger.LogDebug("User details: {@user}", user);
    
    return Results.Ok(user);
});

app.Run();

// Simple user class for demonstration
public class User
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    
    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}
```

## 6. Understanding the LoggerFactory Setup

Let's examine the key parts of how we've set up the logger:

```csharp
var logger = LoggerFactory.Create(builder =>
{
    builder.AddPowertoolsLogger(config =>
    {
        config.Service = "powertools-aspnet-demo";
        config.MinimumLogLevel = LogLevel.Debug;
        config.LoggerOutputCase = LoggerOutputCase.CamelCase;
        config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    });
}).CreatePowertoolsLogger();
```

This setup:

1. Creates a new `LoggerFactory` instance
2. Adds the Powertools Logger provider to the factory 
3. Configures the logger with:
   - Service name that appears in all logs
   - Minimum logging level set to Information
   - CamelCase output format for JSON properties
4. Creates a Powertools logger instance from the factory

## 7. Building and Deploying the Lambda Function

Build your function:

```bash
dotnet build
```

Deploy the function using the AWS Lambda CLI tools:

We started from a serverless template but we are just going to deploy a Lambda function not an API Gateway.

First update the `aws-lambda-tools-defaults.json` file with your details:

```json
{
  "Information": [
  ],
  "profile": "",
  "region": "",
  "configuration": "Release",
  "function-runtime": "dotnet8",
  "function-memory-size": 512,
  "function-timeout": 30,
  "function-handler": "PowertoolsAspNetLoggerDemo",
  "function-role": "arn:aws:iam::123456789012:role/my-role",
  "function-name": "PowertoolsAspNetLoggerDemo"
}
```
!!! Info "IAM Role"
    Make sure to replace the `function-role` with the ARN of an IAM role that has permissions to write logs to CloudWatch.

!!! Info
    As you can see the function-handler is set to `PowertoolsAspNetLoggerDemo` which is the name of the project.
    This example template uses [Executable assembly handlers](https://docs.aws.amazon.com/lambda/latest/dg/csharp-handler.html#csharp-executable-assembly-handlers) which use the assembly name as the handler.

Then deploy the function:

```bash
dotnet lambda deploy-function
```

Follow the prompts to complete the deployment.

## 8. Testing the Function

Test your Lambda function using the AWS CLI.
The following command simulates an API Gateway payload, more information can be found in the [AWS Lambda documentation](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-develop-integrations-lambda.html).

```bash
dotnet lambda invoke-function PowertoolsAspNetLoggerDemo --payload '{
  "requestContext": {
    "http": {
      "method": "GET",
      "path": "/"
    }
  }
}'
```

You should see a response and the logs in JSON format.

```bash
Payload:
{
  "statusCode": 200,
  "headers": {
    "Content-Type": "text/plain; charset=utf-8"
  },
  "body": "Hello from Powertools ASP.NET Core Minimal API!",
  "isBase64Encoded": false
}

Log Tail:
START RequestId: cf670319-d9c4-4005-aebc-3afd08ae01e0 Version: $LATEST
warn: Amazon.Lambda.AspNetCoreServer.AbstractAspNetCoreFunction[0]
Request does not contain domain name information but is derived from APIGatewayProxyFunction.
{
  "level": "Information",
  "message": "Processing root request",
  "timestamp": "2025-04-23T18:02:54.9014083Z",
  "service": "powertools-aspnet-demo",
  "coldStart": true,
  "xrayTraceId": "1-68092b4e-352be5201ea5b15b23854c44",
  "name": "AWS.Lambda.Powertools.Logging.Logger"
}
END RequestId: cf670319-d9c4-4005-aebc-3afd08ae01e0
```

## 9. Advanced Logging Features

Now that we have basic logging set up, let's explore some advanced features of Powertools Logger.

### Adding Context with AppendKey

You can add custom keys to all subsequent log messages:

```csharp
app.MapGet("/users/{id}", (string id) =>
{
    // Add context to all subsequent logs
    Logger.AppendKey("userId", id);
    Logger.AppendKey("source", "users-api");

    logger.LogInformation("Getting user with ID: {id}", id);

    // Log a structured object
    var user = new User
    {
        Id = id,
        Name = "John Doe",
        Email = "john.doe@example.com"
    };

    logger.LogInformation("User details: {@user}", user);

    return Results.Ok(user);
});
```

This will add `userId` and `source` to all logs generated in this request context.
This will output:

```bash hl_lines="19-20 32-36"
Payload:
{
  "statusCode": 200,
  "headers": {
    "Content-Type": "application/json; charset=utf-8"
  },
  "body": "{\"id\":\"1\",\"name\":\"John Doe\",\"email\":\"john.doe@example.com\"}",
  "isBase64Encoded": false
}
Log Tail:
{
   "level": "Information",
   "message": "Getting user with ID: 1",
   "timestamp": "2025-04-23T18:21:28.5314300Z",
   "service": "powertools-aspnet-demo",
   "coldStart": true,
   "xrayTraceId": "1-68092fa7-64f070f7329650563b7501fe",
   "name": "AWS.Lambda.Powertools.Logging.Logger",
   "userId": "1",
   "source": "users-api"
}
{
  "level": "Information",
  "message": "User details: John Doe (1)",
  "timestamp": "2025-04-23T18:21:28.6491316Z",
  "service": "powertools-aspnet-demo",
  "coldStart": true,
  "xrayTraceId": "1-68092fa7-64f070f7329650563b7501fe",
  "name": "AWS.Lambda.Powertools.Logging.Logger",
  "userId": "1",
  "source": "users-api",
  "user": {                     // User object logged
    "id": "1",
    "name": "John Doe",
    "email": "john.doe@example.com"
  }
}
```

### Customizing Log Output

You can customize the log output format:

```csharp
builder.AddPowertoolsLogger(config =>
{
    config.Service = "powertools-aspnet-demo";
    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;  // Change to snake_case
    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss";  // Custom timestamp format
});
```

### Log Sampling for Debugging

When you need more detailed logs for a percentage of requests:

```csharp
// In your logger factory setup
builder.AddPowertoolsLogger(config =>
{
    config.Service = "powertools-aspnet-demo";
    config.MinimumLogLevel = LogLevel.Information;  // Normal level
    config.SamplingRate = 0.1;  // 10% of requests will log at Debug level
});
```

### Structured Logging

Powertools Logger provides excellent support for structured logging:

```csharp
app.MapPost("/products", (Product product) => {
    logger.LogInformation("Creating new product: {productName}", product.Name);
    
    // Log the entire object with all properties
    logger.LogDebug("Product details: {@product}", product);
    
    // Log the ToString() of the object
    logger.LogDebug("Product details: {product}", product);
    
    return Results.Created($"/products/{product.Id}", product);
});

public class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public override string ToString()
    {
        return $"{Name} ({Id}) - {Category}: {Price:C}";
    }
}
```

### Using Log Buffering

For high-throughput applications, you can buffer lower-level logs and only flush them when needed:

```csharp
var logger = LoggerFactory.Create(builder =>
{
    builder.AddPowertoolsLogger(config =>
    {
        config.Service = "powertools-aspnet-demo";
        config.LogBuffering.Enabled = true;
        config.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
        config.LogBuffering.FlushOnErrorLog = true;
    });
}).CreatePowertoolsLogger();

// Usage example
app.MapGet("/process", () => {
    logger.LogDebug("Debug log 1"); // Buffered
    logger.LogDebug("Debug log 2"); // Buffered
    
    try {
        // Business logic that might fail
        throw new Exception("Something went wrong");
    }
    catch (Exception ex) {
        // This will also flush all buffered logs
        logger.LogError(ex, "An error occurred");
        return Results.Problem("Processing failed");
    }
    
    // Manual flushing option
    // Logger.FlushBuffer();
    
    return Results.Ok("Processed successfully");
});
```

### Correlation IDs

For tracking requests across multiple services:

```csharp
app.Use(async (context, next) => {
    // Extract correlation ID from headers
    if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
    {
        Logger.AppendKey("correlationId", correlationId.ToString());
    }
    
    await next();
});
```

## 10. Best Practices for ASP.NET Minimal API Logging

### Register Logger as a Singleton

For better performance, you can register the Powertools Logger as a singleton:

```csharp
// In Program.cs
builder.Services.AddSingleton<ILogger>(sp => {
    return LoggerFactory.Create(builder =>
    {
        builder.AddPowertoolsLogger(config =>
        {
            config.Service = "powertools-aspnet-demo";
        });
    }).CreatePowertoolsLogger();
});

// Then inject it in your handlers
app.MapGet("/example", (ILogger logger) => {
    logger.LogInformation("Using injected logger");
    return "Example with injected logger";
});
```

## 11. Viewing and Analyzing Logs

After deploying your Lambda function, you can view the logs in AWS CloudWatch Logs. The structured JSON format makes it easy to search and analyze logs.

Here's an example of what your logs will look like:

```json
{
  "level": "Information",
  "message": "Getting user with ID: 123",
  "timestamp": "2023-04-15 14:23:45.123",
  "service": "powertools-aspnet-demo",
  "coldStart": true,
  "functionName": "PowertoolsAspNetLoggerDemo",
  "functionMemorySize": 256,
  "functionArn": "arn:aws:lambda:us-east-1:123456789012:function:PowertoolsAspNetLoggerDemo",
  "functionRequestId": "a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6",
  "userId": "123"
}
```

## Summary

In this tutorial, you've learned:

1. How to set up ASP.NET Core Minimal API with AWS Lambda
2. How to integrate Powertools Logger using the LoggerFactory approach
3. How to configure and customize the logger
4. Advanced logging features like structured logging, correlation IDs, and log buffering
5. Best practices for using the logger in an ASP.NET Core application

Powertools for AWS Lambda Logger provides structured logging that makes it easier to search, analyze, and monitor your Lambda functions, and integrates seamlessly with ASP.NET Core Minimal APIs.

!!! tip "Next Steps"
    Explore integrating Powertools Tracing and Metrics with your ASP.NET Core Minimal API to gain even more observability insights.
