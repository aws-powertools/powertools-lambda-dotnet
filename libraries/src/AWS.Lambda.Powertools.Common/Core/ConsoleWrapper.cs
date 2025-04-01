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
using System.IO;

namespace AWS.Lambda.Powertools.Common;

/// <inheritdoc />
public class ConsoleWrapper : IConsoleWrapper
{
    private static bool _redirected;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleWrapper" /> class.
    /// </summary>
    public ConsoleWrapper()
    {
        if(_redirected)
        {
            _redirected = false;
            return;
        }
        
        var standardOutput = new StreamWriter(Console.OpenStandardOutput());
        standardOutput.AutoFlush = true;
        Console.SetOut(standardOutput);
        var errordOutput = new StreamWriter(Console.OpenStandardError());
        errordOutput.AutoFlush = true;
        Console.SetError(errordOutput);
    }
    /// <inheritdoc />
    public void WriteLine(string message) => Console.WriteLine(message);
    /// <inheritdoc />
    public void Debug(string message) => System.Diagnostics.Debug.WriteLine(message);
    /// <inheritdoc />
    public void Error(string message) => Console.Error.WriteLine(message);
    
    internal static void SetOut(StringWriter consoleOut)
    {
        _redirected = true;
        Console.SetOut(consoleOut);
    }
}