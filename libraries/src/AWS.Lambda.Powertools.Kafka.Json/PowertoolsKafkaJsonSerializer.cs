using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Kafka.Json;

/// <summary>
/// A Lambda serializer for Kafka events that handles JSON-formatted data.
/// This serializer automatically deserializes the JSON format from base64-encoded strings
/// in Kafka records and converts them to strongly-typed objects.
/// </summary>
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
    /// Deserializes complex (non-primitive) types using JSON format.
    /// </summary>
    [RequiresDynamicCode("JSON deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("JSON deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeComplexTypeFormat(byte[] data,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type targetType, bool isKey, SchemaMetadata? schemaMetadata = null)
    {
        if (data == null || data.Length == 0)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        var jsonStr = Encoding.UTF8.GetString(data);

        // Try context-based deserialization first
        if (SerializerContext != null)
        {
            var typeInfo = SerializerContext.GetTypeInfo(targetType);
            if (typeInfo != null)
            {
                return JsonSerializer.Deserialize(jsonStr, typeInfo);
            }
        }

        // Fallback to regular deserialization
#pragma warning disable IL2026, IL3050
        return JsonSerializer.Deserialize(jsonStr, targetType, JsonOptions);
#pragma warning restore IL2026, IL3050
    }
}