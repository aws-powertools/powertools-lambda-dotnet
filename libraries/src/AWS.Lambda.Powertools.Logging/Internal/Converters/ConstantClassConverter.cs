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
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Logging.Internal.Converters;

/// <summary>
///     JsonConvert to handle the AWS SDK for .NET custom enum classes that derive from the class called ConstantClass.
/// </summary>
public class ConstantClassConverter : JsonConverter<object>
{
    private static readonly HashSet<string> ConstantClassNames = new()
    {
        "Amazon.S3.EventType",
        "Amazon.DynamoDBv2.OperationType",
        "Amazon.DynamoDBv2.StreamViewType"
    };

    /// <summary>
    ///     Check to see if the type is derived from ConstantClass.
    /// </summary>
    /// <param name="typeToConvert">The type which should be converted.</param>
    /// <returns>True if the type is derived from ConstantClass, False otherwise.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return ConstantClassNames.Contains(typeToConvert.FullName);
    }

    /// <summary>
    ///     Converter throws NotSupportedException. Deserializing ConstantClass is not allowed.
    /// </summary>
    /// <param name="reader">Reference to the JsonReader</param>
    /// <param name="typeToConvert">The type which should be converted.</param>
    /// <param name="options">The Json serializer options.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserializing ConstantClass is not allowed");
    }
    
    /// <summary>
    ///     Write the ConstantClass instance as JSON. 
    /// </summary>
    /// <param name="writer">The unicode JsonWriter.</param>
    /// <param name="value">The exception instance.</param>
    /// <param name="options">The JsonSerializer options.</param>
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}