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

namespace AWS.Lambda.Powertools.Logging.Serializers;

/// <inheritdoc />
public sealed class PowertoolsSourceGeneratorSerializer<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    TSgContext> : SourceGeneratorLambdaJsonSerializer<TSgContext> where TSgContext : JsonSerializerContext
{
    /// <summary>
    /// Constructs instance of serializer.
    /// </summary>
    public PowertoolsSourceGeneratorSerializer()
        : this(null)
    {
    }

    /// <summary>
    /// Constructs instance of serializer with the option to customize the JsonSerializerOptions after the 
    /// Amazon.Lambda.Serialization.SystemTextJson's default settings have been applied.
    /// </summary>
    /// <param name="customizer"></param>
    public PowertoolsSourceGeneratorSerializer(
        Action<JsonSerializerOptions> customizer)

    {
        var options = CreateDefaultJsonSerializationOptions();
        customizer?.Invoke(options);

        var constructor = typeof(TSgContext).GetConstructor(new Type[] { typeof(JsonSerializerOptions) });
        if (constructor == null)
        {
            throw new ApplicationException(
                $"The serializer {typeof(TSgContext).FullName} is missing a constructor that takes in JsonSerializerOptions object");
        }

        var jsonSerializerContext = constructor.Invoke(new object[] { options }) as TSgContext;
        PowertoolsLoggingSerializer.AddSerializerContext(jsonSerializerContext);
    }
}

#endif