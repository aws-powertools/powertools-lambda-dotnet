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

/// <summary>
///     Class SystemWrapper.
///     Implements the <see cref="ISystemWrapper" />
/// </summary>
/// <seealso cref="ISystemWrapper" />
internal class SystemWrapper : ISystemWrapper
{
    /// <summary>
    ///     Prevents a default instance of the <see cref="SystemWrapper" /> class from being created.
    /// </summary>
    public SystemWrapper()
    {
        // Clear AWS SDK Console injected parameters StdOut and StdErr
        var standardOutput = new StreamWriter(Console.OpenStandardOutput());
        standardOutput.AutoFlush = true;
        Console.SetOut(standardOutput);
        var errordOutput = new StreamWriter(Console.OpenStandardError());
        errordOutput.AutoFlush = true;
        Console.SetError(errordOutput);
    }

    /// <inheritdoc />
    public void Log(string value)
    {
        Console.Write(value);
    }

    /// <inheritdoc />
    public void LogLine(string value)
    {
        Console.WriteLine(value);
    }

    /// <inheritdoc />
    public double GetRandom()
    {
        return new Random().NextDouble();
    }

    /// <inheritdoc />
    public void SetOut(TextWriter writeTo)
    {
        Console.SetOut(writeTo);
    }
}