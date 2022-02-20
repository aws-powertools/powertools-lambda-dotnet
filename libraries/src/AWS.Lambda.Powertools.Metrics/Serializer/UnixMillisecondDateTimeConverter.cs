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

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Class UnixMillisecondDateTimeConverter.
///     Implements the <see cref="System.Text.Json.Serialization.JsonConverter" />
/// </summary>
/// <seealso cref="System.Text.Json.Serialization.JsonConverter" />
public class UnixMillisecondDateTimeConverter : JsonConverter<DateTime>
{
    /// <summary>
    ///     Writes a specified value as JSON.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <exception cref="System.Text.Json.JsonException">Invalid date</exception>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var ms = (long) (value.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds;

        if (ms < 0) throw new JsonException("Invalid date");

        writer.WriteNumberValue(ms);
    }

    /// <summary>
    ///     Reads and converts the JSON to DateTime />.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}