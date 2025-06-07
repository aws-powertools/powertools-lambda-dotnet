using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

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
    /// Initializes a new instance of the <see cref="PowertoolsKafkaJsonSerializer"/> class
    /// with a JSON serializer context for AOT-compatible serialization.
    /// </summary>
    /// <param name="serializerContext">JSON serializer context for AOT compatibility.</param>
    public PowertoolsKafkaJsonSerializer(JsonSerializerContext serializerContext) : base(serializerContext)
    {
    }
    
    /// <summary>
    /// Deserializes a base64-encoded JSON value into an object.
    /// </summary>
    /// <param name="base64Value">The base64-encoded JSON data.</param>
    /// <param name="valueType">The type to deserialize to.</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("JSON deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("JSON deserialization might require types that cannot be statically analyzed.")]
    protected override object DeserializeValue(string base64Value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type valueType)
    {
        var jsonBytes = Convert.FromBase64String(base64Value);
        var jsonString = Encoding.UTF8.GetString(jsonBytes);
        
        if (SerializerContext != null)
        {
            // Try to get type info from context for AOT compatibility
            var typeInfo = SerializerContext.GetTypeInfo(valueType);
            if (typeInfo != null)
            {
                var result = JsonSerializer.Deserialize(jsonString, typeInfo);
                return result ?? throw new InvalidOperationException($"Failed to deserialize JSON to type {valueType.Name}");
            }
        }
        
        // Fallback to regular deserialization
        #pragma warning disable IL2026, IL3050
        var fallbackResult = JsonSerializer.Deserialize(jsonString, valueType, JsonOptions);
        #pragma warning restore IL2026, IL3050
        
        return fallbackResult ?? throw new InvalidOperationException($"Failed to deserialize JSON to type {valueType.Name}");
    }

    /// <summary>
    /// Deserializes complex key types from JSON.
    /// </summary>
    /// <param name="keyBytes">The key bytes to deserialize.</param>
    /// <param name="keyType">The type to deserialize to.</param>
    /// <returns>The deserialized key object.</returns>
    [RequiresDynamicCode("JSON deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("JSON deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeComplexKey(byte[] keyBytes, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type keyType)
    {
        try
        {
            // Convert bytes to JSON string
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
            // If deserialization fails, return null
            return null;
        }
    }
}
