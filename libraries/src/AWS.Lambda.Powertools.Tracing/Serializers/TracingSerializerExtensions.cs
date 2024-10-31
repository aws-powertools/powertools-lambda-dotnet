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
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace AWS.Lambda.Powertools.Tracing.Serializers;

/// <summary>
/// Extensions for SourceGeneratorLambdaJsonSerializer to add tracing support
/// </summary>
public static class TracingSerializerExtensions
{
    // Internal helper to access protected methods
    private sealed class DefaultOptionsHelper<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> 
        : SourceGeneratorLambdaJsonSerializer<T> 
        where T : JsonSerializerContext
    {
        internal static JsonSerializerOptions GetDefaultOptions()
        {
            var helper = new DefaultOptionsHelper<T>();
            return helper.CreateDefaultJsonSerializationOptions();
        }
    }

    /// <summary>
    /// Adds tracing serialization support to the Lambda serializer
    /// </summary>
    public static SourceGeneratorLambdaJsonSerializer<T> WithTracing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this SourceGeneratorLambdaJsonSerializer<T> serializer) 
        where T : JsonSerializerContext
    {
        var options = DefaultOptionsHelper<T>.GetDefaultOptions();
        
        var constructor = typeof(T).GetConstructor(new Type[] { typeof(JsonSerializerOptions) });
        if (constructor == null)
        {
            throw new JsonSerializerException(
                $"The serializer {typeof(T).FullName} is missing a constructor that takes in JsonSerializerOptions object");
        }

        var jsonSerializerContext = constructor.Invoke(new object[] { options }) as T;
        PowertoolsTracingSerializer.AddSerializerContext(jsonSerializerContext);
        
        return serializer;
    }
}

#endif