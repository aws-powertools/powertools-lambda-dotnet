using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avro;
using Avro.IO;
using Avro.Specific;

namespace AWS.Lambda.Powertools.Kafka.Avro;

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
    /// Deserializes complex (non-primitive) types using Avro format.
    /// Requires types to have a public static _SCHEMA field.
    /// </summary>
    /// <remarks>
    /// The key and value types of <see cref="ConsumerRecords{TK,T}"/> are kept automatically under Native AOT.
    /// Apache.Avro finds the classes of nested records by name and creates the collections of array and map fields
    /// with reflection, so records with those need extra configuration; see the Native AOT section of the docs.
    /// </remarks>
    protected override object? DeserializeComplexTypeFormat(byte[] data,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type targetType, bool isKey, SchemaMetadata? schemaMetadata = null)
    {
        var schema = GetAvroSchema(targetType);
        if (schema == null)
        {
            throw new InvalidOperationException(
                $"Unsupported type for Avro deserialization: {targetType.Name}. " +
                "Avro deserialization requires a type with a static _SCHEMA field. " +
                "Consider using an alternative Deserializer.");
        }

        using var stream = new MemoryStream(data);
        var decoder = new BinaryDecoder(stream);
        var reader = new SpecificDatumReader<object>(schema, schema);
        return reader.Read(null!, decoder);
    }

    /// <summary>
    /// Gets the Avro schema for the specified type from its static _SCHEMA field.
    /// </summary>
    private Schema? GetAvroSchema(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type payloadType)
    {
        var schemaField = payloadType.GetField("_SCHEMA", BindingFlags.Public | BindingFlags.Static);
        return schemaField?.GetValue(null) as Schema;
    }
}