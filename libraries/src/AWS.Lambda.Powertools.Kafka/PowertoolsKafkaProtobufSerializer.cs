using Google.Protobuf;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AWS.Lambda.Powertools.Kafka;

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
    /// Deserializes a base64-encoded Protobuf binary value into an object.
    /// </summary>
    /// <param name="base64Value">The base64-encoded Protobuf binary data.</param>
    /// <param name="valueType">The type to deserialize to.</param>
    /// <returns>The deserialized object.</returns>
    protected override object DeserializeValue(string base64Value, Type valueType)
    {
        byte[] protobufBytes = Convert.FromBase64String(base64Value);
        return DeserializeProtobufValue(protobufBytes, valueType);
    }

    /// <summary>
    /// Deserializes Protobuf binary data into an object of the specified type.
    /// </summary>
    /// <param name="protobufBytes">The Protobuf binary data.</param>
    /// <param name="messageType">The Protobuf message type to deserialize to.</param>
    /// <returns>The deserialized object.</returns>
    private object DeserializeProtobufValue(byte[] protobufBytes, Type messageType)
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
    protected override object? DeserializeComplexKey(byte[] keyBytes, Type keyType)
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
            string jsonStr = Encoding.UTF8.GetString(keyBytes);
            return JsonSerializer.Deserialize(jsonStr, keyType, JsonOptions);
        }
        catch
        {
            // If all deserialization attempts fail, return null
            return null;
        }
    }
}
