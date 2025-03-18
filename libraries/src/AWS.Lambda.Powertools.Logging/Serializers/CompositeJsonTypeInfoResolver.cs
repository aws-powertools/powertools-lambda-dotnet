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
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AWS.Lambda.Powertools.Logging.Serializers
{
    /// <summary>
    /// Combines multiple IJsonTypeInfoResolver instances into one
    /// </summary>
    internal class CompositeJsonTypeInfoResolver : IJsonTypeInfoResolver
    {
        private readonly IJsonTypeInfoResolver[] _resolvers;

        /// <summary>
        /// Creates a new composite resolver from multiple resolvers
        /// </summary>
        /// <param name="resolvers">Array of resolvers to use</param>
        public CompositeJsonTypeInfoResolver(IJsonTypeInfoResolver[] resolvers)
        {
            _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
        }


        /// <summary>
        /// Gets JSON type info by trying each resolver in order (.NET Standard 2.0 version)
        /// </summary>
        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            foreach (var resolver in _resolvers)
            {
                var typeInfo = resolver?.GetTypeInfo(type, options);
                if (typeInfo != null)
                {
                    return typeInfo;
                }
            }

            return null;
        }
    }
}
#endif