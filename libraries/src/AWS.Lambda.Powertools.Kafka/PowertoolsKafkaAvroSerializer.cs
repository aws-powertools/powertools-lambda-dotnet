using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avro;
using Avro.IO;
using Avro.Specific;

namespace AWS.Lambda.Powertools.Kafka;

/// <summary>
/// A Lambda serializer for Kafka events that handles Avro-formatted data.
/// This serializer automatically deserializes the Avro binary format from base64-encoded strings
/// in Kafka records and converts them to strongly-typed objects.
/// </summary>
/// <example>
/// <code>
/// [assembly: LambdaSerializer(typeof(PowertoolsKafkaAvroSerializer))]
/// 
/// // Your Lambda handler will receive properly deserialized objects
/// public class Function
/// {
///     public void Handler(ConsumerRecords&lt;string, Customer&gt; records, ILambdaContext context)
///     {
///         foreach (var record in records)
///         {
///             Customer customer = record.Value;
///             context.Logger.LogInformation($"Processed customer {customer.Name}, age {customer.Age}");
///         }
///     }
/// }
/// </code>
/// </example>
public class PowertoolsKafkaAvroSerializer : PowertoolsKafkaSerializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaAvroSerializer"/> class
    /// with default JSON serialization options.
    /// </summary>
    public PowertoolsKafkaAvroSerializer() : base()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaAvroSerializer"/> class
    /// with custom JSON serialization options.
    /// </summary>
    /// <param name="jsonOptions">Custom JSON serializer options to use during deserialization.</param>
    public PowertoolsKafkaAvroSerializer(JsonSerializerOptions jsonOptions) : base(jsonOptions)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PowertoolsKafkaAvroSerializer"/> class
    /// with a JSON serializer context for AOT-compatible serialization.
    /// </summary>
    /// <param name="serializerContext">JSON serializer context for AOT compatibility.</param>
    public PowertoolsKafkaAvroSerializer(JsonSerializerContext serializerContext) : base(serializerContext)
    {
    }
    
    /// <summary>
    /// Gets the Avro schema for the specified type.
    /// The type must have a public static _SCHEMA field defined.
    /// </summary>
    /// <param name="payloadType">The type to get the Avro schema for.</param>
    /// <returns>The Avro Schema object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no schema is found for the type.</exception>
    [RequiresDynamicCode("Avro schema access requires reflection which may be incompatible with AOT.")]
    [RequiresUnreferencedCode("Avro schema access requires reflection which may be incompatible with trimming.")]
    private Schema GetAvroSchema([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type payloadType)
    {
        var schemaField = payloadType.GetField("_SCHEMA",
            BindingFlags.Public | BindingFlags.Static);

        if (schemaField == null)
            throw new InvalidOperationException($"No Avro schema found for type {payloadType.Name}");

        var schema = schemaField.GetValue(null) as Schema;
        if (schema == null)
            throw new InvalidOperationException($"Avro schema for type {payloadType.Name} is null");

        return schema;
    }

    /// <summary>
    /// Deserializes a base64-encoded Avro binary value into an object.
    /// </summary>
    /// <param name="base64Value">The base64-encoded Avro binary data.</param>
    /// <param name="valueType">The type to deserialize to.</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("Avro deserialization requires reflection which may be incompatible with AOT.")]
    [RequiresUnreferencedCode("Avro deserialization requires reflection which may be incompatible with trimming.")]
    protected override object DeserializeValue(string base64Value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type valueType)
    {
        var schema = GetAvroSchema(valueType);
        return DeserializeAvroValue(base64Value, schema);
    }

    /// <summary>
    /// Deserializes a base64-encoded Avro binary value into an object using the provided schema.
    /// </summary>
    /// <param name="base64Value">The base64-encoded Avro binary data.</param>
    /// <param name="schema">The Avro schema to use for deserialization.</param>
    /// <returns>The deserialized object.</returns>
    private object DeserializeAvroValue(string base64Value, Schema schema)
    {
        var avroBytes = Convert.FromBase64String(base64Value);
        using var stream = new MemoryStream(avroBytes);
        var decoder = new BinaryDecoder(stream);
        var reader = new SpecificDatumReader<object>(schema, schema);
        var result = reader.Read(null!, decoder);
        return result ?? throw new InvalidOperationException("Failed to deserialize Avro value");
    }

    /// <summary>
    /// Deserializes complex key types using Avro format.
    /// </summary>
    /// <param name="keyBytes">The key bytes to deserialize.</param>
    /// <param name="keyType">The type to deserialize to.</param>
    /// <returns>The deserialized key object.</returns>
    [RequiresDynamicCode("Avro and JSON deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Avro and JSON deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeComplexKey(byte[] keyBytes, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type keyType)
    {
        try
        {
            // Try to get Avro schema for the key type
            var schemaField = keyType.GetField("_SCHEMA",
                BindingFlags.Public | BindingFlags.Static);

            if (schemaField != null)
            {
                var schema = schemaField.GetValue(null) as Schema;
                if (schema != null)
                {
                    using var stream = new MemoryStream(keyBytes);
                    var decoder = new BinaryDecoder(stream);
                    var reader = new SpecificDatumReader<object>(schema, schema);
                    return reader.Read(null!, decoder);
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
