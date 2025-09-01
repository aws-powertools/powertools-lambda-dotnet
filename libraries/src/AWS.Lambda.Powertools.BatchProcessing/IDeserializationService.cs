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

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Service for deserializing record data into strongly-typed objects.
/// </summary>
public interface IDeserializationService
{
    /// <summary>
    /// Deserializes the provided data string into the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="data">The data string to deserialize.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object of type T.</returns>
    /// <exception cref="Exceptions.DeserializationException">Thrown when deserialization fails.</exception>
    T Deserialize<T>(string data, DeserializationOptions options = null);

    /// <summary>
    /// Attempts to deserialize the provided data string into the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="data">The data string to deserialize.</param>
    /// <param name="result">When this method returns, contains the deserialized object if successful, or the default value if unsuccessful.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>true if deserialization was successful; otherwise, false.</returns>
    bool TryDeserialize<T>(string data, out T result, DeserializationOptions options = null);

    /// <summary>
    /// Attempts to deserialize the provided data string into the specified type, capturing any exception that occurs.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="data">The data string to deserialize.</param>
    /// <param name="result">When this method returns, contains the deserialized object if successful, or the default value if unsuccessful.</param>
    /// <param name="exception">When this method returns, contains the exception that occurred during deserialization if unsuccessful, or null if successful.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>true if deserialization was successful; otherwise, false.</returns>
    bool TryDeserialize<T>(string data, out T result, out Exception exception, DeserializationOptions options = null);
}