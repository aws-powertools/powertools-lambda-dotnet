using Avro;
using Avro.IO;
using Avro.Specific;
using System.Reflection;
using System.Text;
using System.Text.Json;

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
    /// Gets the Avro schema for the specified type.
    /// The type must have a public static _SCHEMA field defined.
    /// </summary>
    /// <param name="payloadType">The type to get the Avro schema for.</param>
    /// <returns>The Avro Schema object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no schema is found for the type.</exception>
    private Schema GetAvroSchema(Type payloadType)
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
    protected override object DeserializeValue(string base64Value, Type valueType)
    {
        Schema schema = GetAvroSchema(valueType);
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
        byte[] avroBytes = Convert.FromBase64String(base64Value);
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
    protected override object? DeserializeComplexKey(byte[] keyBytes, Type keyType)
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
