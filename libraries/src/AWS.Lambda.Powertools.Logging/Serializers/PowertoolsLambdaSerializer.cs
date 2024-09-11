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
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Logging.Serializers;

#if NET8_0_OR_GREATER
/// <summary>
/// Provides a custom Lambda serializer that combines multiple JsonSerializerContexts.
/// </summary>
public class PowertoolsLambdaSerializer : ILambdaSerializer
{
    /// <summary>
    /// Initializes a new instance of PowertoolsLambdaSerializer.
    /// </summary>
    /// <param name="customerContext">The customer's JsonSerializerContext.</param>
    public PowertoolsLambdaSerializer(JsonSerializerContext customerContext)
    {
        PowertoolsLoggingSerializer.AddSerializerContext(customerContext);
    }

    /// <summary>
    /// Deserializes the input stream to the specified type.
    /// </summary>
    public T Deserialize<T>(Stream requestStream)
    {
        if (!requestStream.CanSeek)
        {
            using var ms = new MemoryStream();
            requestStream.CopyTo(ms);
            ms.Position = 0;
            requestStream = ms;
        }

        var typeInfo = PowertoolsLoggingSerializer.GetTypeInfo(typeof(T));
        if (typeInfo == null)
        {
            throw new InvalidOperationException(
                $"Type {typeof(T)} is not known to the serializer. Ensure it's included in the JsonSerializerContext.");
        }

        return (T)JsonSerializer.Deserialize(requestStream, typeInfo)!;
    }

    /// <summary>
    /// Serializes the specified object and writes the result to the output stream.
    /// </summary>
    public void Serialize<T>(T response, Stream responseStream)
    {
        var typeInfo = PowertoolsLoggingSerializer.GetTypeInfo(typeof(T));
        if (typeInfo == null)
        {
            throw new InvalidOperationException(
                $"Type {typeof(T)} is not known to the serializer. Ensure it's included in the JsonSerializerContext.");
        }

        using var writer = new Utf8JsonWriter(responseStream, new JsonWriterOptions { SkipValidation = true });
        JsonSerializer.Serialize(writer, response, typeInfo);
    }
}

#endif