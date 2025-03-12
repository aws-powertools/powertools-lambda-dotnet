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

namespace AWS.Lambda.Powertools.Common;

/// <summary>
/// Wrapper for console operations to facilitate testing by abstracting system console interactions.
/// </summary>
public interface IConsoleWrapper
{
    /// <summary>
    /// Writes the specified message followed by a line terminator to the standard output stream.
    /// </summary>
    /// <param name="message">The message to write.</param>
    void WriteLine(string message);

    /// <summary>
    /// Writes a debug message to the trace listeners in the Debug.Listeners collection.
    /// </summary>
    /// <param name="message">The debug message to write.</param>
    void Debug(string message);

    /// <summary>
    /// Writes the specified error message followed by a line terminator to the standard error stream.
    /// </summary>
    /// <param name="message">The error message to write.</param>
    void Error(string message);

    /// <summary>
    /// Reads the next line of characters from the standard input stream.
    /// </summary>
    /// <returns>The next line of characters from the input stream, or null if no more lines are available.</returns>
    string ReadLine();
}