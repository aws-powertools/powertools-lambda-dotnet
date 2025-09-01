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
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Internal;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// JSON-based implementation of IDeserializationService using System.Text.Json.
/// </summary>
public class JsonDeserializationService : IDeserializationService
{
    /// <summary>
    /// Gets the singleton instance of JsonDeserializationService.
    /// </summary>
    public static JsonDeserializationService Instance { get; } = new();

    /// <summary>
    /// Initializes a new instance of the JsonDeserializationService class.
    /// </summary>
    public JsonDeserializationService()
    {
    }

    /// <summary>
    /// Performs deserialization with AOT compatibility validation and fallback behavior.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="data">The JSON data to deserialize.</param>
    /// <param name="options">The deserialization options.</param>
    /// <returns>The deserialized object.</returns>

    private T DeserializeWithFallback<T>(string data, DeserializationOptions options)
    {
        // Check if we're in AOT mode and provide appropriate guidance
        if (AotCompatibilityHelper.IsAotMode())
        {
            throw new AotCompatibilityException(typeof(T), 
                AotCompatibilityHelper.GetAotCompatibilityErrorMessage(typeof(T), false));
        }

        // Use reflection-based deserialization as fallback for non-AOT scenarios
        return JsonSerializer.Deserialize<T>(data, options?.JsonSerializerOptions);
    }

    /// <inheritdoc />
    public T Deserialize<T>(string data, DeserializationOptions options = null)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new DeserializationException("Data cannot be null or empty.", new ArgumentException("Data cannot be null or empty.", nameof(data)));
        }

        try
        {
            if (options?.JsonSerializerContext != null)
            {
                // Validate AOT compatibility when JsonSerializerContext is provided
                AotCompatibilityHelper.ValidateTypeInContext<T>(options.JsonSerializerContext, true);
                return (T)JsonSerializer.Deserialize(data, typeof(T), options.JsonSerializerContext);
            }

            // Use fallback deserialization with AOT validation
            return DeserializeWithFallback<T>(data, options);
        }
        catch (Exception ex) when (ex is JsonException || ex is NotSupportedException || ex is ArgumentException)
        {
            if (options?.IgnoreDeserializationErrors == true || options?.ErrorPolicy == DeserializationErrorPolicy.IgnoreRecord)
            {
                return default(T);
            }

            throw new DeserializationException(data, typeof(T), ex);
        }
        catch (AotCompatibilityException)
        {
            // Re-throw AOT compatibility exceptions without wrapping
            throw;
        }
        catch (AotTypeValidationException)
        {
            // Re-throw AOT type validation exceptions without wrapping
            throw;
        }
    }

    /// <inheritdoc />
    public bool TryDeserialize<T>(string data, out T result, DeserializationOptions options = null)
    {
        return TryDeserialize(data, out result, out _, options);
    }

    /// <inheritdoc />
    public bool TryDeserialize<T>(string data, out T result, out Exception exception, DeserializationOptions options = null)
    {
        result = default(T);
        exception = null;

        if (string.IsNullOrWhiteSpace(data))
        {
            exception = new ArgumentException("Data cannot be null or empty.", nameof(data));
            return false;
        }

        try
        {
            if (options?.JsonSerializerContext != null)
            {
                // Validate AOT compatibility when JsonSerializerContext is provided
                AotCompatibilityHelper.ValidateTypeInContext<T>(options.JsonSerializerContext, true);
                result = (T)JsonSerializer.Deserialize(data, typeof(T), options.JsonSerializerContext);
            }
            else
            {
                // Use fallback deserialization with AOT validation
                result = DeserializeWithFallback<T>(data, options);
            }

            return true;
        }
        catch (Exception ex) when (ex is JsonException || ex is NotSupportedException || ex is ArgumentException || 
                                   ex is AotCompatibilityException || ex is AotTypeValidationException)
        {
            exception = ex;
            return false;
        }
    }
}