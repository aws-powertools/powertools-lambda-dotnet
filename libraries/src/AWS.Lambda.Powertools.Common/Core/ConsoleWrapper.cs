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
    private static TextWriter _testOutputStream;
    private static bool _outputResetPerformed = false;
    private static bool _inTestMode = false;

    /// <inheritdoc />
    public void WriteLine(string message)
    {
        if (_inTestMode && _testOutputStream != null)
        {
            _testOutputStream.WriteLine(message);
        }
        else
        {
            EnsureConsoleOutputOnce();
            Console.WriteLine(message);
        }
    }

    /// <inheritdoc />
    public void Debug(string message)
    {
        if (_inTestMode && _testOutputStream != null)
        {
            _testOutputStream.WriteLine(message);
        }
        else
        {
            EnsureConsoleOutputOnce();
            System.Diagnostics.Debug.WriteLine(message);
        }
    }

    /// <inheritdoc />
    public void Error(string message)
    {
        if (_inTestMode && _testOutputStream != null)
        {
            _testOutputStream.WriteLine(message);
        }
        else
        {
            if (!_override)
            {
                var errordOutput = new StreamWriter(Console.OpenStandardError());
                errordOutput.AutoFlush = true;
                Console.SetError(errordOutput);
            }
            Console.Error.WriteLine(message);
        }
    }

    /// <summary>
    ///     Set the ConsoleWrapper to use a different TextWriter
    ///     This is useful for unit tests where you want to capture the output
    /// </summary>
    public static void SetOut(TextWriter consoleOut)
    {
        _testOutputStream = consoleOut;
        _inTestMode = true;
        _override = true;
        Console.SetOut(consoleOut);
    }
    
    private static void EnsureConsoleOutputOnce()
    {
        if (_outputResetPerformed) return;
        OverrideLambdaLogger();
        _outputResetPerformed = true;
    }
    
    private static void OverrideLambdaLogger()
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

    /// <summary>
    ///     Reset the ConsoleWrapper to its original state
    /// </summary>
    public static void ResetForTest()
    {
        _override = false;
        _inTestMode = false;
        _testOutputStream = null;
        _outputResetPerformed = false;
    }
    
    /// <summary>
    ///     Clear the output reset flag
    /// </summary>
    public static void ClearOutputResetFlag()
    {
        _outputResetPerformed = false;
    }
}