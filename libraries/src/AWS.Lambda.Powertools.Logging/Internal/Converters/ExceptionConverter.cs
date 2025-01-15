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
using AWS.Lambda.Powertools.Logging.Serializers;

namespace AWS.Lambda.Powertools.Logging.Internal.Converters;

/// <summary>
///     Converts an exception to JSON.
/// </summary>
internal class ExceptionConverter : JsonConverter<Exception>
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
    ///     Converter throws NotSupportedException. Deserializing Exception is not allowed.
    /// </summary>
    /// <param name="reader">Reference to the JsonReader</param>
    /// <param name="typeToConvert">The type which should be converted.</param>
    /// <param name="options">The Json serializer options.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserializing Exception is not allowed");
    }

    /// <summary>
    ///     Write the exception value as JSON. 
    /// </summary>
    /// <param name="writer">The unicode JsonWriter.</param>
    /// <param name="value">The exception instance.</param>
    /// <param name="options">The JsonSerializer options.</param>
    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        void WriteException(Utf8JsonWriter w, Exception ex)
        {
            var exceptionType = ex.GetType();
            var properties = ExceptionPropertyExtractor.ExtractProperties(ex);

            if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
                properties = properties.Where(prop => prop.Value != null);

            var props = properties.ToArray();
            if (props.Length == 0)
                return;

            w.WriteStartObject();
            w.WriteString(ApplyPropertyNamingPolicy("Type", options), exceptionType.FullName);

            foreach (var prop in props)
            {
                switch (prop.Value)
                {
                    case IntPtr intPtr:
                        w.WriteNumber(ApplyPropertyNamingPolicy(prop.Key, options), intPtr.ToInt64());
                        break;
                    case UIntPtr uIntPtr:
                        w.WriteNumber(ApplyPropertyNamingPolicy(prop.Key, options), uIntPtr.ToUInt64());
                        break;
                    case Type propType:
                        w.WriteString(ApplyPropertyNamingPolicy(prop.Key, options), propType.FullName);
                        break;
                    case string propString:
                        w.WriteString(ApplyPropertyNamingPolicy(prop.Key, options), propString);
                        break;
                }
            }

            if (ex.InnerException != null)
            {
                w.WritePropertyName(ApplyPropertyNamingPolicy("InnerException", options));
                WriteException(w, ex.InnerException);
            }

            w.WriteEndObject();
        }

        WriteException(writer, value);
    }

    /// <summary>
    ///     Applying the property naming policy to property name
    /// </summary>
    /// <param name="propertyName">The name of the property</param>
    /// <param name="options">The JsonSerializer options.</param>
    /// <returns></returns>
    private static string ApplyPropertyNamingPolicy(string propertyName, JsonSerializerOptions options)
    {
        return !string.IsNullOrWhiteSpace(propertyName) && options?.PropertyNamingPolicy is not null
            ? options.PropertyNamingPolicy.ConvertName(propertyName)
            : propertyName;
    }
}