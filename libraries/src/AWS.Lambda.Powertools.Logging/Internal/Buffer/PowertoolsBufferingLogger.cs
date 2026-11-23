using System;
using System.Collections.Generic;
using AWS.Lambda.Powertools.Common;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Logger implementation that supports buffering
/// </summary>
internal class PowertoolsBufferingLogger : ILogger
{
    private readonly ILogger _logger;
    private readonly Func<PowertoolsLoggerConfiguration> _getCurrentConfig;
    private readonly LogBuffer _buffer;

    public PowertoolsBufferingLogger(
        ILogger logger,
        Func<PowertoolsLoggerConfiguration> getCurrentConfig,
        IPowertoolsConfigurations powertoolsConfigurations)
    {
        _logger = logger;
        _getCurrentConfig = getCurrentConfig;
        _buffer = new LogBuffer(powertoolsConfigurations);
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return _logger.BeginScope(state);
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
            if (_logger is PowertoolsLogger powertoolsLogger)
            {
                Buffer(powertoolsLogger, options, logLevel, state, exception, formatter);
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
            if (_logger is PowertoolsLogger powertoolsLogger)
            {
                if (_buffer.HasEvictions)
                {
                    ConsoleWrapper.WriteLine(LogLevel.Warning.ToLambdaLogLevel(), "Some logs are not displayed because they were evicted from the buffer. Increase buffer size to store more logs in the buffer");
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
                _logger.LogError(ex, "Failed to flush log buffer");
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
    /// Add an entry to the buffer. If that fails, write it now instead of losing it.
    /// </summary>
    private void Buffer<TState>(
        PowertoolsLogger logger,
        PowertoolsLoggerConfiguration config,
        LogLevel logLevel,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        var bufferOptions = config.LogBuffering;
        string logEntry = null;
        var written = false;

        try
        {
            logEntry = logger.LogEntryString(logLevel, state, exception, formatter);

            // Check the size of the log entry, log it if too large
            var size = 100 + logEntry.Length * 2;
            if (size > bufferOptions.MaxBytes)
            {
                // log the entry directly if it exceeds the buffer size
                logger.LogLine(logEntry);
                written = true;
                ConsoleWrapper.WriteLine(LogLevel.Warning.ToLambdaLogLevel(), "Cannot add item to the buffer");
            }
            else
            {
                _buffer.Add(logEntry, bufferOptions.MaxBytes, size);
            }
        }
        catch (Exception ex)
        {
            // If buffering fails, try to log an error about it
            try
            {
                _logger.LogError(ex, "Failed to buffer log entry");
            }
            catch
            {
                // Last resort: if even that fails, just suppress the error
            }

            if (!written)
            {
                WriteUnbuffered(logger, config, logEntry, logLevel, state, exception, formatter);
            }
        }
    }

    /// <summary>
    /// Write an entry that could not be buffered straight to the output.
    /// If the entry can't be formatted, a minimal entry without the message is written instead,
    /// so a custom formatter that masks data is never bypassed.
    /// </summary>
    private static void WriteUnbuffered<TState>(
        PowertoolsLogger logger,
        PowertoolsLoggerConfiguration config,
        string logEntry,
        LogLevel logLevel,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        try
        {
            if (logEntry is null)
            {
                try
                {
                    logEntry = logger.LogEntryString(logLevel, state, exception, formatter);
                }
                catch
                {
                    logEntry = UnformattedEntry(config, logLevel);
                }
            }

            logger.LogLine(logEntry);
        }
        catch
        {
            // Nothing else we can do
        }
    }

    internal const string UnformattedEntryMessage = "Log entry could not be formatted";

    /// <summary>
    /// Build a minimal entry with the same keys as a regular entry, but without the original message.
    /// It only holds strings, so it serializes with the same key casing as regular entries, AOT included.
    /// </summary>
    private static string UnformattedEntry(PowertoolsLoggerConfiguration config, LogLevel logLevel)
    {
        var now = DateTime.UtcNow;
        string timestamp;
        try
        {
            timestamp = now.ToString(config.TimestampFormat ?? "o");
        }
        catch (FormatException)
        {
            // An invalid TimestampFormat may be why the entry failed in the first place
            timestamp = now.ToString("o");
        }

        var entry = new Dictionary<string, object>
        {
            { config.LogLevelKey, logLevel.ToString() },
            { LoggingConstants.KeyMessage, UnformattedEntryMessage },
            { LoggingConstants.KeyTimestamp, timestamp },
            { LoggingConstants.KeyService, config.Service }
        };

        return config.Serializer.Serialize(entry, typeof(object));
    }

    /// <summary>
    /// Clear buffered logs only for the current invocation
    /// </summary>
    public void ClearCurrentInvocation()
    {
        _buffer.ClearCurrentInvocation();
    }
}