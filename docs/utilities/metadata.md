---
title: Lambda Metadata
description: Utility
---

<!-- markdownlint-disable MD013 -->
The Lambda Metadata utility provides idiomatic access to the Lambda Metadata Endpoint (LMDS), eliminating boilerplate code for retrieving execution environment metadata like Availability Zone ID.

## Key features

* Retrieve Lambda execution environment metadata (e.g., Availability Zone ID)
* Automatic caching for the sandbox lifetime
* Thread-safe access for concurrent executions
* Async/await support
* Lazy loading on first access
* Native AOT compatible

!!! warning "Migrating to v3"

    If you're upgrading to v3, please review the [Migration Guide v3](../migration-guide-v3.md) for important breaking changes including .NET 8 requirement and AWS SDK v4 migration.

## Installation

Powertools for AWS Lambda (.NET) are available as NuGet packages. You can install the packages from [NuGet Gallery](https://www.nuget.org/packages?q=AWS+Lambda+Powertools*){target="_blank"} or from Visual Studio editor by searching `AWS.Lambda.Powertools*` to see various utilities available.

* [AWS.Lambda.Powertools.Metadata](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Metadata):

    `dotnet add package AWS.Lambda.Powertools.Metadata`

## Getting started

The Lambda Metadata utility provides a simple static client to retrieve metadata about the Lambda execution environment.

### Basic usage

=== "Synchronous"

    ```c# hl_lines="10-11"
    using AWS.Lambda.Powertools.Metadata;

    public class Function
    {
        public string FunctionHandler(object input, ILambdaContext context)
        {
            // Retrieve metadata (cached after first call)
            var metadata = LambdaMetadataClient.Get();
            
            // Access the Availability Zone ID
            var azId = metadata.AvailabilityZoneId;
            
            return $"Running in AZ: {azId}";
        }
    }
    ```

=== "Asynchronous"

    ```c# hl_lines="10-11"
    using AWS.Lambda.Powertools.Metadata;

    public class Function
    {
        public async Task<string> FunctionHandler(object input, ILambdaContext context)
        {
            // Retrieve metadata asynchronously (cached after first call)
            var metadata = await LambdaMetadataClient.GetAsync();
            
            // Access the Availability Zone ID
            var azId = metadata.AvailabilityZoneId;
            
            return $"Running in AZ: {azId}";
        }
    }
    ```

### Eager loading

For optimal performance, you can fetch metadata during cold start by using a static field initializer:

```c# hl_lines="7-8"
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    // Fetch during cold start - metadata is cached for the sandbox lifetime
    private static readonly LambdaMetadata Metadata = LambdaMetadataClient.Get();

    public string FunctionHandler(object input, ILambdaContext context)
    {
        // Use cached metadata - no additional API call
        return $"Running in AZ: {Metadata.AvailabilityZoneId}";
    }
}
```

This pattern ensures the metadata is fetched once during Lambda initialization and reused across all invocations.

## Available metadata

The `LambdaMetadata` class provides the following properties:

| Property              | Type     | Description                                                                 |
|-----------------------|----------|-----------------------------------------------------------------------------|
| `AvailabilityZoneId`  | `string` | The Availability Zone ID where the Lambda function is running (e.g., `use1-az1`) |

## Refreshing metadata

In most cases, you won't need to refresh metadata since it remains constant for the Lambda sandbox lifetime. However, if needed:

=== "Synchronous"

    ```c# hl_lines="5"
    using AWS.Lambda.Powertools.Metadata;

    // Force a refresh of cached metadata
    var metadata = LambdaMetadataClient.Refresh();
    ```

=== "Asynchronous"

    ```c# hl_lines="5"
    using AWS.Lambda.Powertools.Metadata;

    // Force a refresh of cached metadata asynchronously
    var metadata = await LambdaMetadataClient.RefreshAsync();
    ```

## Error handling

The utility throws `LambdaMetadataException` when it cannot retrieve metadata:

```c# hl_lines="9-13"
using AWS.Lambda.Powertools.Metadata;
using AWS.Lambda.Powertools.Metadata.Exceptions;

public class Function
{
    public string FunctionHandler(object input, ILambdaContext context)
    {
        try
        {
            var metadata = LambdaMetadataClient.Get();
            return $"Running in AZ: {metadata.AvailabilityZoneId}";
        }
        catch (LambdaMetadataException ex)
        {
            // Handle error - metadata endpoint unavailable
            Console.WriteLine($"Failed to get metadata: {ex.Message}");
            
            // Check HTTP status code if available
            if (ex.StatusCode.HasValue)
            {
                Console.WriteLine($"HTTP Status: {ex.StatusCode}");
            }
            
            return "Unknown AZ";
        }
    }
}
```

### Common error scenarios

| Scenario                          | Exception Message                                           |
|-----------------------------------|-------------------------------------------------------------|
| Missing metadata token            | `Lambda metadata token not available. Ensure AWS_LAMBDA_METADATA_TOKEN is set.` |
| Missing metadata API endpoint     | `Lambda metadata API endpoint not available. Ensure AWS_LAMBDA_METADATA_API is set.` |
| HTTP error from metadata endpoint | `Metadata request failed with status {code}: {message}`     |
| Deserialization failure           | `Failed to deserialize Lambda metadata response.`           |

## Environment variables

The utility uses the following environment variables (automatically set by the Lambda runtime):

| Environment Variable        | Description                                      |
|-----------------------------|--------------------------------------------------|
| `AWS_LAMBDA_METADATA_API`   | The metadata API endpoint                        |
| `AWS_LAMBDA_METADATA_TOKEN` | Authentication token for the metadata API        |

!!! note
    These environment variables are automatically configured by the Lambda runtime. You don't need to set them manually.

## Thread safety

The `LambdaMetadataClient` is fully thread-safe:

* Uses double-check locking pattern for synchronous access
* Uses `SemaphoreSlim` for asynchronous access
* Cached metadata is stored in a `volatile` field for safe concurrent reads
* Both sync and async methods share the same cache

This means you can safely call `Get()` or `GetAsync()` from multiple concurrent Lambda invocations without race conditions.

## Cancellation support

The async methods support cancellation tokens:

```c# hl_lines="5-6"
using AWS.Lambda.Powertools.Metadata;

public async Task<string> FunctionHandler(object input, ILambdaContext context)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
    var metadata = await LambdaMetadataClient.GetAsync(cts.Token);
    return metadata.AvailabilityZoneId;
}
```

## AOT support

This utility is fully compatible with Native AOT compilation. It uses source-generated JSON serialization to avoid reflection-based deserialization.

No additional configuration is required for AOT support.

## Use cases

### Multi-AZ routing decisions

```c#
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    private static readonly LambdaMetadata Metadata = LambdaMetadataClient.Get();

    public async Task<string> FunctionHandler(OrderRequest request, ILambdaContext context)
    {
        // Route to AZ-local resources for lower latency
        var azId = Metadata.AvailabilityZoneId;
        var endpoint = GetAzLocalEndpoint(azId);
        
        return await ProcessOrder(request, endpoint);
    }
    
    private string GetAzLocalEndpoint(string azId)
    {
        return azId switch
        {
            "use1-az1" => "https://service-az1.internal",
            "use1-az2" => "https://service-az2.internal",
            _ => "https://service.internal"
        };
    }
}
```

### Logging and observability

```c#
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    private static readonly LambdaMetadata Metadata = LambdaMetadataClient.Get();

    public Function()
    {
        // Add AZ ID to all log entries
        Logger.AppendKey("availability_zone_id", Metadata.AvailabilityZoneId);
    }

    [Logging]
    public string FunctionHandler(object input, ILambdaContext context)
    {
        Logger.LogInformation("Processing request");
        return "Success";
    }
}
```
