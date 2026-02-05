# AWS.Lambda.Powertools.Metadata

Powertools for AWS Lambda (.NET) - Lambda Metadata package.

This utility provides idiomatic access to the Lambda Metadata Endpoint (LMDS), eliminating boilerplate code for retrieving execution environment metadata like Availability Zone ID.

## Features

- **Automatic caching** for the sandbox lifetime
- **Thread-safe** access for concurrent executions
- **Lazy loading** on first access
- **Native AOT** compatible

## Installation

```bash
dotnet add package AWS.Lambda.Powertools.Metadata
```

## Usage

### Basic Usage

```csharp
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    public string FunctionHandler(object input, ILambdaContext context)
    {
        var metadata = LambdaMetadataClient.Get();
        var azId = metadata.AvailabilityZoneId;
        
        return $"Running in AZ: {azId}";
    }
}
```

### Eager Loading (Recommended for Production)

```csharp
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    // Fetch during cold start for optimal performance
    private static readonly LambdaMetadata Metadata = LambdaMetadataClient.Get();

    public string FunctionHandler(object input, ILambdaContext context)
    {
        return $"Running in AZ: {Metadata.AvailabilityZoneId}";
    }
}
```

## Available Metadata

| Property | Description | Example |
|----------|-------------|---------|
| `AvailabilityZoneId` | The Availability Zone ID where the function is executing | `use1-az1` |

## Environment Variables

The utility reads the following environment variables (automatically set by Lambda):

| Variable | Description |
|----------|-------------|
| `AWS_LAMBDA_METADATA_API` | The metadata API endpoint |
| `AWS_LAMBDA_METADATA_TOKEN` | The authentication token for the metadata API |

## Error Handling

```csharp
try
{
    var metadata = LambdaMetadataClient.Get();
}
catch (LambdaMetadataException ex)
{
    // Handle metadata fetch failure
    Console.WriteLine($"Failed to get metadata: {ex.Message}");
    
    if (ex.StatusCode > 0)
    {
        Console.WriteLine($"HTTP Status: {ex.StatusCode}");
    }
}
```

## License

This library is licensed under the Apache License 2.0.
