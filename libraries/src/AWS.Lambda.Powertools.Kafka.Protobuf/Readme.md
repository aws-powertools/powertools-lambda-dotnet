# Powertools for AWS Lambda (.NET) - Kafka Protobuf

A specialized Lambda serializer for handling Kafka events with Protocol Buffers (Protobuf) formatted data in .NET Lambda functions.

## Features

- **Automatic Protobuf Deserialization**: Seamlessly converts Protobuf binary data from Kafka records into strongly-typed .NET objects
- **Base64 Decoding**: Handles base64-encoded Protobuf data from Kafka events automatically
- **Type Safety**: Leverages compile-time type checking with Protobuf-generated classes
- **Flexible Configuration**: Supports custom JSON serialization options and AOT-compatible contexts
- **Performance Optimized**: Efficient binary serialization format for high-throughput scenarios
- **Error Handling**: Provides clear error messages for serialization failures

## Installation

```bash
dotnet add package AWS.Lambda.Powertools.Kafka.Protobuf
```

## Quick Start

### 1. Configure the Serializer

Add the serializer to your Lambda function assembly:

```csharp
[assembly: LambdaSerializer(typeof(PowertoolsKafkaProtobufSerializer))]
```

### 2. Define Your Protobuf Model

Create your `.proto` file and generate C# classes:

```protobuf
syntax = "proto3";

message Customer {
  string id = 1;
  string name = 2;
  int32 age = 3;
  string email = 4;
}
```

Generated C# class will implement `IMessage`:

```csharp
public partial class Customer : IMessage<Customer>
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
    
    // Generated Protobuf methods...
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
            Customer customer = record.Value; // Automatically deserialized from Protobuf
            context.Logger.LogInformation($"Processing customer: {customer.Name}, Age: {customer.Age}");
        }
    }
}
```

## Advanced Configuration

### Custom JSON Options

```csharp
[assembly: LambdaSerializer(typeof(PowertoolsKafkaProtobufSerializer))]

// In your startup or configuration
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

var serializer = new PowertoolsKafkaProtobufSerializer(jsonOptions);
```

### AOT-Compatible Serialization

```csharp
[JsonSerializable(typeof(ConsumerRecords<string, Customer>))]
public partial class MyJsonContext : JsonSerializerContext { }

[assembly: LambdaSerializer(typeof(PowertoolsKafkaProtobufSerializer))]

// Configure with AOT context
var serializer = new PowertoolsKafkaProtobufSerializer(MyJsonContext.Default);
```

### Complex Message Types

```csharp
// Nested message example
public class Function
{
    public void Handler(ConsumerRecords<string, Order> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            Order order = record.Value;
            context.Logger.LogInformation($"Order {order.Id} from {order.Customer.Name}");
            
            foreach (var item in order.Items)
            {
                context.Logger.LogInformation($"  Item: {item.Name}, Qty: {item.Quantity}");
            }
        }
    }
}
```

## Requirements

- **.NET 6.0+**: This library targets .NET 6.0 and later versions
- **Google.Protobuf**: Requires the Google Protocol Buffers library for .NET
- **Protobuf Compiler**: Use `protoc` to generate C# classes from `.proto` files
- **IMessage Implementation**: Your data classes must implement `IMessage<T>`
- **AWS Lambda**: Designed specifically for AWS Lambda runtime environments

## Protobuf Code Generation

### Using protoc directly

```bash
protoc --csharp_out=. customer.proto
```

### Using MSBuild integration

Add to your `.csproj`:

```xml
<ItemGroup>
  <Protobuf Include="Protos\customer.proto" />
</ItemGroup>
```

## Error Handling

The serializer provides detailed error messages for common issues:

```csharp
// Missing IMessage implementation
InvalidOperationException: "Unsupported type for Protobuf deserialization: MyClass. 
Protobuf deserialization requires a type that implements IMessage<T>."

// Deserialization failures
SerializationException: "Failed to deserialize value data: [specific error details]"
```

## Performance Benefits

Protocol Buffers offer several advantages for high-throughput Lambda functions:

- **Compact Binary Format**: Smaller message sizes compared to JSON
- **Fast Serialization**: Optimized binary encoding/decoding
- **Schema Evolution**: Forward and backward compatibility
- **Strong Typing**: Compile-time validation of message structure

## Schema Evolution

Protobuf supports schema evolution while maintaining compatibility:

```protobuf
// Version 1
message Customer {
  string id = 1;
  string name = 2;
}

// Version 2 - Added optional field
message Customer {
  string id = 1;
  string name = 2;
  int32 age = 3;        // New optional field
  string email = 4;     // Another new field
}
```

## Compatibility Notes

- **Reflection Requirements**: Uses reflection to instantiate Protobuf types, which may impact AOT compilation
- **Trimming**: May require additional configuration for self-contained deployments with trimming enabled
- **Performance**: Optimized for high-throughput scenarios and Lambda execution patterns
- **Schema Registry**: Compatible with Confluent Schema Registry for centralized schema management

## Related Packages

- [AWS.Lambda.Powertools.Logging](https://www.nuget.org/packages/AWS.Lambda.Powertools.Logging/) - Structured logging
- [AWS.Lambda.Powertools.Tracing](https://www.nuget.org/packages/AWS.Lambda.Powertools.Tracing/) - Distributed tracing
- [Google.Protobuf](https://www.nuget.org/packages/Google.Protobuf/) - Protocol Buffers runtime library

## Documentation

For more detailed documentation and examples, visit the [official documentation](https://docs.powertools.aws.dev/lambda/dotnet/).

## License

This library is licensed under the Apache License 2.0.