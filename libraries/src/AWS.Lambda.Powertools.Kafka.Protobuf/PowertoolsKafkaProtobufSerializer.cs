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
    /// Deserializes binary data using Protobuf format or falls back to JSON.
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="targetType">The type to deserialize to.</param>
    /// <param name="isKey">Whether this data represents a key (true) or a value (false).</param>
    /// <returns>The deserialized object.</returns>
    [RequiresDynamicCode("Protobuf and JSON deserialization might require runtime code generation.")]
    [RequiresUnreferencedCode("Protobuf and JSON deserialization might require types that cannot be statically analyzed.")]
    protected override object? DeserializeFormatSpecific(byte[] data, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        Type targetType, bool isKey)
    {
        try
        {
            // Check if it's a Protobuf message type
            var parserProperty = targetType.GetProperty("Parser", 
                BindingFlags.Public | BindingFlags.Static);
            
            if (parserProperty != null)
            {
                var parser = parserProperty.GetValue(null) as MessageParser;
                if (parser != null)
                {
                    using var stream = new MemoryStream(data);
                    return parser.ParseFrom(stream);
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
