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
/// Exception thrown when AOT (Ahead-of-Time) compilation compatibility requirements are not met.
/// </summary>
public class AotCompatibilityException : Exception
{
    /// <summary>
    /// Gets the type that caused the AOT compatibility issue.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Initializes a new instance of the AotCompatibilityException class.
    /// </summary>
    /// <param name="targetType">The type that caused the AOT compatibility issue.</param>
    /// <param name="message">The error message.</param>
    public AotCompatibilityException(Type targetType, string message)
        : base(message)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// Initializes a new instance of the AotCompatibilityException class.
    /// </summary>
    /// <param name="targetType">The type that caused the AOT compatibility issue.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AotCompatibilityException(Type targetType, string message, Exception innerException)
        : base(message, innerException)
    {
        TargetType = targetType;
    }
}