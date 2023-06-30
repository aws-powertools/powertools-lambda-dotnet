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
/// This exception is thrown when the same payload is sent
/// while the previous one was not yet fully stored in the persistence layer (marked as COMPLETED).
/// Implements the <see cref="System.Exception" />
/// </summary>
/// <seealso cref="System.Exception" />
public class IdempotencyAlreadyInProgressException: Exception
{
    /// <summary>
    /// Creates a new IdempotencyAlreadyInProgressException
    /// </summary>
    public IdempotencyAlreadyInProgressException()
    {
    }
    
    /// <inheritdoc />
    public IdempotencyAlreadyInProgressException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public IdempotencyAlreadyInProgressException(string message, Exception innerException) : base(message, innerException)
    {
    }
}