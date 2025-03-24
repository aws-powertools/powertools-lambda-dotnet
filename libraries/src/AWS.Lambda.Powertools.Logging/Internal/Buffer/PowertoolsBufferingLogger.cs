using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
    /// Logger implementation that supports buffering
    /// </summary>
    internal class PowertoolsBufferingLogger : ILogger
    {
        private readonly ILogger _innerLogger;
        private readonly IOptionsMonitor<PowertoolsLoggerConfiguration> _options;
        private readonly string _categoryName;
        private readonly LogBuffer _buffer = new();
        
        public PowertoolsBufferingLogger(
            ILogger innerLogger, 
            IOptionsMonitor<PowertoolsLoggerConfiguration> options,
            string categoryName)
        {
            _innerLogger = innerLogger;
            _options = options;
            _categoryName = categoryName;
        }
        
        public IDisposable BeginScope<TState>(TState state)
        {
            return _innerLogger.BeginScope(state);
        }
        
        public bool IsEnabled(LogLevel logLevel)
        {
            var options = _options.CurrentValue;
    
            // If buffering is disabled, defer to inner logger
            if (!options.LogBuffering.Enabled)
            {
                return _innerLogger.IsEnabled(logLevel);
            }
    
            // If the log level is at or above the configured minimum log level,
            // let the inner logger decide
            if (logLevel >= options.MinimumLogLevel)
            {
                return _innerLogger.IsEnabled(logLevel);
            }
    
            // For logs below minimum level but at or above buffer threshold, 
            // we should handle them (buffer them)
            if (logLevel >= options.LogBuffering.BufferAtLogLevel)
            {
                return true;
            }
    
            // Otherwise, the log level is below our buffer threshold
            return false;
        }
        
        public void Log<TState>(
            LogLevel logLevel, 
            EventId eventId, 
            TState state, 
            Exception exception, 
            Func<TState, Exception, string> formatter)
        {
            // Skip if logger is not enabled for this level
            if (!IsEnabled(logLevel))
                return;
                
            var options = _options.CurrentValue;
            var bufferOptions = options.LogBuffering;
            
            // Check if this log should be buffered
            bool shouldBuffer = bufferOptions.Enabled &&
                                logLevel >= bufferOptions.BufferAtLogLevel &&
                                logLevel < options.MinimumLogLevel;
            
            if (shouldBuffer)
            {
                // Add to buffer instead of logging
                try
                {
                    if (_innerLogger is PowertoolsLogger powertoolsLogger)
                    {
                        var logEntry = powertoolsLogger.LogEntryString(logLevel, state, exception, formatter);
                        _buffer.Add(logEntry, bufferOptions.MaxBytes);
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
                if (bufferOptions.Enabled && 
                    bufferOptions.FlushOnErrorLog && 
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
                // Get all buffered entries
                var entries = _buffer.GetAndClear();
                
                if (_innerLogger is PowertoolsLogger powertoolsLogger)
                {
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