---
title: Native AOT with Logger
description: Getting started with Logging in Native AOT applications
---

# Getting Started with AWS Lambda Powertools for .NET Logger in Native AOT

This tutorial shows you how to set up an AWS Lambda project using Native AOT compilation with Powertools for .NET Logger, addressing performance, trimming, and deployment considerations.

## Prerequisites

- An AWS account with appropriate permissions
- A code editor (we'll use Visual Studio Code in this tutorial)
- .NET 8 SDK or later
- Docker (required for cross-platform AOT compilation)

## 1. Understanding Native AOT

Native AOT (Ahead-of-Time) compilation converts your .NET application directly to native code during build time rather than compiling to IL (Intermediate Language) code that gets JIT-compiled at runtime. Benefits for AWS Lambda include:

- Faster cold start times (typically 50-70% reduction)
- Lower memory footprint
- No runtime JIT compilation overhead
- No need for the full .NET runtime to be packaged with your Lambda

## 2. Installing Required Tools

First, ensure you have the .NET 8 SDK installed:

```bash
dotnet --version
```

Install the AWS Lambda .NET CLI tools:

```bash
dotnet tool install -g Amazon.Lambda.Tools
dotnet new install Amazon.Lambda.Templates
```

Verify installation:

```bash
dotnet lambda --help
```

## 3. Creating a Native AOT Lambda Project

Create a directory for your project:

```bash
mkdir powertools-aot-logger-demo
cd powertools-aot-logger-demo
```

Create a new Lambda project using the Native AOT template:

```bash
dotnet new lambda.NativeAOT -n PowertoolsAotLoggerDemo
cd PowertoolsAotLoggerDemo
```

## 4. Adding the Powertools Logger Package

Add the AWS.Lambda.Powertools.Logging package:

```bash
cd src/PowertoolsAotLoggerDemo
dotnet add package AWS.Lambda.Powertools.Logging
```

## 5. Implementing the Lambda Function with AOT-compatible Logger

Let's modify the Function.cs file to implement our function with Powertools Logger in an AOT-compatible way:

```csharp
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Logging;

namespace PowertoolsAotLoggerDemo;

public class Function
{
    /// <summary>
    /// The main entry point for the Lambda function. The main function is called once during the Lambda init phase.
    /// It initializes the Lambda runtime client and passes the function handler to it.
    /// </summary>
    private static async Task Main()
    {
        // Configure the serializer
        var serializer = new DefaultLambdaJsonSerializer(options =>
        {
            options.PropertyNameCaseInsensitive = true;
        });

        // Create a runtime client and pass the handler function
        using var handlerWrapper = LambdaBootstrapBuilder.Create()
            .WithSerializer(serializer)
            .WithHandler(FunctionHandler)
            .Build();

        // Start handling Lambda events
        await handlerWrapper.RunAsync();
    }

    /// <summary>
    /// This function handler processes incoming events.
    /// </summary>
    [Logging(LoggerOutputCase = LoggerOutputCase.CamelCase, CorrelationIdPath = "/headers/x-correlation-id")]
    public static async Task<string> FunctionHandler(JsonElement input, ILambdaContext context)
    {
        // Log the incoming event
        Logger.LogInformation("Processing event: {@Event}", input);

        // Extract a name from the event (if provided)
        string name = "World";
        if (input.TryGetProperty("name", out var nameProperty) && nameProperty.ValueKind == JsonValueKind.String)
        {
            name = nameProperty.GetString() ?? "World";
            Logger.LogInformation("Name provided: {Name}", name);
        }
        else
        {
            Logger.LogInformation("Using default name");
        }

        // Create a simple response
        var response = new
        {
            Message = $"Hello, {name}! (from Native AOT)",
            ProcessedAt = DateTime.UtcNow.ToString("o"),
            ExecutionEnvironment = "Native AOT"
        };

        // Log the response
        Logger.LogInformation("Returning response: {@Response}", response);

        // Return the serialized response
        return JsonSerializer.Serialize(response);
    }
}
```

## 6. Updating the Project File for AOT Compatibility


```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Enable AOT compilation -->
    <PublishAot>true</PublishAot>

    <!-- Enable trimming, required for Native AOT -->
    <PublishTrimmed>true</PublishTrimmed>
    
    <!-- Set trimming level: full is most aggressive -->
    <TrimMode>full</TrimMode>
    
    <!-- Prevent warnings from becoming errors -->
    <TrimmerWarningLevel>0</TrimmerWarningLevel>
    
    <!-- If you're encountering trimming issues, enable this for more detailed info -->
    <!-- <TrimmerLogLevel>detailed</TrimmerLogLevel> -->
    
    <!-- These settings optimize for Lambda -->
    <StripSymbols>true</StripSymbols>
    <OptimizationPreference>Size</OptimizationPreference>
    <InvariantGlobalization>true</InvariantGlobalization>
    
    <!-- Assembly attributes needed for Lambda -->
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <AWSProjectType>Lambda</AWSProjectType>
    
    <!-- Native AOT requires executable, not library -->
    <OutputType>Exe</OutputType>
    
    <!-- Avoid the copious logging from the native AOT compiler -->
    <IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>
    <IlcOptimizationPreference>Size</IlcOptimizationPreference>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.RuntimeSupport" Version="1.10.0" />
    <PackageReference Include="Amazon.Lambda.Core" Version="2.2.0" />
    <PackageReference Include="Amazon.Lambda.Serialization.SystemTextJson" Version="2.4.0" />
    <PackageReference Include="AWS.Lambda.Powertools.Logging" Version="1.4.0" />
  </ItemGroup>
</Project>
```

## 8. Cross-Platform Deployment Considerations

Native AOT compilation must target the same OS and architecture as the deployment environment. AWS Lambda runs on Amazon Linux 2023 (AL2023) with x64 architecture.

### Building for AL2023 on Different Platforms

#### Option A: Using the AWS Lambda .NET Tool with Docker

The simplest approach is to use the AWS Lambda .NET tool, which handles the cross-platform compilation:

```bash
dotnet lambda deploy-function --function-name powertools-aot-logger-demo --function-role your-lambda-role-arn
```

This will:
1. Detect your project is using Native AOT
2. Use Docker behind the scenes to compile for Amazon Linux
3. Deploy the resulting function

#### Option B: Using Docker Directly

Alternatively, you can use Docker directly for more control:

##### On macOS/Linux:

```bash
# Create a build container using Amazon's provided image
docker run --rm -v $(pwd):/workspace -w /workspace public.ecr.aws/sam/build-dotnet8:latest-x86_64 \
    bash -c "cd src/PowertoolsAotLoggerDemo && dotnet publish -c Release -r linux-x64 -o publish"

# Deploy using the AWS CLI
cd src/PowertoolsAotLoggerDemo/publish
zip -r function.zip *
aws lambda create-function \
    --function-name powertools-aot-logger-demo \
    --runtime provided.al2023 \
    --handler bootstrap \
    --role arn:aws:iam::123456789012:role/your-lambda-role \
    --zip-file fileb://function.zip
```

##### On Windows:

```powershell
# Create a build container using Amazon's provided image
docker run --rm -v ${PWD}:/workspace -w /workspace public.ecr.aws/sam/build-dotnet8:latest-x86_64 `
    bash -c "cd src/PowertoolsAotLoggerDemo && dotnet publish -c Release -r linux-x64 -o publish"

# Deploy using the AWS CLI
cd src\PowertoolsAotLoggerDemo\publish
Compress-Archive -Path * -DestinationPath function.zip -Force
aws lambda create-function `
    --function-name powertools-aot-logger-demo `
    --runtime provided.al2023 `
    --handler bootstrap `
    --role arn:aws:iam::123456789012:role/your-lambda-role `
    --zip-file fileb://function.zip
```

## 9. Testing the Function

Test your Lambda function using the AWS CLI:

```bash
aws lambda invoke --function-name powertools-aot-logger-demo --payload '{"name":"PowertoolsAOT"}' response.json
cat response.json
```

You should see a response like:

```json
{
  "Message": "Hello, PowertoolsAOT! (from Native AOT)",
  "ProcessedAt": "2023-11-10T10:15:20.1234567Z",
  "ExecutionEnvironment": "Native AOT"
}
```

Check the logs in CloudWatch Logs to see the structured logs created by Powertools Logger.

## 10. Performance Considerations and Best Practices

### Trimming Considerations

Native AOT uses aggressive trimming, which can cause issues with reflection-based code. Here are tips to avoid common problems:

1. **Using DynamicJsonSerializer**: If you're encountering trimming issues with JSON serialization, add a trimming hint:

```csharp
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
public class MyRequestType
{
    // Properties that will be preserved during trimming
}
```

2. **Logging Objects**: When logging objects with structural logging, consider creating simple DTOs instead of complex types:

```csharp
// Instead of logging complex domain objects:
Logger.LogInformation("User: {@user}", complexUserWithCircularReferences);

// Create a simple loggable DTO:
var userInfo = new { Id = user.Id, Name = user.Name, Status = user.Status };
Logger.LogInformation("User: {@userInfo}", userInfo);
```

3. **Handling Reflection**: If you need reflection, explicitly preserve types:

```xml
<ItemGroup>
  <TrimmerRootDescriptor Include="TrimmerRoots.xml" />
</ItemGroup>
```

And in TrimmerRoots.xml:

```xml
<linker>
  <assembly fullname="YourAssembly">
    <type fullname="YourAssembly.TypeToPreserve" preserve="all" />
  </assembly>
</linker>
```

### Lambda Configuration Best Practices

1. **Memory Settings**: Native AOT functions typically need less memory:

```bash
aws lambda update-function-configuration \
    --function-name powertools-aot-logger-demo \
    --memory-size 512
```

2. **Environment Variables**: Set the AWS_LAMBDA_DOTNET_PREJIT environment variable to 0 (it's not needed for AOT):

```bash
aws lambda update-function-configuration \
    --function-name powertools-aot-logger-demo \
    --environment Variables={AWS_LAMBDA_DOTNET_PREJIT=0}
```

3. **ARM64 Support**: For even better performance, consider using ARM64 architecture:

When creating your project:
```bash
dotnet new lambda.NativeAOT -n PowertoolsAotLoggerDemo --architecture arm64
```

Or modify your deployment:
```bash
aws lambda update-function-configuration \
    --function-name powertools-aot-logger-demo \
    --architectures arm64
```

### Monitoring Cold Start Performance

The Powertools Logger automatically logs cold start information. Use CloudWatch Logs Insights to analyze performance:

```
fields @timestamp, coldStart, billedDurationMs, maxMemoryUsedMB
| filter functionName = "powertools-aot-logger-demo"
| sort @timestamp desc
| limit 100
```

## 11. Troubleshooting Common AOT Issues

### Missing Type Metadata

If you see errors about missing metadata, you may need to add more types to your trimmer roots:

```xml
<ItemGroup>
  <TrimmerRootAssembly Include="AWS.Lambda.Powertools.Logging" />
  <TrimmerRootAssembly Include="System.Private.CoreLib" />
  <TrimmerRootAssembly Include="System.Text.Json" />
</ItemGroup>
```

### Build Failures on macOS/Windows

If you're building directly on macOS/Windows without Docker and encountering errors, remember that Native AOT is platform-specific. Always use the cross-platform build options mentioned earlier.

## Summary

In this tutorial, you've learned:

1. How to set up a .NET Native AOT Lambda project with Powertools Logger
2. How to handle trimming concerns and ensure compatibility
3. Cross-platform build and deployment strategies for Amazon Linux 2023
4. Performance optimization techniques specific to AOT lambdas

Native AOT combined with Powertools Logger gives you the best of both worlds: high-performance, low-latency Lambda functions with rich, structured logging capabilities.

!!! tip "Next Steps"
    Explore using the Embedded Metrics Format (EMF) with your Native AOT Lambda functions for enhanced observability, or try implementing Powertools Tracing in your Native AOT functions.
