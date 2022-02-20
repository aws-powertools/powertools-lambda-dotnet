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
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Class StringEnumConverter.
///     Implements the <see cref="System.Text.Json.Serialization.JsonConverterFactory" />
/// </summary>
/// <seealso cref="System.Text.Json.Serialization.JsonConverterFactory" />
public class StringEnumConverter : JsonConverterFactory
{
    /// <summary>
    ///     The allow integer values
    /// </summary>
    private readonly bool _allowIntegerValues;

    /// <summary>
    ///     The base converter
    /// </summary>
    private readonly JsonStringEnumConverter _baseConverter;

    /// <summary>
    ///     The naming policy
    /// </summary>
    private readonly JsonNamingPolicy _namingPolicy;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StringEnumConverter" /> class.
    /// </summary>
    public StringEnumConverter() : this(null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="StringEnumConverter" /> class.
    /// </summary>
    /// <param name="namingPolicy">The naming policy.</param>
    /// <param name="allowIntegerValues">if set to <c>true</c> [allow integer values].</param>
    private StringEnumConverter(JsonNamingPolicy namingPolicy = null, bool allowIntegerValues = true)
    {
        _namingPolicy = namingPolicy;
        _allowIntegerValues = allowIntegerValues;
        _baseConverter = new JsonStringEnumConverter(namingPolicy, allowIntegerValues);
    }

    /// <summary>
    ///     When overridden in a derived class, determines whether the converter instance can convert the specified object
    ///     type.
    /// </summary>
    /// <param name="typeToConvert">The type of the object to check whether it can be converted by this converter instance.</param>
    /// <returns>
    ///     <see langword="true" /> if the instance can convert the specified object type; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return _baseConverter.CanConvert(typeToConvert);
    }
    
    /// <summary>
    ///     Creates a converter for a specified type.
    /// </summary>
    /// <param name="typeToConvert">The type handled by the converter.</param>
    /// <param name="options">The serialization options to use.</param>
    /// <returns>A converter for which <typeparamref name="T" /> is compatible with <paramref name="typeToConvert" />.</returns>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var query = from field in typeToConvert.GetFields(BindingFlags.Public | BindingFlags.Static)
            let attr = field.GetCustomAttribute<EnumMemberAttribute>()
            where attr != null
            select (field.Name, attr.Value);
        var dictionary = query.ToDictionary(p => p.Item1, p => p.Item2);
        return dictionary.Count > 0
            ? new JsonStringEnumConverter(new DictionaryLookupNamingPolicy(dictionary, _namingPolicy),
                _allowIntegerValues).CreateConverter(typeToConvert, options)
            : _baseConverter.CreateConverter(typeToConvert, options);
    }
}