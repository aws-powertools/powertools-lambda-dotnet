# AWS.Lambda.Powertools.Metadata

Powertools for AWS Lambda (.NET) - Lambda Metadata utility.

Provides access to Lambda execution environment metadata from the Lambda Metadata Endpoint (LMDS).

## Installation

```bash
dotnet add package AWS.Lambda.Powertools.Metadata
```

## Usage

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

## Available Metadata

| Property | Description | Example |
|----------|-------------|---------|
| `AvailabilityZoneId` | The AZ where the function is executing | `use1-az1` |

## Error Handling

```csharp
try
{
    var azId = LambdaMetadata.AvailabilityZoneId;
}
catch (LambdaMetadataException ex)
{
    Console.WriteLine($"Failed: {ex.Message}");
}
```

## License

Apache License 2.0
