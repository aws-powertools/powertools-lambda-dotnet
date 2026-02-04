using System;
using System.Collections.Generic;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class LoggerExtensions.
/// </summary>
public static class PowertoolsLoggerExtensions
{
    #region JSON Logger Extentions

    /// <summary>
    ///     Formats and writes a trace log message as JSON.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogTrace(new {User = user, Address = address})</example>
    public static void LogTrace(this ILogger logger, object message)
    {
        logger.LogTrace(LoggingConstants.KeyJsonFormatter, message);
    }

    /// <summary>
    ///     Formats and writes an trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogTrace(exception)</example>
    public static void LogTrace(this ILogger logger, Exception exception)
    {
        logger.LogTrace(exception: exception, message: exception.Message);
    }

    /// <summary>
    ///     Formats and writes a debug log message as JSON.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogDebug(new {User = user, Address = address})</example>
    public static void LogDebug(this ILogger logger, object message)
    {
        logger.LogDebug(LoggingConstants.KeyJsonFormatter, message);
    }

    /// <summary>
    ///     Formats and writes an debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogDebug(exception)</example>
    public static void LogDebug(this ILogger logger, Exception exception)
    {
        logger.LogDebug(exception: exception, message: exception.Message);
    }

    /// <summary>
    ///     Formats and writes an information log message as JSON.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogInformation(new {User = user, Address = address})</example>
    public static void LogInformation(this ILogger logger, object message)
    {
        logger.LogInformation(LoggingConstants.KeyJsonFormatter, message);
    }

    /// <summary>
    ///     Formats and writes an information log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogInformation(exception)</example>
    public static void LogInformation(this ILogger logger, Exception exception)
    {
        logger.LogInformation(exception: exception, message: exception.Message);
    }

    /// <summary>
    ///     Formats and writes a warning log message as JSON.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogWarning(new {User = user, Address = address})</example>
    public static void LogWarning(this ILogger logger, object message)
    {
        logger.LogWarning(LoggingConstants.KeyJsonFormatter, message);
    }

    /// <summary>
    ///     Formats and writes an warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogWarning(exception)</example>
    public static void LogWarning(this ILogger logger, Exception exception)
    {
        logger.LogWarning(exception: exception, message: exception.Message);
    }

    /// <summary>
    ///     Formats and writes a error log message as JSON.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogCritical(new {User = user, Address = address})</example>
    public static void LogError(this ILogger logger, object message)
    {
        logger.LogError(LoggingConstants.KeyJsonFormatter, message);
    }

    /// <summary>
    ///     Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogError(exception)</example>
    public static void LogError(this ILogger logger, Exception exception)
    {
        logger.LogError(exception: exception, message: exception.Message);
    }

    /// <summary>
    ///     Formats and writes a critical log message as JSON.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogCritical(new {User = user, Address = address})</example>
    public static void LogCritical(this ILogger logger, object message)
    {
        logger.LogCritical(LoggingConstants.KeyJsonFormatter, message);
    }

    /// <summary>
    ///     Formats and writes an critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogCritical(exception)</example>
    public static void LogCritical(this ILogger logger, Exception exception)
    {
        logger.LogCritical(exception: exception, message: exception.Message);
    }

    /// <summary>
    ///     Formats and writes a log message as JSON at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.Log(LogLevel.Information, new {User = user, Address = address})</example>
    public static void Log(this ILogger logger, LogLevel logLevel, object message)
    {
        logger.Log(logLevel, LoggingConstants.KeyJsonFormatter, message);
    }

    /// <summary>
    ///     Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" /> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.Log(LogLevel.Information, exception)</example>
    public static void Log(this ILogger logger, LogLevel logLevel, Exception exception)
    {
        logger.Log(logLevel, exception: exception, message: exception.Message);
    }

    #endregion
    
    /// <summary>
    ///     Gets the correlation identifier from the log context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The correlation identifier, or null if not set.</returns>
    public static string GetCorrelationId(this ILogger logger)
    {
        return Logger.CorrelationId;
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(this ILogger logger,IEnumerable<KeyValuePair<string, object>> keys)
    {
        Logger.AppendKeys(keys);
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(this ILogger logger,IEnumerable<KeyValuePair<string, string>> keys)
    {
        Logger.AppendKeys(keys);
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="System.ArgumentNullException">key</exception>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public static void AppendKey(this ILogger logger, string key, object value)
    {
        Logger.AppendKey(key, value);
    }
    
    /// <summary>
    ///     Returns all additional keys added to the log context.
    /// </summary>
    /// <returns>IEnumerable&lt;KeyValuePair&lt;System.String, System.Object&gt;&gt;.</returns>
    public static IEnumerable<KeyValuePair<string, object>>     GetAllKeys(this ILogger logger)
    {
        return Logger.GetAllKeys();
    }
    
    /// <summary>
    ///     Removes all additional keys from the log context.
    /// </summary>
    internal static void RemoveAllKeys(this ILogger logger)
    {
        Logger.RemoveAllKeys();
    }

    /// <summary>
    ///     Remove additional keys from the log context.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="keys">The list of keys.</param>
    public static void RemoveKeys(this ILogger logger, params string[] keys)
    {
        Logger.RemoveKeys(keys);
    }
    
    /// <summary>
    ///     Removes a key from the log context.
    /// </summary>
    public static void RemoveKey(this ILogger logger, string key)
    {
        Logger.RemoveKey(key);
    }

    /// <summary>
    ///     Adds temporary keys to the log context that are automatically removed when disposed.
    ///     Safe to use across async/await boundaries.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="keys">The keys to add temporarily.</param>
    /// <returns>An IDisposable that removes the keys when disposed.</returns>
    /// <example>
    /// <code>
    /// using (logger.ExtraKeys(new Dictionary&lt;string, object&gt; { {"orderId", "123"} }))
    /// {
    ///     await ProcessOrderAsync();
    ///     logger.LogInformation("Order processed"); // includes orderId
    /// }
    /// // orderId is automatically removed
    /// </code>
    /// </example>
    public static IDisposable ExtraKeys(this ILogger logger, IEnumerable<KeyValuePair<string, object>> keys)
    {
        return Logger.ExtraKeys(keys);
    }

    /// <summary>
    ///     Adds temporary keys to the log context that are automatically removed when disposed.
    ///     Safe to use across async/await boundaries.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="keys">The keys to add temporarily as tuples.</param>
    /// <returns>An IDisposable that removes the keys when disposed.</returns>
    /// <example>
    /// <code>
    /// using (logger.ExtraKeys(("orderId", "123"), ("customerId", "456")))
    /// {
    ///     logger.LogInformation("Processing"); // includes orderId and customerId
    /// }
    /// </code>
    /// </example>
    public static IDisposable ExtraKeys(this ILogger logger, params (string Key, object Value)[] keys)
    {
        return Logger.ExtraKeys(keys);
    }

    // Replace the buffer methods with direct calls to the manager

    /// <summary>
    /// Flush any buffered logs
    /// </summary>
    public static void FlushBuffer(this ILogger logger)
    {
        // Direct call to the buffer manager to avoid any recursion
        LogBufferManager.FlushCurrentBuffer();
    }

    /// <summary>
    /// Clear any buffered logs without writing them
    /// </summary>
    public static void ClearBuffer(this ILogger logger)
    {
        // Direct call to the buffer manager to avoid any recursion
        LogBufferManager.ClearCurrentBuffer();
    }
    
    /// <summary>
    ///   Refresh the sampling calculation and update the minimum log level if needed
    /// </summary>
    /// <returns></returns>
    public static bool RefreshSampleRateCalculation(this ILogger logger)
    {
        return Logger.RefreshSampleRateCalculation();
    }
}