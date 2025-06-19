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
using System.Text;
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
    /// Deserializes binary data using Avro format or falls back to JSON.
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="targetType">The type to deserialize to.</param>
    /// <param name="isKey">Whether this data represents a key (true) or a value (false).</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("Avro and JSON deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Avro and JSON deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeFormatSpecific(byte[] data, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] 
        Type targetType, bool isKey)
    {
        try
        {
            // Try to get Avro schema for the type
            var schemaField = targetType.GetField("_SCHEMA",
                BindingFlags.Public | BindingFlags.Static);

            if (schemaField != null)
            {
                var schema = schemaField.GetValue(null) as Schema;
                if (schema != null)
                {
                    using var stream = new MemoryStream(data);
                    var decoder = new BinaryDecoder(stream);
                    var reader = new SpecificDatumReader<object>(schema, schema);
                    return reader.Read(null!, decoder);
                }
            }

            // As a fallback, try JSON deserialization
            var jsonStr = Encoding.UTF8.GetString(data);
            
            if (SerializerContext != null)
            {
                // Try to get type info from context for AOT compatibility
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
        catch
        {
            // If all deserialization attempts fail, return null or default
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }
}
