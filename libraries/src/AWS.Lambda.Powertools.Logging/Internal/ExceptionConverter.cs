/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Converts an exception to JSON.
/// </summary>
public class ExceptionConverter: JsonConverter<Exception>
{
    /// <summary>
    ///     Determines whether the type can be converted.
    /// </summary>
    /// <param name="typeToConvert">The type which should be converted.</param>
    /// <returns>True if the type can be converted, False otherwise.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Exception).IsAssignableFrom(typeToConvert);
    }

    /// <summary>
    ///     Converter throws NotSupportedException. Deserializing exception is not allowed.
    /// </summary>
    /// <param name="reader">Reference to the JsonReader</param>
    /// <param name="typeToConvert">The type which should be converted.</param>
    /// <param name="options">The Json serializer options.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserializing exception is not allowed");
    }

    /// <summary>
    ///     Write the exception value as JSON. 
    /// </summary>
    /// <param name="writer">The unicode JsonWriter.</param>
    /// <param name="value">The exception instance.</param>
    /// <param name="options">The Json serializer options.</param>
    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        if (options.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
            return;

        var exceptionType = value.GetType();
        var serializableProperties = exceptionType
            .GetProperties()
            .Where(prop => prop.Name != nameof(Exception.TargetSite))
            .Select(prop => new { prop.Name, Value = prop.GetValue(value) });

        if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
            serializableProperties = serializableProperties.Where(prop => prop.Value != null);

        var propList = serializableProperties.ToArray();
        if (!propList.Any())
            return;

        writer.WriteStartObject();
        writer.WriteString("Type", exceptionType.FullName);

        foreach (var prop in propList)
        {
            switch (prop.Value)
            {
                case IntPtr intPtr:
                    writer.WriteNumber(prop.Name, intPtr.ToInt64());
                    break;
                case UIntPtr uIntPtr:
                    writer.WriteNumber(prop.Name, uIntPtr.ToUInt64());
                    break;
                case Type propType:
                    writer.WriteString(prop.Name, propType.FullName);
                    break;
                default:
                    writer.WritePropertyName(prop.Name);
                    JsonSerializer.Serialize(writer, prop.Value, options);
                    break;
            }
        }

        writer.WriteEndObject();
    }
}