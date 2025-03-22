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

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Serializers;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class PowertoolsLoggerConfiguration.
///     Implements the <see cref="T:Microsoft.Extensions.Options.IOptions{PowertoolsLoggerConfiguration}" />
/// </summary>
public class PowertoolsLoggerConfiguration : IOptions<PowertoolsLoggerConfiguration>
{
    public const string ConfigurationSectionName = "AWS.Lambda.Powertools.Logging.Logger";

    /// <summary>
    ///     Service name is used for logging.
    ///     This can be also set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
    /// </summary>
    public string? Service { get; set; } = null;
    
    /// <summary>
    ///     Timestamp format for logging.
    /// </summary>
    public string? TimestampFormat { get; set; }

    /// <summary>
    ///     Specify the minimum log level for logging (Information, by default).
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOG_LEVEL</c>.
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.None;

    /// <summary>
    ///     Dynamically set a percentage of logs to DEBUG level.
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOGGER_SAMPLE_RATE</c>.
    /// </summary>
    public double SamplingRate { get; set; }

    /// <summary>
    ///     The logger output case.
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOGGER_CASE</c>.
    /// </summary>
    public LoggerOutputCase LoggerOutputCase { get; set; } = LoggerOutputCase.Default;

    /// <summary>
    /// Internal key used for log level in output
    /// </summary>
    internal string LogLevelKey { get; set; } = "level";

    /// <summary>
    /// Custom output logger to use instead of Console
    /// </summary>
    public ISystemWrapper? LoggerOutput { get; set; }

    /// <summary>
    /// Custom log formatter to use for formatting log entries
    /// </summary>
    public ILogFormatter? LogFormatter { get; set; }

    /// <summary>
    /// JSON serializer options to use for log serialization
    /// </summary>
    private JsonSerializerOptions? _jsonOptions;
    public JsonSerializerOptions? JsonOptions
    {
        get => _jsonOptions;
        set
        {
            _jsonOptions = value;
            if (_jsonOptions != null)
            {
                PowertoolsLoggingSerializer.SetOptions(_jsonOptions);
            }
        }
    }

    /// <summary>
    /// Options for log buffering
    /// </summary>
    public LogBufferingOptions LogBufferingOptions { get; set; } = new LogBufferingOptions();

    /// <summary>
    /// Apply output case configuration
    /// </summary>
    internal void ApplyOutputCase()
    {
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase);
    }

    // IOptions implementation
    PowertoolsLoggerConfiguration IOptions<PowertoolsLoggerConfiguration>.Value => this;
}