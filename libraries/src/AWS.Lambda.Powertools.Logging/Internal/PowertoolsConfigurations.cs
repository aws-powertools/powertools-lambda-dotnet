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
using System.Text.Encodings.Web;
using System.Text.Json;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Converters;
using AWS.Lambda.Powertools.Logging.Serializers;
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
        AwsLogLevelMapper.TryGetValue((powertoolsConfigurations.AWSLambdaLogLevel ?? "").Trim().ToUpper(),
            out var awsLogLevel);

        if (Enum.TryParse(awsLogLevel, true, out LogLevel result))
        {
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

    private static readonly Dictionary<string, string> AwsLogLevelMapper = new()
    {
        { "TRACE", "TRACE" },
        { "DEBUG", "DEBUG" },
        { "INFO", "INFORMATION" },
        { "WARN", "WARNING" },
        { "ERROR", "ERROR" },
        { "FATAL", "CRITICAL" }
    };

    internal static JsonSerializerOptions BuildJsonSerializerOptions(
        this IPowertoolsConfigurations powertoolsConfigurations, LoggerOutputCase? loggerOutputCase = null)
    {
        var jsonOptions = powertoolsConfigurations.GetLoggerOutputCase(loggerOutputCase) switch
        {
            LoggerOutputCase.CamelCase => new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            },
            LoggerOutputCase.PascalCase => new JsonSerializerOptions
            {
                PropertyNamingPolicy = PascalCaseNamingPolicy.Instance,
                DictionaryKeyPolicy = PascalCaseNamingPolicy.Instance
            },
            _ => new JsonSerializerOptions //defaults to snake_case
            {
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
                DictionaryKeyPolicy = SnakeCaseNamingPolicy.Instance
            }
        };

        jsonOptions.Converters.Add(new ByteArrayConverter());
        jsonOptions.Converters.Add(new ExceptionConverter());
        jsonOptions.Converters.Add(new MemoryStreamConverter());
        jsonOptions.Converters.Add(new ConstantClassConverter());
        jsonOptions.Converters.Add(new DateOnlyConverter());
        jsonOptions.Converters.Add(new TimeOnlyConverter());
            
        jsonOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

#if NET8_0_OR_GREATER
        jsonOptions.TypeInfoResolver = LoggingSerializationContext.Default;
#endif

        return jsonOptions;
    }
}