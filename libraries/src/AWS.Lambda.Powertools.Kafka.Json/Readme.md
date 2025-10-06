# Powertools for AWS Lambda (.NET) - Kafka JSON

A specialized Lambda serializer for handling Kafka events with JSON-formatted data in .NET Lambda functions.

## Features

- **Automatic JSON Deserialization**: Seamlessly converts JSON data from Kafka records into strongly-typed .NET objects
- **Base64 Decoding**: Handles base64-encoded JSON data from Kafka events automatically
- **Type Safety**: Leverages compile-time type checking with .NET classes
- **Flexible Configuration**: Supports custom JSON serialization options and AOT-compatible contexts
- **High Performance**: Optimized JSON processing using System.Text.Json
- **Error Handling**: Provides clear error messages for serialization failures

## Installation

```bash
dotnet add package AWS.Lambda.Powertools.Kafka.Json
```

## Quick Start

### 1. Configure the Serializer

Add the serializer to your Lambda function assembly:

```csharp
[assembly: LambdaSerializer(typeof(PowertoolsKafkaJsonSerializer))]
```

### 2. Define Your Data Model

Create your .NET classes with JSON serialization attributes:

```csharp
public class Customer
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("age")]
    public int Age { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
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
            Customer customer = record.Value; // Automatically deserialized from JSON
            context.Logger.LogInformation($"Processing customer: {customer.Name}, Age: {customer.Age}");
        }
    }
}
```

## Advanced Configuration

### Custom JSON Options

```csharp
[assembly: LambdaSerializer(typeof(PowertoolsKafkaJsonSerializer))]

// In your startup or configuration
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var serializer = new PowertoolsKafkaJsonSerializer(jsonOptions);
```

### AOT-Compatible Serialization

```csharp
[JsonSerializable(typeof(ConsumerRecords<string, Customer>))]
[JsonSerializable(typeof(Customer))]
public partial class MyJsonContext : JsonSerializerContext { }

[assembly: LambdaSerializer(typeof(PowertoolsKafkaJsonSerializer))]

// Configure with AOT context
var serializer = new PowertoolsKafkaJsonSerializer(MyJsonContext.Default);
```

### Complex Object Handling

```csharp
public class Order
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("customer")]
    public Customer Customer { get; set; } = new();
    
    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = new();
    
    [JsonPropertyName("total")]
    public decimal Total { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class Function
{
    public void Handler(ConsumerRecords<string, Order> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            Order order = record.Value;
            context.Logger.LogInformation($"Order {order.Id} from {order.Customer.Name}");
            context.Logger.LogInformation($"Total: ${order.Total:F2}, Items: {order.Items.Count}");
        }
    }
}
```

## Requirements

- **.NET 6.0+**: This library targets .NET 6.0 and later versions
- **System.Text.Json**: Uses the high-performance JSON library from .NET
- **JSON Serializable Types**: Your data classes should be compatible with System.Text.Json
- **AWS Lambda**: Designed specifically for AWS Lambda runtime environments

## JSON Serialization Best Practices

### Property Naming

```csharp
// Use JsonPropertyName for explicit mapping
public class Product
{
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = "";
    
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = "";
}

// Or configure global naming policy
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
};
```

### Handling Nullable Types

```csharp
public class Customer
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }  // Nullable reference type
    
    [JsonPropertyName("age")]
    public int? Age { get; set; }       // Nullable value type
}
```

### Custom Converters

```csharp
public class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}

// Register the converter
var options = new JsonSerializerOptions();
options.Converters.Add(new DateTimeConverter());
```

## Error Handling

The serializer provides detailed error messages for common issues:

```csharp
// JSON parsing errors
JsonException: "The JSON value could not be converted to [Type]. Path: [path] | LineNumber: [line] | BytePositionInLine: [position]."

// Type conversion errors
SerializationException: "Failed to deserialize value data: [specific error details]"
```

## Performance Optimization

### Source Generation (AOT)

```csharp
[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(ConsumerRecords<string, Customer>))]
[JsonSerializable(typeof(ConsumerRecords<string, Order>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppJsonContext : JsonSerializerContext { }
```

### Memory Optimization

```csharp
// Configure for minimal memory allocation
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultBufferSize = 4096,  // Adjust based on typical message size
    MaxDepth = 32              // Prevent deep recursion
};
```

## Compatibility Notes

- **AOT Support**: Full support for Native AOT when using source generation
- **Trimming**: Compatible with IL trimming when properly configured
- **Performance**: Optimized for high-throughput Lambda scenarios
- **Memory Usage**: Efficient memory allocation patterns for serverless environments

## Migration from Newtonsoft.Json

If migrating from Newtonsoft.Json, consider these differences:

```csharp
// Newtonsoft.Json attribute
[JsonProperty("customer_name")]
public string CustomerName { get; set; }

// System.Text.Json equivalent
[JsonPropertyName("customer_name")]
public string CustomerName { get; set; }
```

## Related Packages

- [AWS.Lambda.Powertools.Kafka.Avro](https://www.nuget.org/packages/AWS.Lambda.Powertools.Kafka.Avro/) - Avro serialization
- [AWS.Lambda.Powertools.Kafka.Protobuf](https://www.nuget.org/packages/AWS.Lambda.Powertools.Kafka.Protobuf/) - Protobuf serialization
- [AWS.Lambda.Powertools.Logging](https://www.nuget.org/packages/AWS.Lambda.Powertools.Logging/) - Structured logging
- [AWS.Lambda.Powertools.Tracing](https://www.nuget.org/packages/AWS.Lambda.Powertools.Tracing/) - Distributed tracing

## Documentation

For more detailed documentation and examples, visit the [official documentation](https://docs.aws.amazon.com/powertools/dotnet/).

## License

This library is licensed under the Apache License 2.0.