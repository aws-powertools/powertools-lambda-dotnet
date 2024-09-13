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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class LoggerConfiguration.
///     Implements the
///     <see cref="T:Microsoft.Extensions.Options.IOptions{LoggerConfiguration}" />
/// </summary>
/// <seealso cref="T:Microsoft.Extensions.Options.IOptions{LoggerConfiguration}" />
public class LoggerConfiguration : IOptions<LoggerConfiguration>
{
    /// <summary>
    ///     Service name is used for logging.
    ///     This can be also set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
    /// </summary>
    /// <value>The service.</value>
    public string Service { get; set; }

    /// <summary>
    ///     Specify the minimum log level for logging (Information, by default).
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOG_LEVEL</c>.
    /// </summary>
    /// <value>The minimum level.</value>
    public LogLevel? MinimumLevel { get; set; }

    /// <summary>
    ///     Dynamically set a percentage of logs to DEBUG level.
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOGGER_SAMPLE_RATE</c>.
    /// </summary>
    /// <value>The sampling rate.</value>
    public double SamplingRate { get; set; }

    /// <summary>
    ///     The default configured options instance
    /// </summary>
    /// <value>The value.</value>
    LoggerConfiguration IOptions<LoggerConfiguration>.Value => this;

    /// <summary>
    ///     The logger output case.
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOGGER_CASE</c>.
    /// </summary>
    /// <value>The logger output case.</value>
    public LoggerOutputCase LoggerOutputCase { get; set; }
}