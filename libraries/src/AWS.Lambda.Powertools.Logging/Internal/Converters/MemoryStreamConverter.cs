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
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Logging.Internal.Converters;

/// <summary>
///     Handles converting MemoryStreams to base 64 strings.
/// </summary>
internal class MemoryStreamConverter : JsonConverter<MemoryStream>
{
    /// <summary>
    ///     Converter throws NotSupportedException. Deserializing MemoryStream is not allowed.
    /// </summary>
    /// <param name="reader">Reference to the JsonReader</param>
    /// <param name="typeToConvert">The type which should be converted.</param>
    /// <param name="options">The Json serializer options.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public override MemoryStream Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserializing MemoryStream is not allowed");
    }
    
    /// <summary>
    ///     Write the MemoryStream as a base 64 string.
    /// </summary>
    /// <param name="writer">The unicode JsonWriter.</param>
    /// <param name="value">The MemoryStream instance.</param>
    /// <param name="options">The JsonSerializer options.</param>
    public override void Write(Utf8JsonWriter writer, MemoryStream value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Convert.ToBase64String(value.ToArray()));
    }
}