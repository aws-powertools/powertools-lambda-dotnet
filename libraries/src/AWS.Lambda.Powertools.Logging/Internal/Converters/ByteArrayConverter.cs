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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Logging.Internal.Converters;

/// <summary>
///     Converts an byte[] to JSON.
/// </summary>
internal class ByteArrayConverter : JsonConverter<byte[]>
{
    /// <summary>
    ///     Converter throws NotSupportedException. Deserializing ByteArray is not allowed.
    /// </summary>
    /// <param name="reader">Reference to the JsonReader</param>
    /// <param name="typeToConvert">The type which should be converted.</param>
    /// <param name="options">The Json serializer options.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return [];
            
        if (reader.TokenType == JsonTokenType.String)
            return Convert.FromBase64String(reader.GetString()!);
            
        throw new JsonException("Expected string value for byte array");
    }

    /// <summary>
    ///     Write the exception value as JSON. 
    /// </summary>
    /// <param name="writer">The unicode JsonWriter.</param>
    /// <param name="value"></param>
    /// <param name="options">The JsonSerializer options.</param>
    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
            
        string base64 = Convert.ToBase64String(value);
        writer.WriteStringValue(base64);
    }
}