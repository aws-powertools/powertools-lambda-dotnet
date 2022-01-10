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
using AWS.Lambda.PowerTools.Common;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging.Internal;

/// <summary>
///     Class PowerToolsConfigurationsExtension.
/// </summary>
internal static class PowerToolsConfigurationsExtension
{
    /// <summary>
    ///     Gets the log level.
    /// </summary>
    /// <param name="powerToolsConfigurations">The power tools configurations.</param>
    /// <param name="logLevel">The log level.</param>
    /// <returns>LogLevel.</returns>
    internal static LogLevel GetLogLevel(this IPowerToolsConfigurations powerToolsConfigurations,
        LogLevel? logLevel = null)
    {
        if (logLevel.HasValue)
            return logLevel.Value;

        if (Enum.TryParse((powerToolsConfigurations.LogLevel ?? "").Trim(), true, out LogLevel result))
            return result;

        return LoggingConstants.DefaultLogLevel;
    }
}