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

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;

/// <summary>
/// Exception thrown when deserialization of record data fails.
/// </summary>
public class DeserializationException : Exception
{
    /// <summary>
    /// Gets the raw record data that failed to deserialize.
    /// </summary>
    public string RecordData { get; }

    /// <summary>
    /// Gets the target type that the record data was being deserialized to.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Gets the record identifier, if available.
    /// </summary>
    public string RecordId { get; }

    /// <summary>
    /// Initializes a new instance of the DeserializationException class.
    /// </summary>
    /// <param name="recordData">The raw record data that failed to deserialize.</param>
    /// <param name="targetType">The target type that the record data was being deserialized to.</param>
    /// <param name="recordId">The record identifier, if available.</param>
    /// <param name="innerException">The exception that caused the deserialization failure.</param>
    public DeserializationException(string recordData, Type targetType, string recordId, Exception innerException)
        : base($"Failed to deserialize record '{recordId}' to type '{targetType?.Name ?? "Unknown"}'. See inner exception for details.", innerException)
    {
        RecordData = recordData;
        TargetType = targetType;
        RecordId = recordId;
    }

    /// <summary>
    /// Initializes a new instance of the DeserializationException class.
    /// </summary>
    /// <param name="recordData">The raw record data that failed to deserialize.</param>
    /// <param name="targetType">The target type that the record data was being deserialized to.</param>
    /// <param name="innerException">The exception that caused the deserialization failure.</param>
    public DeserializationException(string recordData, Type targetType, Exception innerException)
        : this(recordData, targetType, "Unknown", innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DeserializationException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that caused the deserialization failure.</param>
    public DeserializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DeserializationException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public DeserializationException(string message)
        : base(message)
    {
    }
}