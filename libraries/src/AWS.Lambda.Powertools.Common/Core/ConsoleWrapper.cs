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
    private static bool _override;

    /// <inheritdoc />
    public void WriteLine(string message)
    {
        OverrideLambdaLogger();
        Console.WriteLine(message);
    }

    /// <inheritdoc />
    public void Debug(string message)
    {
        OverrideLambdaLogger();
        System.Diagnostics.Debug.WriteLine(message);
    }

    /// <inheritdoc />
    public void Error(string message)
    {
        if (!_override)
        {
            var errordOutput = new StreamWriter(Console.OpenStandardError());
            errordOutput.AutoFlush = true;
            Console.SetError(errordOutput);
        }

        Console.Error.WriteLine(message);
    }

    internal static void SetOut(StringWriter consoleOut)
    {
        _override = true;
        Console.SetOut(consoleOut);
    }
    
    private void OverrideLambdaLogger()
    {
        if (_override)
        {
            return;
        }
        // Force override of LambdaLogger
        var standardOutput = new StreamWriter(Console.OpenStandardOutput());
        standardOutput.AutoFlush = true;
        Console.SetOut(standardOutput);
    }
    
    internal static void WriteLine(string logLevel, string message)
    {
        Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\t{logLevel}\t{message}");
    }

    public static void ResetForTest()
    {
        _override = false;
    }
}