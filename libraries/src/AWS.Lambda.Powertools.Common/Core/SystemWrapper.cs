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
using System.Text;

namespace AWS.Lambda.Powertools.Common;

/// <summary>
///     Class SystemWrapper.
///     Implements the <see cref="ISystemWrapper" />
/// </summary>
/// <seealso cref="ISystemWrapper" />
public class SystemWrapper : ISystemWrapper
{
    private static IPowertoolsEnvironment _powertoolsEnvironment;

    /// <summary>
    ///     The instance
    /// </summary>
    private static ISystemWrapper _instance;

    /// <summary>
    ///     Prevents a default instance of the <see cref="SystemWrapper" /> class from being created.
    /// </summary>
    public SystemWrapper(IPowertoolsEnvironment powertoolsEnvironment)
    {
        _powertoolsEnvironment = powertoolsEnvironment;
        _instance ??= this;
        
        // Clear AWS SDK Console injected parameters StdOut and StdErr
        var standardOutput = new StreamWriter(Console.OpenStandardOutput());
        standardOutput.AutoFlush = true;
        Console.SetOut(standardOutput);
        var errordOutput = new StreamWriter(Console.OpenStandardError());
        errordOutput.AutoFlush = true;
        Console.SetError(errordOutput);
    }

    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static ISystemWrapper Instance => _instance ??= new SystemWrapper(PowertoolsEnvironment.Instance);

    /// <summary>
    ///     Gets the environment variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>System.String.</returns>
    public string GetEnvironmentVariable(string variable)
    {
        return _powertoolsEnvironment.GetEnvironmentVariable(variable);
    }

    /// <summary>
    ///     Logs the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Log(string value)
    {
        Console.Write(value);
    }

    /// <summary>
    ///     Logs the line.
    /// </summary>
    /// <param name="value">The value.</param>
    public void LogLine(string value)
    {
        Console.WriteLine(value);
    }

    /// <summary>
    ///     Gets random number
    /// </summary>
    /// <returns>System.Double.</returns>
    public double GetRandom()
    {
        return new Random().NextDouble();
    }

    /// <inheritdoc />
    public void SetEnvironmentVariable(string variable, string value)
    {
        _powertoolsEnvironment.SetEnvironmentVariable(variable, value);
    }

    /// <inheritdoc />
    public void SetExecutionEnvironment<T>(T type)
    {
        const string envName = Constants.AwsExecutionEnvironmentVariableName;
        var envValue = new StringBuilder();
        var currentEnvValue = GetEnvironmentVariable(envName);
        var assemblyName = ParseAssemblyName(_powertoolsEnvironment.GetAssemblyName(type));

        // If there is an existing execution environment variable add the annotations package as a suffix.
        if (!string.IsNullOrEmpty(currentEnvValue))
        {
            // Avoid duplication - should not happen since the calling Instances are Singletons - defensive purposes
            if (currentEnvValue.Contains(assemblyName))
            {
                return;
            }

            envValue.Append($"{currentEnvValue} ");
        }

        var assemblyVersion = _powertoolsEnvironment.GetAssemblyVersion(type);

        envValue.Append($"{assemblyName}/{assemblyVersion}");

        SetEnvironmentVariable(envName, envValue.ToString());
    }

    /// <inheritdoc />
    public void SetOut(TextWriter writeTo)
    {
        Console.SetOut(writeTo);
    }

    /// <summary>
    /// Parsing the name to conform with the required naming convention for the UserAgent header (PTFeature/Name/Version)
    /// Fallback to Assembly Name on exception
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <returns></returns>
    private string ParseAssemblyName(string assemblyName)
    {
        try
        {
            var parsedName = assemblyName.Substring(assemblyName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            return $"{Constants.FeatureContextIdentifier}/{parsedName}";
        }
        catch
        {
            //NOOP
        }

        return $"{Constants.FeatureContextIdentifier}/{assemblyName}";
    }
}