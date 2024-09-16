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
using AWS.Lambda.Powertools.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class LoggerProvider. This class cannot be inherited.
///     Implements the <see cref="T:Microsoft.Extensions.Logging.ILoggerProvider" />
/// </summary>
/// <seealso cref="T:Microsoft.Extensions.Logging.ILoggerProvider" />
public sealed class LoggerProvider : ILoggerProvider
{
    /// <summary>
    ///     The powertools configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;
    
    /// <summary>
    ///     The system wrapper
    /// </summary>
    private readonly ISystemWrapper _systemWrapper;

    /// <summary>
    ///     The loggers
    /// </summary>
    private readonly ConcurrentDictionary<string, PowertoolsLogger> _loggers = new();


    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggerProvider" /> class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <param name="powertoolsConfigurations"></param>
    /// <param name="systemWrapper"></param>
    public LoggerProvider(IOptions<LoggerConfiguration> config, IPowertoolsConfigurations powertoolsConfigurations, ISystemWrapper systemWrapper)
    {
        _powertoolsConfigurations = powertoolsConfigurations;
        _systemWrapper = systemWrapper;
        _powertoolsConfigurations.SetCurrentConfig(config?.Value, systemWrapper);
    }

    /// <summary>
    ///     Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName,
            name => PowertoolsLogger.CreateLogger(name,
                _powertoolsConfigurations,
                _systemWrapper));
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        _loggers.Clear();
    }
}