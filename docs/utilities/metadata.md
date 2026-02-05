---
title: Lambda Metadata
description: Utility
---

<!-- markdownlint-disable MD013 -->
The Lambda Metadata utility provides access to the Lambda Metadata Endpoint (LMDS), giving you execution environment metadata like Availability Zone ID.

## Key features

* Retrieve Lambda execution environment metadata
* Automatic caching for the sandbox lifetime
* Thread-safe access
* Native AOT compatible

## Installation

```bash
dotnet add package AWS.Lambda.Powertools.Metadata
```

## Getting started

```csharp
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    public string Handler(object input, ILambdaContext context)
    {
        var azId = LambdaMetadata.AvailabilityZoneId;
        return $"Running in AZ: {azId}";
    }
}
```

## Available metadata

| Property              | Type     | Description                                              |
|-----------------------|----------|----------------------------------------------------------|
| `AvailabilityZoneId`  | `string` | The AZ where the function is running (e.g., `use1-az1`) |

## Error handling

```csharp
using AWS.Lambda.Powertools.Metadata;
using AWS.Lambda.Powertools.Metadata.Exceptions;

try
{
    var azId = LambdaMetadata.AvailabilityZoneId;
}
catch (LambdaMetadataException ex)
{
    Console.WriteLine($"Failed to get metadata: {ex.Message}");
    
    if (ex.StatusCode.HasValue)
        Console.WriteLine($"HTTP Status: {ex.StatusCode}");
}
```

## Refreshing metadata

Metadata remains constant for the Lambda sandbox lifetime. If you need to force a refresh:

```csharp
LambdaMetadata.Refresh();
```

## Thread safety

`LambdaMetadata.AvailabilityZoneId` is thread-safe. You can access it from multiple concurrent invocations without race conditions.

## Use cases

### Multi-AZ routing

```csharp
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    public async Task<string> Handler(OrderRequest request, ILambdaContext context)
    {
        var endpoint = LambdaMetadata.AvailabilityZoneId switch
        {
            "use1-az1" => "https://service-az1.internal",
            "use1-az2" => "https://service-az2.internal",
            _ => "https://service.internal"
        };
        
        return await ProcessOrder(request, endpoint);
    }
}
```

### Logging

```csharp
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    public Function()
    {
        Logger.AppendKey("az_id", LambdaMetadata.AvailabilityZoneId);
    }

    [Logging]
    public string Handler(object input, ILambdaContext context)
    {
        Logger.LogInformation("Processing request");
        return "Success";
    }
}
```
