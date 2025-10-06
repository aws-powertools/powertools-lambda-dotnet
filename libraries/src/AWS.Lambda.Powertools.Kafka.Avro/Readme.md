# Powertools for AWS Lambda (.NET) - Kafka Avro

A specialized Lambda serializer for handling Kafka events with Avro-formatted data in .NET Lambda functions.

## Features

- **Automatic Avro Deserialization**: Seamlessly converts Avro binary data from Kafka records into strongly-typed .NET objects
- **Base64 Decoding**: Handles base64-encoded Avro data from Kafka events automatically
- **Type Safety**: Leverages compile-time type checking with Avro-generated classes
- **Flexible Configuration**: Supports custom JSON serialization options and AOT-compatible contexts
- **Error Handling**: Provides clear error messages for serialization failures

## Installation

```bash
dotnet add package AWS.Lambda.Powertools.Kafka.Avro
```

## Quick Start

### 1. Configure the Serializer

Add the serializer to your Lambda function assembly:

```csharp
[assembly: LambdaSerializer(typeof(PowertoolsKafkaAvroSerializer))]
```

### 2. Define Your Avro Model

Ensure your Avro-generated classes have the required `_SCHEMA` field:

```csharp
public partial class Customer : ISpecificRecord
{
    public static Schema _SCHEMA = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""Customer"",
        ""fields"": [
            {""name"": ""id"", ""type"": ""string""},
            {""name"": ""name"", ""type"": ""string""},
            {""name"": ""age"", ""type"": ""int""}
        ]
    }");
    
    public string Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
```

### 3. Create Your Lambda Handler

```csharp
public class Function
{
    public void Handler(ConsumerRecords<string, Customer> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            Customer customer = record.Value; // Automatically deserialized from Avro
            context.Logger.LogInformation($"Processing customer: {customer.Name}, Age: {customer.Age}");
        }
    }
}
```

## Advanced Configuration

### Custom JSON Options

```csharp
[assembly: LambdaSerializer(typeof(PowertoolsKafkaAvroSerializer))]

// In your startup or configuration
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

var serializer = new PowertoolsKafkaAvroSerializer(jsonOptions);
```

### AOT-Compatible Serialization

```csharp
[JsonSerializable(typeof(ConsumerRecords<string, Customer>))]
public partial class MyJsonContext : JsonSerializerContext { }

[assembly: LambdaSerializer(typeof(PowertoolsKafkaAvroSerializer))]

// Configure with AOT context
var serializer = new PowertoolsKafkaAvroSerializer(MyJsonContext.Default);
```

## Requirements

- **.NET 6.0+**: This library targets .NET 6.0 and later versions
- **Avro.NET**: Requires the Apache Avro library for .NET
- **Avro Schema**: Your data classes must include a public static `_SCHEMA` field
- **AWS Lambda**: Designed specifically for AWS Lambda runtime environments

## Error Handling

The serializer provides detailed error messages for common issues:

```csharp
// Missing _SCHEMA field
InvalidOperationException: "Unsupported type for Avro deserialization: MyClass. 
Avro deserialization requires a type with a static _SCHEMA field."

// Deserialization failures
SerializationException: "Failed to deserialize value data: [specific error details]"
```

## Compatibility Notes

- **Reflection Requirements**: Uses reflection to access Avro schemas, which may impact AOT compilation
- **Trimming**: May require additional configuration for self-contained deployments with trimming enabled
- **Performance**: Optimized for typical Lambda cold start and execution patterns

## Related Packages

- [AWS.Lambda.Powertools.Logging](https://www.nuget.org/packages/AWS.Lambda.Powertools.Logging/) - Structured logging
- [AWS.Lambda.Powertools.Tracing](https://www.nuget.org/packages/AWS.Lambda.Powertools.Tracing/) - Distributed tracing

## Documentation

For more detailed documentation and examples, visit the [official documentation](https://docs.aws.amazon.com/powertools/dotnet/).

## License

This library is licensed under the Apache License 2.0.