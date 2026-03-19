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

namespace AWS.Lambda.Powertools.Metadata.Exceptions;

/// <summary>
/// Exception thrown when the Lambda Metadata Endpoint is unavailable or returns an error.
/// <para>
/// This exception may be thrown when:
/// <list type="bullet">
///   <item><description>The metadata endpoint environment variables are not set</description></item>
///   <item><description>The metadata endpoint returns a non-200 status code</description></item>
///   <item><description>Network errors occur when connecting to the endpoint</description></item>
///   <item><description>The response cannot be parsed</description></item>
/// </list>
/// </para>
/// </summary>
public class LambdaMetadataException : Exception
{
    /// <summary>
    /// Gets the HTTP status code from the metadata endpoint.
    /// Returns -1 if not applicable.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Constructs a new exception with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public LambdaMetadataException(string message) : base(message)
    {
        StatusCode = -1;
    }

    /// <summary>
    /// Constructs a new exception with the specified message and cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying cause.</param>
    public LambdaMetadataException(string message, Exception innerException) : base(message, innerException)
    {
        StatusCode = -1;
    }

    /// <summary>
    /// Constructs a new exception with the specified message and HTTP status code.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code from the metadata endpoint.</param>
    public LambdaMetadataException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
