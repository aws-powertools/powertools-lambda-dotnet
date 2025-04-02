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
using AWS.Lambda.Powertools.Common;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class PowertoolsConfigurationsExtension.
/// </summary>
internal static class PowertoolsConfigurationsExtension
{
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
    internal static LogLevel GetLogLevel(this IPowertoolsConfigurations powertoolsConfigurations, LogLevel logLevel = LogLevel.None)
    {
        if (logLevel != LogLevel.None)
            return logLevel;

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
    /// Determines whether [is lambda log level enabled].
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    /// <returns></returns>
    internal static bool LambdaLogLevelEnabled(this IPowertoolsConfigurations powertoolsConfigurations)
    {
        return powertoolsConfigurations.GetLambdaLogLevel() != LogLevel.None;
    }
}