/*
 * Copyright JsonCons.Net authors. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

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
    /// Gets the Avro schema for the specified type.
    /// The type must have a public static _SCHEMA field defined.
    /// </summary>
    /// <param name="payloadType">The type to get the Avro schema for.</param>
    /// <returns>The Avro Schema object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no schema is found for the type.</exception>
    [RequiresDynamicCode("Avro schema access requires reflection which may be incompatible with AOT.")]
    [RequiresUnreferencedCode("Avro schema access requires reflection which may be incompatible with trimming.")]
    private Schema? GetAvroSchema([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type payloadType)
    {
        var schemaField = payloadType.GetField("_SCHEMA",
            BindingFlags.Public | BindingFlags.Static);

        if (schemaField == null)
            return null;

        return schemaField.GetValue(null) as Schema;
    }

    /// <summary>
    /// Deserializes complex (non-primitive) types using Avro format.
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="targetType">The type to deserialize to.</param>
    /// <param name="isKey">Whether this data represents a key (true) or a value (false).</param>
    /// <param name="schemaMetadata">Optional schema metadata for the data.</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("Avro deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Avro deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeComplexTypeFormat(byte[] data, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] 
        Type targetType, bool isKey, SchemaMetadata? schemaMetadata = null)
    {
        try
        {
            // Try to get Avro schema for the type
            var schema = GetAvroSchema(targetType);

            if (schema != null)
            {
                using var stream = new MemoryStream(data);
                var decoder = new BinaryDecoder(stream);
                var reader = new SpecificDatumReader<object>(schema, schema);
                return reader.Read(null!, decoder);
            }
            
            // If no Avro schema was found, throw an exception
            throw new InvalidOperationException($"Unsupported type for Avro deserialization: {targetType.Name}. " +
                                               "Avro deserialization requires a type with a static _SCHEMA field. " +
                                               "Consider using an alternative Deserializer.");
        }
        catch (Exception ex)
        {
            // Preserve the error message while wrapping in SerializationException for consistent error handling
            throw new System.Runtime.Serialization.SerializationException($"Failed to deserialize {(isKey ? "key" : "value")} data: {ex.Message}", ex);
        }
    }
}
