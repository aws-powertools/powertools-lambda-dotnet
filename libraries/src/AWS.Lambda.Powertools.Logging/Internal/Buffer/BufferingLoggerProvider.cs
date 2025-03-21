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
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Logger provider that supports buffering logs
/// </summary>
[ProviderAlias("PowertoolsBuffering")]
internal partial class BufferingLoggerProvider : ILoggerProvider
{
    private readonly ILoggerProvider _innerProvider;
    private readonly ConcurrentDictionary<string, PowertoolsBufferingLogger> _loggers = new();
    private readonly IOptionsMonitor<PowertoolsLoggerConfiguration> _options;
    
    public BufferingLoggerProvider(
        ILoggerProvider innerProvider, 
        IOptionsMonitor<PowertoolsLoggerConfiguration> options)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        // Register with the buffer manager
        LogBufferManager.RegisterProvider(this);
    }
    
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(
            categoryName, 
            name => new PowertoolsBufferingLogger(
                _innerProvider.CreateLogger(name),
                _options,
                name));
    }
    
    public void Dispose()
    {
        // Flush all buffers before disposing
        foreach (var logger in _loggers.Values)
        {
            logger.FlushBuffer();
        }
        
        _innerProvider.Dispose();
        _loggers.Clear();
    }
    
    /// <summary>
    /// Flush all buffered logs
    /// </summary>
    public void FlushBuffers()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.FlushBuffer();
        }
    }
    
    /// <summary>
    /// Clear all buffered logs
    /// </summary>
    public void ClearBuffers()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.ClearBuffer();
        }
    }
    
    /// <summary>
    /// Clear buffered logs for the current invocation only
    /// </summary>
    public void ClearCurrentBuffer()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.ClearCurrentInvocation();
        }
    }
}