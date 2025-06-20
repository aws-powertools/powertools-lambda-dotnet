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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
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
    // Cache for Protobuf parsers to improve performance
    private static readonly ConcurrentDictionary<Type, MessageParser> _parserCache = new();

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
    /// - UUID schema ID (16+ chars): Glue format - strips first byte
    /// - 4-character schema ID: Confluent format - strips message field numbers
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="targetType">The type to deserialize to.</param>
    /// <param name="isKey">Whether this data represents a key (true) or a value (false).</param>
    /// <param name="schemaMetadata">Optional schema metadata for the data.</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("Protobuf deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode(
        "Protobuf deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeComplexTypeFormat(byte[] data,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type targetType, bool isKey, SchemaMetadata? schemaMetadata = null)
    {
        try
        {
            // Check if it's a Protobuf message type
            if (typeof(IMessage).IsAssignableFrom(targetType))
            {
                // This is a Protobuf message type - try to get the parser
                var parser = GetProtobufParser(targetType);
                if (parser == null)
                {
                    throw new InvalidOperationException($"Could not find Protobuf parser for type {targetType.Name}");
                }

                // Determine parsing strategy based on schema metadata
                var parsingStrategy = DetermineParsingStrategy(schemaMetadata);

                try
                {
                    return parsingStrategy switch
                    {
                        ProtobufParsingStrategy.Pure => DeserializePureProtobuf(data, parser),
                        ProtobufParsingStrategy.Glue => DeserializeGlueProtobuf(data, parser),
                        ProtobufParsingStrategy.Confluent => DeserializeConfluentProtobuf(data, parser),
                        _ => throw new InvalidOperationException($"Unknown parsing strategy: {parsingStrategy}")
                    };
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to deserialize {targetType.Name} using Protobuf with {parsingStrategy} strategy. " +
                        "The data may not be in a valid Protobuf format.", ex);
                }
            }
            else
            {
                // For non-Protobuf complex types, throw the specific expected exception
                throw new InvalidOperationException(
                    $"Unsupported type for Protobuf deserialization: {targetType.Name}. " +
                    "Protobuf deserialization requires a type of com.google.protobuf.Message. " +
                    "Consider using an alternative Deserializer.");
            }
        }
        catch (Exception ex)
        {
            // Preserve the error message while wrapping in SerializationException for consistent error handling
            throw new System.Runtime.Serialization.SerializationException(
                $"Failed to deserialize {(isKey ? "key" : "value")} data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Determines the parsing strategy based on schema metadata.
    /// </summary>
    /// <param name="schemaMetadata">The schema metadata to analyze.</param>
    /// <returns>The appropriate parsing strategy.</returns>
    private ProtobufParsingStrategy DetermineParsingStrategy(SchemaMetadata? schemaMetadata)
    {
        if (schemaMetadata?.SchemaId == null || string.IsNullOrEmpty(schemaMetadata.SchemaId))
        {
            return ProtobufParsingStrategy.Pure;
        }

        var schemaId = schemaMetadata.SchemaId;

        // Check for UUID format (longer than 10 characters indicates Glue Schema Registry)
        if (schemaId.Length > 10)
        {
            return ProtobufParsingStrategy.Glue;
        }

        // Check for Confluent format (numeric schema ID, typically shorter)
        if (schemaId.Length <= 10)
        {
            return ProtobufParsingStrategy.Confluent;
        }

        // Default to pure protobuf for unknown formats
        return ProtobufParsingStrategy.Pure;
    }

    /// <summary>
    /// Deserializes pure Protobuf data without any preprocessing.
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="parser">The Protobuf message parser.</param>
    /// <returns>The deserialized Protobuf message.</returns>
    private IMessage DeserializePureProtobuf(byte[] data, MessageParser parser)
    {
        return parser.ParseFrom(data);
    }

    /// <summary>
    /// Deserializes Glue format Protobuf data by reading and skipping the magic uint32.
    /// Based on Glue Schema Registry protobuf deserializer implementation.
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="parser">The Protobuf message parser.</param>
    /// <returns>The deserialized Protobuf message.</returns>
    private IMessage DeserializeGlueProtobuf(byte[] data, MessageParser parser)
    {
        using var inputStream = new MemoryStream(data);
        using var codedInput = new CodedInputStream(inputStream);
        
        try
        {
            // Seek one byte forward. Based on Glue Proto deserializer implementation
            codedInput.ReadUInt32();
            
            // Parse the remaining data as protobuf
            return parser.ParseFrom(codedInput);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse Glue protobuf data after removing magic bytes", ex);
        }
    }

    /// <summary>
    /// Deserializes Confluent format Protobuf data by handling message index bytes.
    /// Based on the TypeScript reference implementation for Confluent Schema Registry.
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="parser">The Protobuf message parser.</param>
    /// <returns>The deserialized Protobuf message.</returns>
    private IMessage DeserializeConfluentProtobuf(byte[] data, MessageParser parser)
    {
        try
        {
            if (data.Length < 1)
            {
                throw new InvalidOperationException("Confluent data too short");
            }

            // Try int32 varint reading first (most common)
            try
            {
                return ClipConfluentSchemaRegistryBuffer(data, parser, useSignedVarint: false);
            }
            catch (Exception)
            {
                // If int32 fails, try sint32 varint reading
                try
                {
                    return ClipConfluentSchemaRegistryBuffer(data, parser, useSignedVarint: true);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to parse Confluent protobuf data with both int32 and sint32 varint types: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            // Final fallback to direct parsing
            try
            {
                return parser.ParseFrom(data);
            }
            catch (Exception directEx)
            {
                throw new InvalidOperationException(
                    $"Failed to parse Confluent protobuf data: {ex.Message}. " +
                    $"Direct parsing also failed: {directEx.Message}. " +
                    $"Data (hex): {Convert.ToHexString(data.Take(20).ToArray())}", ex);
            }
        }
    }

    /// <summary>
    /// Clips the Confluent Schema Registry buffer to remove the index bytes.
    /// Based on the Java reference implementation logic.
    /// Uses signed varint encoding (ZigZag) as per Confluent Schema Registry specification.
    /// </summary>
    /// <param name="buffer">The buffer to clip.</param>
    /// <param name="parser">The Protobuf message parser.</param>
    /// <param name="useSignedVarint">Whether to use signed varint (sint32) or unsigned (int32).</param>
    /// <returns>The deserialized Protobuf message.</returns>
    private IMessage ClipConfluentSchemaRegistryBuffer(byte[] buffer, MessageParser parser, bool useSignedVarint)
    {
        using var inputStream = new MemoryStream(buffer);
        using var codedInput = new CodedInputStream(inputStream);

        // Read the first signed varint to get the size (number of message indexes)
        // Confluent uses signed varint encoding (ZigZag) for the message index count
        var size = codedInput.ReadSInt32();
        
        // Only if the size is greater than zero, continue reading varInt
        // https://docs.confluent.io/platform/current/schema-registry/fundamentals/serdes-develop/index.html#wire-format
        if (size > 0)
        {
            for (int i = 0; i < size; i++)
            {
                // Read and discard each message index varint
                // These could be either signed or unsigned depending on the schema
                if (useSignedVarint)
                {
                    codedInput.ReadSInt32();
                }
                else
                {
                    codedInput.ReadUInt32();
                }
            }
        }

        // Parse the remaining data as protobuf
        return parser.ParseFrom(codedInput);
    }

    /// <summary>
    /// Gets a Protobuf parser for the specified type, using a cache for better performance.
    /// </summary>
    /// <param name="messageType">The Protobuf message type.</param>
    /// <returns>A MessageParser for the specified type, or null if not found.</returns>
    private MessageParser? GetProtobufParser(Type messageType)
    {
        return _parserCache.GetOrAdd(messageType, type =>
        {
            try
            {
                var parserProperty = type.GetProperty("Parser",
                    BindingFlags.Public | BindingFlags.Static);

                if (parserProperty == null)
                {
                    return null!;
                }

                var parser = parserProperty.GetValue(null) as MessageParser;
                if (parser == null)
                {
                    return null!;
                }

                return parser;
            }
            catch
            {
                return null!;
            }
        });
    }

    /// <summary>
    /// Enum representing different Protobuf parsing strategies.
    /// </summary>
    private enum ProtobufParsingStrategy
    {
        /// <summary>
        /// Pure Protobuf deserialization without preprocessing.
        /// </summary>
        Pure,

        /// <summary>
        /// Glue format - strips the first byte before deserialization.
        /// </summary>
        Glue,

        /// <summary>
        /// Confluent format - strips message field numbers before deserialization.
        /// </summary>
        Confluent
    }
}