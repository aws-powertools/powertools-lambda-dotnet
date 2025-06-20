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
    /// Deserializes complex (non-primitive) types using Protobuf format.
    /// Handles different parsing strategies based on schema metadata:
    /// - No schema ID: Pure Protobuf deserialization
    /// - UUID schema ID (16+ chars): Glue format - removes magic uint32
    /// - Short schema ID (≤10 chars): Confluent format - removes message indexes
    /// </summary>
    [RequiresDynamicCode("Protobuf deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Protobuf deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeComplexTypeFormat(byte[] data,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type targetType, bool isKey, SchemaMetadata? schemaMetadata = null)
    {
        if (!typeof(IMessage).IsAssignableFrom(targetType))
        {
            throw new InvalidOperationException(
                $"Unsupported type for Protobuf deserialization: {targetType.Name}. " +
                "Protobuf deserialization requires a type that implements IMessage. " +
                "Consider using an alternative Deserializer.");
        }

        var parser = GetProtobufParser(targetType);
        if (parser == null)
        {
            throw new InvalidOperationException($"Could not find Protobuf parser for type {targetType.Name}");
        }

        return DeserializeByStrategy(data, parser, schemaMetadata);
    }

    /// <summary>
    /// Deserializes protobuf data using the appropriate strategy based on schema metadata.
    /// </summary>
    private IMessage DeserializeByStrategy(byte[] data, MessageParser parser, SchemaMetadata? schemaMetadata)
    {
        var schemaId = schemaMetadata?.SchemaId;
        
        if (string.IsNullOrEmpty(schemaId))
        {
            // Pure protobuf - no preprocessing needed
            return parser.ParseFrom(data);
        }

        if (schemaId.Length > 10)
        {
            // Glue Schema Registry - remove magic uint32
            return DeserializeGlueFormat(data, parser);
        }

        // Confluent Schema Registry - remove message indexes
        return DeserializeConfluentFormat(data, parser);
    }

    /// <summary>
    /// Deserializes Glue Schema Registry format by removing the magic uint32.
    /// </summary>
    private IMessage DeserializeGlueFormat(byte[] data, MessageParser parser)
    {
        using var inputStream = new MemoryStream(data);
        using var codedInput = new CodedInputStream(inputStream);
        
        codedInput.ReadUInt32(); // Skip magic bytes
        return parser.ParseFrom(codedInput);
    }

    /// <summary>
    /// Deserializes Confluent Schema Registry format by removing message indexes.
    /// Based on Java reference implementation.
    /// </summary>
    private IMessage DeserializeConfluentFormat(byte[] data, MessageParser parser)
    {
        using var inputStream = new MemoryStream(data);
        using var codedInput = new CodedInputStream(inputStream);

        /*
            ReadSInt32() behavior:
               ReadSInt32() properly handles signed varint encoding using ZigZag encoding
               ZigZag encoding maps signed integers to unsigned integers: (n << 1) ^ (n >> 31)
               This allows both positive and negative numbers to be efficiently encoded
               The key insight is that Confluent Schema Registry uses signed varint encoding for the message index count, not unsigned length encoding.
               The ByteUtils.readVarint() in Java typically reads signed varints, which corresponds to ReadSInt32() in C# Google.Protobuf.
         */
        
        // Read number of message indexes
        var indexCount = codedInput.ReadSInt32();
        
        // Skip message indexes if any exist
        if (indexCount > 0)
        {
            for (int i = 0; i < indexCount; i++)
            {
                codedInput.ReadSInt32(); // Read and discard each index
            }
        }

        return parser.ParseFrom(codedInput);
    }

    /// <summary>
    /// Gets the Protobuf parser for the specified type.
    /// </summary>
    private MessageParser? GetProtobufParser(Type messageType)
    {
        var parserProperty = messageType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
        return parserProperty?.GetValue(null) as MessageParser;
    }
}