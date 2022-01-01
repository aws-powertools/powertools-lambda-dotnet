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

using System.Collections.Concurrent;
using AWS.Lambda.PowerTools.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.PowerTools.Logging.Internal;

/// <summary>
///     Class LoggerProvider. This class cannot be inherited.
///     Implements the <see cref="Microsoft.Extensions.Logging.ILoggerProvider" />
/// </summary>
/// <seealso cref="Microsoft.Extensions.Logging.ILoggerProvider" />
internal sealed class LoggerProvider : ILoggerProvider
{
    /// <summary>
    ///     The loggers
    /// </summary>
    private readonly ConcurrentDictionary<string, PowerToolsLogger> _loggers = new();


    /// <summary>
    ///     The current configuration
    /// </summary>
    private LoggerConfiguration _currentConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggerProvider" /> class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    internal LoggerProvider(IOptions<LoggerConfiguration> config)
    {
        _currentConfig = config?.Value;
    }

    /// <summary>
    ///     Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName,
            name => new PowerToolsLogger(name,
                PowerToolsConfigurations.Instance,
                SystemWrapper.Instance,
                GetCurrentConfig));
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        _loggers.Clear();
    }

    /// <summary>
    ///     Gets the current configuration.
    /// </summary>
    /// <returns>LoggerConfiguration.</returns>
    private LoggerConfiguration GetCurrentConfig()
    {
        return _currentConfig;
    }

    /// <summary>
    ///     Configures the specified configuration.
    /// </summary>
    /// <param name="config">The configuration.</param>
    internal void Configure(IOptions<LoggerConfiguration> config)
    {
        if (_currentConfig is not null || config is null)
            return;

        _currentConfig = config.Value;
        foreach (var logger in _loggers.Values)
            logger.ClearConfig();
    }
}