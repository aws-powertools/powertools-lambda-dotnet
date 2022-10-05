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

namespace AWS.Lambda.Powertools.Idempotency.Exceptions;

/// <summary>
/// Exception thrown when Idempotency is not well configured:
///     - An annotated method does not return anything
///     - An annotated method does not use Task as return value
///     - An annotated method does not have parameters
/// </summary>
public class IdempotencyConfigurationException : Exception
{
    /// <summary>
    /// Creates a new IdempotencyConfigurationException
    /// </summary>
    public IdempotencyConfigurationException()
    {
    }

    /// <inheritdoc />
    public IdempotencyConfigurationException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public IdempotencyConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}