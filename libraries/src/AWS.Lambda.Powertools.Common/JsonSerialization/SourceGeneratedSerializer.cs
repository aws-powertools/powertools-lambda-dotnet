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

namespace AWS.Lambda.Powertools.Common;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

public class SourceGeneratedSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSGContext> : IPowerToolsSerializer where TSGContext : JsonSerializerContext
{
    TSGContext _jsonSerializerContext;

    /// <inheritdoc />
    public void InternalSerialize<T>(Utf8JsonWriter writer, T response, JsonSerializerOptions options)
    {
        try
        {
            if (this._jsonSerializerContext == null)
            {
                var constructor = typeof(TSGContext).GetConstructor(new Type[] { typeof(JsonSerializerOptions) });
                if(constructor == null)
                {
                    throw new ApplicationException($"The serializer {typeof(TSGContext).FullName} is missing a constructor that takes in JsonSerializerOptions object");
                }

                _jsonSerializerContext = constructor.Invoke(new object[] { options }) as TSGContext;   
            }

            var jsonTypeInfo = _jsonSerializerContext.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>;
            if (jsonTypeInfo == null)
            {
                throw new Exception($"No JsonTypeInfo registered in {_jsonSerializerContext.GetType().FullName} for type {typeof(T).FullName}.");
            }
        
            JsonSerializer.Serialize(writer, response, jsonTypeInfo);
        }
        catch (Exception)
        {
            writer.WriteRawValue("{}");
        }
    }

    /// <inheritdoc />
    public string InternalSerializeAsString<T>(T response, JsonSerializerOptions options = null)
    {
        try
        {
            if (this._jsonSerializerContext == null)
            {
                var constructor = typeof(TSGContext).GetConstructor(new Type[] { typeof(JsonSerializerOptions) });
                if(constructor == null)
                {
                    throw new ApplicationException($"The serializer {typeof(TSGContext).FullName} is missing a constructor that takes in JsonSerializerOptions object");
                }

                _jsonSerializerContext = constructor.Invoke(new object[] { options }) as TSGContext;   
            }

            var jsonTypeInfo = _jsonSerializerContext.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>;
        
            if (jsonTypeInfo == null)
            {
                throw new Exception($"No JsonTypeInfo registered in {_jsonSerializerContext.GetType().FullName} for type {typeof(T).FullName}.");
            }
        
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
        
            JsonSerializer.Serialize(writer, response, jsonTypeInfo);
        
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (Exception)
        {
            return "";
        }
    }
}