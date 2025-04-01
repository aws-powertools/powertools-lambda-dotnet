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

using System.IO;

namespace AWS.Lambda.Powertools.Common;

/// <summary>
///     Interface ISystemWrapper
/// </summary>
public interface ISystemWrapper
{
    /// <summary>
    ///     Gets the environment variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>System.String.</returns>
    string GetEnvironmentVariable(string variable);

    /// <summary>
    ///     Logs the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    void Log(string value);

    /// <summary>
    ///     Logs the line.
    /// </summary>
    /// <param name="value">The value.</param>
    void LogLine(string value);

    /// <summary>
    ///     Gets random number
    /// </summary>
    /// <returns>System.Double.</returns>
    double GetRandom();
    
    /// <summary>
    ///     Sets the environment variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="value"></param>
    void SetEnvironmentVariable(string variable, string value);
    
    /// <summary>
    /// Sets the execution Environment Variable (AWS_EXECUTION_ENV)
    /// </summary>
    /// <param name="type"></param>
    void SetExecutionEnvironment<T>(T type);

    /// <summary>
    /// Sets console output
    /// Useful for testing and checking the console output
    /// <code>
    /// var consoleOut = new StringWriter();
    /// SystemWrapper.Instance.SetOut(consoleOut);
    /// </code>
    /// </summary>
    /// <param name="writeTo">The TextWriter instance where to write to</param>
    void SetOut(TextWriter writeTo);
}