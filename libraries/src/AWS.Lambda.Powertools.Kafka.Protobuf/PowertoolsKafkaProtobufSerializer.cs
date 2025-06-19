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
    /// Deserializes binary data using Protobuf format or falls back to JSON.
    /// Handles both standard protobuf serialization and Confluent Schema Registry serialization.
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="targetType">The type to deserialize to.</param>
    /// <param name="isKey">Whether this data represents a key (true) or a value (false).</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("Protobuf and JSON deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode(
        "Protobuf and JSON deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeFormatSpecific(byte[] data,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type targetType, bool isKey)
    {
        try
        {
            // Check if it's a Protobuf message type
            if (typeof(IMessage).IsAssignableFrom(targetType))
            {
                // Get the parser from cache or create a new one
                var parser = GetProtobufParser(targetType);
                if (parser != null)
                {
                    try
                    {
                        // First, try standard protobuf deserialization
                        return parser.ParseFrom(data);
                    }
                    catch
                    {
                        try
                        {
                            // If standard deserialization fails, try message index handling
                            var result = DeserializeWithMessageIndex(data, parser);
                            if (result != null)
                            {
                                return result;
                            }
                        }
                        catch
                        {
                            // Continue to JSON fallback if message index handling fails
                        }
                    }
                }
            }

            // If not a Protobuf message or parser not found, fall back to JSON
            var jsonStr = Encoding.UTF8.GetString(data);

            if (SerializerContext == null) return JsonSerializer.Deserialize(jsonStr, targetType, JsonOptions);

            var typeInfo = SerializerContext.GetTypeInfo(targetType);
            if (typeInfo != null)
            {
                return JsonSerializer.Deserialize(jsonStr, typeInfo);
            }

            return JsonSerializer.Deserialize(jsonStr, targetType, JsonOptions);
        }
        catch (Exception ex)
        {
            // If all deserialization attempts fail, throw with more helpful message
            throw new InvalidOperationException("Unsupported type for Protobuf deserialization: " + targetType.Name + ". "
                                                + "Protobuf deserialization requires a type of com.google.protobuf.Message. "
                                                + "Consider using an alternative Deserializer.", ex);
        }
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
            catch (Exception ex)
            {
                return null!;
            }
        });
    }

    /// <summary>
    /// Deserializes Protobuf data that may include a Confluent Schema Registry message index.
    /// Handles both the simple case (single 0) and complex case (length-prefixed array of indexes).
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="parser">The Protobuf message parser.</param>
    /// <returns>The deserialized Protobuf message or throws an exception if parsing fails.</returns>
    private IMessage DeserializeWithMessageIndex(byte[] data, MessageParser parser)
    {
        using var inputStream = new MemoryStream(data);
        using var codedInput = new CodedInputStream(inputStream);

        try
        {
            // Read the first varint - this could be either a simple 0 or the length of message index array
            var firstValue = codedInput.ReadUInt32();

            if (firstValue == 0)
            {
                // Simple case: Single 0 byte means first message type
                return parser.ParseFrom(codedInput);
            }
            else
            {
                // Complex case: firstValue is the length of the message index array
                // Skip each message index value
                for (int i = 0; i < firstValue; i++)
                {
                    codedInput.ReadUInt32();
                }

                // Now the remaining data should be the actual protobuf message
                return parser.ParseFrom(codedInput);
            }
        }
        catch (Exception ex)
        {
            // If reading message index fails, try another approach with the remaining data
            try
            {
                // Reset stream position and try again with the whole data
                inputStream.Position = 0;
                return parser.ParseFrom(inputStream);
            }
            catch
            {
                // If that also fails, throw the original exception
                throw new InvalidOperationException("Failed to parse protobuf data with or without message index", ex);
            }
        }
    }
}