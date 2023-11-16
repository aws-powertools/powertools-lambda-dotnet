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
    
    internal static LogLevel GetLambdaLogLevel(this IPowertoolsConfigurations powertoolsConfigurations)
    {
        AwsLogLevelMapper.TryGetValue((powertoolsConfigurations.AWSLambdaLogLevel ?? "").Trim().ToUpper(), out var awsLogLevel);

        if (Enum.TryParse(awsLogLevel, true, out LogLevel result))
        {
            //Logger.LogWarning("Oopss!!");
            return result;
        }

        return LogLevel.None;
    }
    
    internal static LoggerOutputCase GetLoggerOutputCase(this IPowertoolsConfigurations powertoolsConfigurations,
        LoggerOutputCase? loggerOutputCase = null)
    {
        if (loggerOutputCase.HasValue)
            return loggerOutputCase.Value;

        if (Enum.TryParse((powertoolsConfigurations.LoggerOutputCase ?? "").Trim(), true, out LoggerOutputCase result))
            return result;

        return LoggingConstants.DefaultLoggerOutputCase;
    }

    private static Dictionary<string, string> AwsLogLevelMapper = new()
    {
        { "TRACE", "TRACE" },
        { "DEBUG", "DEBUG" },
        { "INFO", "INFORMATION" },
        { "WARN", "WARNING" },
        { "ERROR", "ERROR" },
        { "FATAL", "CRITICAL" }
    };
}