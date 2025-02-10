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

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
/// Exception thrown when LogFormatter returns invalid object or error:
/// </summary>
public class LogFormatException : Exception
{
    /// <summary>
    /// Creates a new LogFormatException
    /// </summary>
    public LogFormatException()
    {
    }

    /// <inheritdoc />
    public LogFormatException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public LogFormatException(string message, Exception innerException) : base(message, innerException)
    {
    }
}