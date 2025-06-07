using System.Text;
using System.Text.Json;

namespace AWS.Lambda.Powertools.Kafka;

/// <summary>
/// A Lambda serializer for Kafka events that handles JSON-formatted data.
/// This serializer deserializes JSON data from Kafka records into strongly-typed objects.
/// </summary>
/// <example>
/// <code>
/// [assembly: LambdaSerializer(typeof(PowertoolsKafkaJsonSerializer))]
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
public class PowertoolsKafkaJsonSerializer : PowertoolsKafkaSerializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaJsonSerializer"/> class
    /// with default JSON serialization options.
    /// </summary>
    public PowertoolsKafkaJsonSerializer() : base()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaJsonSerializer"/> class
    /// with custom JSON serialization options.
    /// </summary>
    /// <param name="jsonOptions">Custom JSON serializer options to use during deserialization.</param>
    public PowertoolsKafkaJsonSerializer(JsonSerializerOptions jsonOptions) : base(jsonOptions)
    {
    }
    
    /// <summary>
    /// Deserializes a base64-encoded JSON value into an object.
    /// </summary>
    /// <param name="base64Value">The base64-encoded JSON data.</param>
    /// <param name="valueType">The type to deserialize to.</param>
    /// <returns>The deserialized object.</returns>
    protected override object DeserializeValue(string base64Value, Type valueType)
    {
        byte[] jsonBytes = Convert.FromBase64String(base64Value);
        string jsonString = Encoding.UTF8.GetString(jsonBytes);
        
        var result = JsonSerializer.Deserialize(jsonString, valueType, JsonOptions);
        return result ?? throw new InvalidOperationException($"Failed to deserialize JSON to type {valueType.Name}");
    }

    /// <summary>
    /// Deserializes complex key types from JSON.
    /// </summary>
    /// <param name="keyBytes">The key bytes to deserialize.</param>
    /// <param name="keyType">The type to deserialize to.</param>
    /// <returns>The deserialized key object.</returns>
    protected override object? DeserializeComplexKey(byte[] keyBytes, Type keyType)
    {
        try
        {
            // Convert bytes to JSON string and deserialize
            string jsonStr = Encoding.UTF8.GetString(keyBytes);
            return JsonSerializer.Deserialize(jsonStr, keyType, JsonOptions);
        }
        catch
        {
            // If deserialization fails, return null
            return null;
        }
    }
}
