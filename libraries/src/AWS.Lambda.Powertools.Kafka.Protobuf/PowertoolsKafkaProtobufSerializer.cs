using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;

namespace AWS.Lambda.Powertools.Kafka.Protobuf;

/// <summary>
/// A Lambda serializer for Kafka events that handles Protobuf-formatted data.
/// This serializer automatically deserializes the Protobuf binary format from base64-encoded strings
/// in Kafka records and converts them to strongly-typed objects.
/// </summary>
/// <example>
/// <code>
/// [assembly: LambdaSerializer(typeof(PowertoolsKafkaProtobufSerializer))]
/// 
/// // Your Lambda handler will receive properly deserialized objects
/// public class Function
/// {
///     public void Handler(ConsumerRecords&lt;string, Customer&gt; records, ILambdaContext context)
///     {
///         foreach (var record in records)
///         {
///             Customer customer = record.Value;
///             context.Logger.LogInformation($"Processed customer {customer.Name}");
///         }
///     }
/// }
/// </code>
/// </example>
public class PowertoolsKafkaProtobufSerializer : PowertoolsKafkaSerializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaProtobufSerializer"/> class
    /// with default JSON serialization options.
    /// </summary>
    public PowertoolsKafkaProtobufSerializer() : base()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaProtobufSerializer"/> class
    /// with custom JSON serialization options.
    /// </summary>
    /// <param name="jsonOptions">Custom JSON serializer options to use during deserialization.</param>
    public PowertoolsKafkaProtobufSerializer(JsonSerializerOptions jsonOptions) : base(jsonOptions)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaProtobufSerializer"/> class
    /// with a JSON serializer context for AOT-compatible serialization.
    /// </summary>
    /// <param name="serializerContext">JSON serializer context for AOT compatibility.</param>
    public PowertoolsKafkaProtobufSerializer(JsonSerializerContext serializerContext) : base(serializerContext)
    {
    }
    
    /// <summary>
    /// Deserializes a base64-encoded Protobuf binary value into an object.
    /// </summary>
    /// <param name="base64Value">The base64-encoded Protobuf binary data.</param>
    /// <param name="valueType">The type to deserialize to.</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("Protobuf deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Protobuf deserialization might require types that cannot be statically analyzed.")]
    protected override object DeserializeComplexValue(string base64Value, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type valueType)
    {
        var protobufBytes = Convert.FromBase64String(base64Value);
        return DeserializeProtobufValue(protobufBytes, valueType);
    }

    /// <summary>
    /// Deserializes Protobuf binary data into an object of the specified type.
    /// </summary>
    /// <param name="protobufBytes">The Protobuf binary data.</param>
    /// <param name="messageType">The Protobuf message type to deserialize to.</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("Protobuf deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Protobuf deserialization might require types that cannot be statically analyzed.")]
    private object DeserializeProtobufValue(byte[] protobufBytes, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type messageType)
    {
        // Find the Parser property which is available on all Protobuf generated classes
        var parserProperty = messageType.GetProperty("Parser", 
            BindingFlags.Public | BindingFlags.Static);
        
        if (parserProperty == null)
            throw new InvalidOperationException($"Type {messageType.Name} does not appear to be a Protobuf message type: Parser property not found");
        
        var parser = parserProperty.GetValue(null) as MessageParser;
        if (parser == null)
            throw new InvalidOperationException($"Could not get Parser for Protobuf type {messageType.Name}");
        
        // Use the parser to deserialize the message
        using var stream = new MemoryStream(protobufBytes);
        var message = parser.ParseFrom(stream);
        
        return message;
    }

    /// <summary>
    /// Deserializes complex key types using Protobuf format.
    /// </summary>
    /// <param name="keyBytes">The key bytes to deserialize.</param>
    /// <param name="keyType">The type to deserialize to.</param>
    /// <returns>The deserialized key object.</returns>
    [RequiresDynamicCode("Protobuf and JSON deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Protobuf and JSON deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeComplexKey(byte[] keyBytes, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type keyType)
    {
        try
        {
            // Check if it's a Protobuf message type
            var parserProperty = keyType.GetProperty("Parser", 
                BindingFlags.Public | BindingFlags.Static);
            
            if (parserProperty != null)
            {
                var parser = parserProperty.GetValue(null) as MessageParser;
                if (parser != null)
                {
                    using var stream = new MemoryStream(keyBytes);
                    return parser.ParseFrom(stream);
                }
            }

            // As a fallback, try JSON deserialization
            var jsonStr = Encoding.UTF8.GetString(keyBytes);
            
            if (SerializerContext != null)
            {
                // Try to get type info from context for AOT compatibility
                var typeInfo = SerializerContext.GetTypeInfo(keyType);
                if (typeInfo != null)
                {
                    return JsonSerializer.Deserialize(jsonStr, typeInfo);
                }
            }
            
            // Fallback to regular deserialization
            #pragma warning disable IL2026, IL3050
            return JsonSerializer.Deserialize(jsonStr, keyType, JsonOptions);
            #pragma warning restore IL2026, IL3050
        }
        catch
        {
            // If all deserialization attempts fail, return null
            return null;
        }
    }
}
