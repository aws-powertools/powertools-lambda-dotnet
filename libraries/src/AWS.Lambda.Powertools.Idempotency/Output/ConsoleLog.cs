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

namespace AWS.Lambda.Powertools.Idempotency.Output;

/// <summary>
/// A log that writes to the console in a colorful way.
/// </summary>
public class ConsoleLog : ILog
{
    /// <summary>
    /// Writes an informational message to the log.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    public void WriteInformation(string format, params object[] args) => Write(ConsoleColor.White, format, args);

    /// <summary>
    /// Writes an error message to the log.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    public void WriteError(string format, params object[] args) => Write(ConsoleColor.Red, format, args);

    /// <summary>
    /// Writes a warning message to the log.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    public void WriteWarning(string format, params object[] args) => Write(ConsoleColor.Yellow, format, args);

    /// <summary>
    /// Writes a debug message to the log.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    public void WriteDebug(string format, params object[] args) => Write(ConsoleColor.Cyan, format, args);

    static void Write(ConsoleColor color, string format, object[] args)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        try
        {
            Console.WriteLine(format, args);
        }
        finally
        {
            Console.ForegroundColor = oldColor;
        }
    }
}