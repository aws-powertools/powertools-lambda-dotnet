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

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Configuration options for deserialization operations.
/// </summary>
public class DeserializationOptions
{
    /// <summary>
    /// Gets or sets the JsonSerializerContext to use for AOT-compatible deserialization.
    /// When provided, this takes precedence over JsonSerializerOptions.
    /// </summary>
    public JsonSerializerContext JsonSerializerContext { get; set; }

    /// <summary>
    /// Gets or sets the JsonSerializerOptions to use for deserialization.
    /// This is ignored if JsonSerializerContext is provided.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether deserialization errors should be ignored.
    /// When true, failed deserialization attempts will not throw exceptions but will return default values.
    /// Default is false.
    /// </summary>
    [Obsolete("Use ErrorPolicy property instead. This property will be removed in a future version.")]
    public bool IgnoreDeserializationErrors { get; set; } = false;

    /// <summary>
    /// Gets or sets the policy for handling deserialization errors.
    /// Default is FailRecord.
    /// </summary>
    public DeserializationErrorPolicy ErrorPolicy { get; set; } = DeserializationErrorPolicy.FailRecord;

    /// <summary>
    /// Creates a new instance of DeserializationOptions with default settings.
    /// </summary>
    public DeserializationOptions()
    {
    }

    /// <summary>
    /// Creates a new instance of DeserializationOptions with the specified JsonSerializerContext.
    /// </summary>
    /// <param name="jsonSerializerContext">The JsonSerializerContext to use for AOT-compatible deserialization.</param>
    public DeserializationOptions(JsonSerializerContext jsonSerializerContext)
    {
        JsonSerializerContext = jsonSerializerContext;
    }

    /// <summary>
    /// Creates a new instance of DeserializationOptions with the specified JsonSerializerOptions.
    /// </summary>
    /// <param name="jsonSerializerOptions">The JsonSerializerOptions to use for deserialization.</param>
    public DeserializationOptions(JsonSerializerOptions jsonSerializerOptions)
    {
        JsonSerializerOptions = jsonSerializerOptions;
    }
}