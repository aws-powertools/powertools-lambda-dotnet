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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Serializers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class PowertoolsConfigurationsExtension.
/// </summary>
internal static class PowertoolsConfigurationsExtension
{
    private static readonly object _lock = new object();
    private static LoggerConfiguration _config;

    /// <summary>
    ///     Maps AWS log level to .NET log level
    /// </summary>
    private static readonly Dictionary<string, LogLevel> AwsLogLevelMapper = new(StringComparer.OrdinalIgnoreCase)
    {
        { "TRACE", LogLevel.Trace },
        { "DEBUG", LogLevel.Debug },
        { "INFO", LogLevel.Information },
        { "WARN", LogLevel.Warning },
        { "ERROR", LogLevel.Error },
        { "FATAL", LogLevel.Critical }
    };

    /// <summary>
    ///     Gets the log level.
    /// </summary>
    /// <param name="powertoolsConfigurations">The Powertools for AWS Lambda (.NET) configurations.</param>
    /// <param name="logLevel">The log level.</param>
    /// <returns>LogLevel.</returns>
    internal static LogLevel GetLogLevel(this IPowertoolsConfigurations powertoolsConfigurations,
        LogLevel? logLevel = null)
    {
        if (logLevel.HasValue)
            return logLevel.Value;

        if (Enum.TryParse((powertoolsConfigurations.LogLevel ?? "").Trim(), true, out LogLevel result))
            return result;

        return LoggingConstants.DefaultLogLevel;
    }

    /// <summary>
    /// Lambda Log Level Mapper
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    /// <returns></returns>
    internal static LogLevel GetLambdaLogLevel(this IPowertoolsConfigurations powertoolsConfigurations)
    {
        var awsLogLevel = (powertoolsConfigurations.AWSLambdaLogLevel ?? string.Empty).Trim().ToUpperInvariant();

        return AwsLogLevelMapper.GetValueOrDefault(awsLogLevel, LogLevel.None);
    }

    /// <summary>
    /// Determines the logger output case based on configuration and input.
    /// </summary>
    /// <param name="powertoolsConfigurations">The Powertools configurations.</param>
    /// <param name="loggerOutputCase">Optional explicit logger output case.</param>
    /// <returns>The determined LoggerOutputCase.</returns>
    internal static LoggerOutputCase GetLoggerOutputCase(this IPowertoolsConfigurations powertoolsConfigurations,
        LoggerOutputCase loggerOutputCase)
    {
        if (loggerOutputCase != LoggerOutputCase.Default)
            return loggerOutputCase;

        if (Enum.TryParse((powertoolsConfigurations.LoggerOutputCase ?? "").Trim(), true, out LoggerOutputCase result))
            return result;

        return LoggingConstants.DefaultLoggerOutputCase;
    }

    /// <summary>
    ///     Gets the current configuration.
    /// </summary>
    /// <returns>AWS.Lambda.Powertools.Logging.LoggerConfiguration.</returns>
    internal static void SetCurrentConfig(this IPowertoolsConfigurations powertoolsConfigurations, LoggerConfiguration config, ISystemWrapper systemWrapper)
    {
        lock (_lock)
        {
            _config = config;

            var logLevel = powertoolsConfigurations.GetLogLevel(_config.MinimumLevel);
            var lambdaLogLevel = powertoolsConfigurations.GetLambdaLogLevel();
            var lambdaLogLevelEnabled = powertoolsConfigurations.LambdaLogLevelEnabled();

            if (lambdaLogLevelEnabled && logLevel < lambdaLogLevel)
            {
                systemWrapper.LogLine($"Current log level ({logLevel}) does not match AWS Lambda Advanced Logging Controls minimum log level ({lambdaLogLevel}). This can lead to data loss, consider adjusting them.");
            }

            // Set service
            _config.Service = _config.Service ?? powertoolsConfigurations.Service;

            // Set output case
            var loggerOutputCase = powertoolsConfigurations.GetLoggerOutputCase(_config.LoggerOutputCase);
            _config.LoggerOutputCase = loggerOutputCase;
            PowertoolsLoggingSerializer.ConfigureNamingPolicy(loggerOutputCase);

            // Set log level
            var minLogLevel = lambdaLogLevelEnabled ? lambdaLogLevel : logLevel;
            _config.MinimumLevel = minLogLevel;

            // Set sampling rate
            SetSamplingRate(powertoolsConfigurations, systemWrapper, minLogLevel);
        }
    }

    /// <summary>
    /// Set sampling rate
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    /// <param name="config"></param>
    /// <param name="systemWrapper"></param>
    /// <param name="minLogLevel"></param>
    /// <returns></returns>
    private static void SetSamplingRate(IPowertoolsConfigurations powertoolsConfigurations, ISystemWrapper systemWrapper, LogLevel minLogLevel)
    {
        double samplingRate = _config.SamplingRate == 0 ? powertoolsConfigurations.LoggerSampleRate : _config.SamplingRate;
        samplingRate = ValidateSamplingRate(samplingRate, minLogLevel, systemWrapper);

        _config.SamplingRate = samplingRate;

        if (samplingRate > 0)
        {
            double sample = systemWrapper.GetRandom();

            if (sample <= samplingRate)
            {
                systemWrapper.LogLine($"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {samplingRate}, Sampler Value: {sample}.");
                _config.MinimumLevel = LogLevel.Debug;
            }
        }
    }
    
    /// <summary>
    /// Validate Sampling rate
    /// </summary>
    /// <param name="samplingRate"></param>
    /// <param name="minLogLevel"></param>
    /// <param name="systemWrapper"></param>
    /// <returns></returns>
    private static double ValidateSamplingRate(double samplingRate, LogLevel minLogLevel, ISystemWrapper systemWrapper)
    {
        if (samplingRate < 0 || samplingRate > 1)
        {
            if (minLogLevel is LogLevel.Debug or LogLevel.Trace)
            {
                systemWrapper.LogLine($"Skipping sampling rate configuration because of invalid value. Sampling rate: {samplingRate}");
            }
            return 0;
        }

        return samplingRate;
    }

    /// <summary>
    /// Determines whether [is lambda log level enabled].
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    /// <returns></returns>
    internal static bool LambdaLogLevelEnabled(this IPowertoolsConfigurations powertoolsConfigurations)
    {
        return powertoolsConfigurations.GetLambdaLogLevel() != LogLevel.None;
    }

    /// <summary>
    /// Converts the input string to the configured output case.
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    /// <param name="correlationIdPath">The string to convert.</param>
    /// <param name="loggerOutputCase"></param>
    /// <returns>
    /// The input string converted to the configured case (camel, pascal, or snake case).
    /// </returns>
    internal static string ConvertToOutputCase(this IPowertoolsConfigurations powertoolsConfigurations,
        string correlationIdPath, LoggerOutputCase loggerOutputCase)
    {
        return powertoolsConfigurations.GetLoggerOutputCase(loggerOutputCase) switch
        {
            LoggerOutputCase.CamelCase => ToCamelCase(correlationIdPath),
            LoggerOutputCase.PascalCase => ToPascalCase(correlationIdPath),
            _ => ToSnakeCase(correlationIdPath), // default snake_case
        };
    }

    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>The input string converted to snake_case.</returns>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder(input.Length + 10);
        bool lastCharWasUnderscore = false;
        bool lastCharWasUpper = false;

        for (int i = 0; i < input.Length; i++)
        {
            char currentChar = input[i];

            if (currentChar == '_')
            {
                result.Append('_');
                lastCharWasUnderscore = true;
                lastCharWasUpper = false;
            }
            else if (char.IsUpper(currentChar))
            {
                if (i > 0 && !lastCharWasUnderscore &&
                    (!lastCharWasUpper || (i + 1 < input.Length && char.IsLower(input[i + 1]))))
                {
                    result.Append('_');
                }

                result.Append(char.ToLowerInvariant(currentChar));
                lastCharWasUnderscore = false;
                lastCharWasUpper = true;
            }
            else
            {
                result.Append(char.ToLowerInvariant(currentChar));
                lastCharWasUnderscore = false;
                lastCharWasUpper = false;
            }
        }

        return result.ToString();
    }


    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>The input string converted to PascalCase.</returns>
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                // Capitalize the first character of each word
                result.Append(char.ToUpperInvariant(word[0]));

                // Handle the rest of the characters
                if (word.Length > 1)
                {
                    // If the word is all uppercase, convert the rest to lowercase
                    if (word.All(char.IsUpper))
                    {
                        result.Append(word.Substring(1).ToLowerInvariant());
                    }
                    else
                    {
                        // Otherwise, keep the original casing
                        result.Append(word.Substring(1));
                    }
                }
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The input string converted to camelCase.</returns>
    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // First, convert to PascalCase
        string pascalCase = ToPascalCase(input);

        // Then convert the first character to lowercase
        return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
    }
    
    /// <summary>
    ///     Determines whether [is log level enabled].
    /// </summary>
    /// <param name="powertoolsConfigurations">The Powertools for AWS Lambda (.NET) configurations.</param>
    /// <param name="logLevel">The log level.</param>
    /// <returns><c>true</c> if [is log level enabled]; otherwise, <c>false</c>.</returns>
    internal static bool IsLogLevelEnabled(this IPowertoolsConfigurations powertoolsConfigurations, LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= _config.MinimumLevel;
    }
    
    /// <summary>
    ///     Gets the current configuration.
    /// </summary>
    /// <returns>AWS.Lambda.Powertools.Logging.LoggerConfiguration.</returns>
    internal static LoggerConfiguration CurrentConfig(this IPowertoolsConfigurations powertoolsConfigurations)
    {
        return _config;
    }
}