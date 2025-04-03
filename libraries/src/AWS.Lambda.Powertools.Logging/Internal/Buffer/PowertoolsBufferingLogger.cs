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
using AWS.Lambda.Powertools.Common;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Logger implementation that supports buffering
/// </summary>
internal class PowertoolsBufferingLogger : ILogger
{
    private readonly ILogger _innerLogger;
    private readonly Func<PowertoolsLoggerConfiguration> _getCurrentConfig;
    private readonly LogBuffer _buffer;

    public PowertoolsBufferingLogger(
        ILogger innerLogger,
        Func<PowertoolsLoggerConfiguration> getCurrentConfig,
        IPowertoolsConfigurations powertoolsConfigurations)
    {
        _innerLogger = innerLogger;
        _getCurrentConfig = getCurrentConfig;
        _buffer = new LogBuffer(powertoolsConfigurations);
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return _innerLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        var options = _getCurrentConfig();
        var bufferOptions = options.LogBuffering;

        // Check if this log should be buffered
        bool shouldBuffer = logLevel <= bufferOptions.BufferAtLogLevel;

        if (shouldBuffer)
        {
            // Add to buffer instead of logging
            try
            {
                if (_innerLogger is PowertoolsLogger powertoolsLogger)
                {
                    var logEntry = powertoolsLogger.LogEntryString(logLevel, state, exception, formatter);
                    
                    // Check the size of the log entry, log it if too large
                    var size = 100 + (logEntry?.Length ?? 0) * 2;
                    if (size > bufferOptions.MaxBytes)
                    {
                        // log the entry directly if it exceeds the buffer size
                        powertoolsLogger.LogLine(logEntry);
                        powertoolsLogger.LogWarning("Cannot add item to the buffer");
                    }
                    else
                    {
                        _buffer.Add(logEntry, bufferOptions.MaxBytes, size);
                    }
                }
            }
            catch (Exception ex)
            {
                // If buffering fails, try to log an error about it
                try
                {
                    _innerLogger.LogError(ex, "Failed to buffer log entry");
                }
                catch
                {
                    // Last resort: if even that fails, just suppress the error
                }
            }
        }
        else
        {
            // If this is an error and we should flush on error
            if (bufferOptions.FlushOnErrorLog &&
                logLevel >= LogLevel.Error)
            {
                FlushBuffer();
            }
        }
    }

    /// <summary>
    /// Flush buffered logs to the inner logger
    /// </summary>
    public void FlushBuffer()
    {
        try
        {
            if (_innerLogger is PowertoolsLogger powertoolsLogger)
            {
                if (_buffer.HasEvictions)
                {
                    powertoolsLogger.LogWarning("Some logs are not displayed because they were evicted from the buffer. Increase buffer size to store more logs in the buffer");
                }
         
                // Get all buffered entries
                var entries = _buffer.GetAndClear();
                
                // Log each entry directly
                foreach (var entry in entries)
                {
                    powertoolsLogger.LogLine(entry);
                }
            }
        }
        catch (Exception ex)
        {
            // If the entire flush operation fails, try to log an error
            try
            {
                _innerLogger.LogError(ex, "Failed to flush log buffer");
            }
            catch
            {
                // If even that fails, just suppress the error
            }
        }
    }

    /// <summary>
    /// Clear the buffer without logging
    /// </summary>
    public void ClearBuffer()
    {
        _buffer.Clear();
    }

    /// <summary>
    /// Clear buffered logs only for the current invocation
    /// </summary>
    public void ClearCurrentInvocation()
    {
        _buffer.ClearCurrentInvocation();
    }
}