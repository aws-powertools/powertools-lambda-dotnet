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

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Logger provider that supports buffering logs
/// </summary>
[ProviderAlias("PowertoolsBuffering")]
internal class BufferingLoggerProvider : PowertoolsLoggerProvider
{
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;
    private readonly ConcurrentDictionary<string, PowertoolsBufferingLogger> _loggers = new();

    internal BufferingLoggerProvider(
        PowertoolsLoggerConfiguration config,
        IPowertoolsConfigurations powertoolsConfigurations)
        : base(config, powertoolsConfigurations)
    {
        _powertoolsConfigurations = powertoolsConfigurations;
        // Register with the buffer manager
        LogBufferManager.RegisterProvider(this);
    }

    public override ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(
            categoryName,
            name => new PowertoolsBufferingLogger(
                base.CreateLogger(name), // Use the parent's logger creation
                GetCurrentConfig,
                _powertoolsConfigurations));
    }

    /// <summary>
    /// Flush all buffered logs
    /// </summary>
    internal void FlushBuffers()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.FlushBuffer();
        }
    }

    /// <summary>
    /// Clear all buffered logs
    /// </summary>
    internal void ClearBuffers()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.ClearBuffer();
        }
    }

    /// <summary>
    /// Clear buffered logs for the current invocation only
    /// </summary>
    internal void ClearCurrentBuffer()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.ClearCurrentInvocation();
        }
    }

    public override void Dispose()
    {
        // Flush all buffers before disposing
        foreach (var logger in _loggers.Values)
        {
            logger.FlushBuffer();
        }
        
        // Unregister from buffer manager
        LogBufferManager.UnregisterProvider(this);

        _loggers.Clear();
        base.Dispose();
    }
}