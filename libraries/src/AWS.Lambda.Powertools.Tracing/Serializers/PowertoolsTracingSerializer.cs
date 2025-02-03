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


#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Tracing.Serializers;

/// <summary>
/// Powertools Tracing Serializer
/// Serializes with client Context
/// </summary>
public static class PowertoolsTracingSerializer
{
    private static JsonSerializerContext _context;
    
    /// <summary>
    /// Adds a JsonSerializerContext for tracing serialization
    /// </summary>
    internal static void AddSerializerContext(JsonSerializerContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Serializes an object using the configured context
    /// </summary>
    public static string Serialize(object value)
    {
        if (_context == null)
        {
            throw new InvalidOperationException("Serializer context not initialized. Ensure WithTracing() is called on the Lambda serializer.");
        }

        // Serialize using the context
        return JsonSerializer.Serialize(value, value.GetType(), _context);
    }
    
    /// <summary>
    /// Serializes an object using the configured context and returns a Dictionary
    /// </summary>
    public static IDictionary<string, object> GetMetadataValue(object value)
    {
        if (_context == null)
        {
            throw new InvalidOperationException("Serializer context not initialized. Ensure WithTracing() is called on the Lambda serializer.");
        }

        // Serialize using the context
        var jsonString = Serialize(value);
        
        // From here bellow it converts the string to a dictionary<string,object>
        // this is required because xray will double serialize if we just pass the string
        // this approach allows to output an object 
        using var document = JsonDocument.Parse(jsonString);
        var result = new Dictionary<string, object>();
        
        foreach (var prop in document.RootElement.EnumerateObject())
        {
            result[prop.Name] = ConvertValue(prop.Value);
        }

        return result;
    }

    private static object ConvertValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertValue(prop.Value);
                }
                return dict;
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            default:
                return null;
        }
    }
}

#endif