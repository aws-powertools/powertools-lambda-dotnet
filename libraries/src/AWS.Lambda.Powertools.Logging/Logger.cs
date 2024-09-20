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
using System.Collections.Generic;
using System.Linq;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class Logger.
/// </summary>
public class Logger
{
    /// <summary>
    ///     The logger instance
    /// </summary>
    private static ILogger _loggerInstance;

    /// <summary>
    ///     Gets the logger instance.
    /// </summary>
    /// <value>The logger instance.</value>
    private static ILogger LoggerInstance => _loggerInstance ??= Create<Logger>();

    /// <summary>
    ///     Gets or sets the logger provider.
    /// </summary>
    /// <value>The logger provider.</value>
    internal static ILoggerProvider LoggerProvider { get; set; }

    /// <summary>
    ///     The logger formatter instance
    /// </summary>
    private static ILogFormatter _logFormatter;

    /// <summary>
    ///     Gets the scope.
    /// </summary>
    /// <value>The scope.</value>
    private static IDictionary<string, object> Scope { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    ///     Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
    /// <exception cref="System.ArgumentNullException">categoryName</exception>
    public static ILogger Create(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentNullException(nameof(categoryName));

        // Needed for when using Logger directly with decorator
        LoggerProvider ??= new LoggerProvider(null);
        
        return LoggerProvider.CreateLogger(categoryName);
    }

    /// <summary>
    ///     Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
    public static ILogger Create<T>()
    {
        return Create(typeof(T).FullName);
    }

    #region Scope Variables

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="System.ArgumentNullException">key</exception>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public static void AppendKey(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        Scope[key] = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(IEnumerable<KeyValuePair<string, object>> keys)
    {
        foreach (var (key, value) in keys)
            AppendKey(key, value);
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(IEnumerable<KeyValuePair<string, string>> keys)
    {
        foreach (var (key, value) in keys)
            AppendKey(key, value);
    }

    /// <summary>
    ///     Remove additional keys from the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void RemoveKeys(params string[] keys)
    {
        if (keys == null) return;
        foreach (var key in keys)
            if (Scope.ContainsKey(key))
                Scope.Remove(key);
    }

    /// <summary>
    ///     Returns all additional keys added to the log context.
    /// </summary>
    /// <returns>IEnumerable&lt;KeyValuePair&lt;System.String, System.Object&gt;&gt;.</returns>
    public static IEnumerable<KeyValuePair<string, object>> GetAllKeys()
    {
        return Scope.AsEnumerable();
    }

    /// <summary>
    ///     Removes all additional keys from the log context.
    /// </summary>
    internal static void RemoveAllKeys()
    {
        Scope.Clear();
    }
    
    internal static void ClearLoggerInstance()
    {
        _loggerInstance = null;
    }

    #endregion

    #region Core Logger Methods

    #region Debug

    /// <summary>
    ///     Formats and writes a debug log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebug(EventId eventId, Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogDebug(eventId, exception, message, args);
    }

    /// <summary>
    ///     Formats and writes a debug log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogDebug(0, "Processing request from {Address}", address)</example>
    public static void LogDebug(EventId eventId, string message, params object[] args)
    {
        LoggerInstance.LogDebug(eventId, message, args);
    }

    /// <summary>
    ///     Formats and writes a debug log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebug(Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogDebug(exception, message, args);
    }

    /// <summary>
    ///     Formats and writes a debug log message.
    /// </summary>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogDebug("Processing request from {Address}", address)</example>
    public static void LogDebug(string message, params object[] args)
    {
        LoggerInstance.LogDebug(message, args);
    }

    #endregion

    #region Trace

    /// <summary>
    ///     Formats and writes a trace log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogTrace(EventId eventId, Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogTrace(eventId, exception, message, args);
    }

    /// <summary>
    ///     Formats and writes a trace log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogTrace(0, "Processing request from {Address}", address)</example>
    public static void LogTrace(EventId eventId, string message, params object[] args)
    {
        LoggerInstance.LogTrace(eventId, message, args);
    }

    /// <summary>
    ///     Formats and writes a trace log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
    public static void LogTrace(Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogTrace(exception, message, args);
    }

    /// <summary>
    ///     Formats and writes a trace log message.
    /// </summary>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogTrace("Processing request from {Address}", address)</example>
    public static void LogTrace(string message, params object[] args)
    {
        LoggerInstance.LogTrace(message, args);
    }

    #endregion

    #region Information

    /// <summary>
    ///     Formats and writes an informational log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformation(EventId eventId, Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogInformation(eventId, exception, message, args);
    }

    /// <summary>
    ///     Formats and writes an informational log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogInformation(0, "Processing request from {Address}", address)</example>
    public static void LogInformation(EventId eventId, string message, params object[] args)
    {
        LoggerInstance.LogInformation(eventId, message, args);
    }

    /// <summary>
    ///     Formats and writes an informational log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformation(Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogInformation(exception, message, args);
    }

    /// <summary>
    ///     Formats and writes an informational log message.
    /// </summary>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogInformation("Processing request from {Address}", address)</example>
    public static void LogInformation(string message, params object[] args)
    {
        LoggerInstance.LogInformation(message, args);
    }

    #endregion

    #region Warning

    /// <summary>
    ///     Formats and writes a warning log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarning(EventId eventId, Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogWarning(eventId, exception, message, args);
    }

    /// <summary>
    ///     Formats and writes a warning log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogWarning(0, "Processing request from {Address}", address)</example>
    public static void LogWarning(EventId eventId, string message, params object[] args)
    {
        LoggerInstance.LogWarning(eventId, message, args);
    }

    /// <summary>
    ///     Formats and writes a warning log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarning(Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogWarning(exception, message, args);
    }

    /// <summary>
    ///     Formats and writes a warning log message.
    /// </summary>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogWarning("Processing request from {Address}", address)</example>
    public static void LogWarning(string message, params object[] args)
    {
        LoggerInstance.LogWarning(message, args);
    }

    #endregion

    #region Error

    /// <summary>
    ///     Formats and writes an error log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogError(EventId eventId, Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogError(eventId, exception, message, args);
    }

    /// <summary>
    ///     Formats and writes an error log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogError(0, "Processing request from {Address}", address)</example>
    public static void LogError(EventId eventId, string message, params object[] args)
    {
        LoggerInstance.LogError(eventId, message, args);
    }

    /// <summary>
    ///     Formats and writes an error log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// &gt;
    /// <example>Logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    public static void LogError(Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogError(exception, message, args);
    }

    /// <summary>
    ///     Formats and writes an error log message.
    /// </summary>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogError("Processing request from {Address}", address)</example>
    public static void LogError(string message, params object[] args)
    {
        LoggerInstance.LogError(message, args);
    }

    #endregion

    #region Critical

    /// <summary>
    ///     Formats and writes a critical log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogCritical(EventId eventId, Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogCritical(eventId, exception, message, args);
    }

    /// <summary>
    ///     Formats and writes a critical log message.
    /// </summary>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogCritical(0, "Processing request from {Address}", address)</example>
    public static void LogCritical(EventId eventId, string message, params object[] args)
    {
        LoggerInstance.LogCritical(eventId, message, args);
    }

    /// <summary>
    ///     Formats and writes a critical log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    public static void LogCritical(Exception exception, string message, params object[] args)
    {
        LoggerInstance.LogCritical(exception, message, args);
    }

    /// <summary>
    ///     Formats and writes a critical log message.
    /// </summary>
    /// <param name="message">
    ///     Format string of the log message in message template format. Example:
    ///     <c>"User {User} logged in from {Address}"</c>
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>Logger.LogCritical("Processing request from {Address}", address)</example>
    public static void LogCritical(string message, params object[] args)
    {
        LoggerInstance.LogCritical(message, args);
    }

    #endregion

    #region Log

    /// <summary>
    ///     Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void Log(LogLevel logLevel, string message, params object[] args)
    {
        LoggerInstance.Log(logLevel, message, args);
    }

    /// <summary>
    ///     Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void Log(LogLevel logLevel, EventId eventId, string message, params object[] args)
    {
        LoggerInstance.Log(logLevel, eventId, message, args);
    }

    /// <summary>
    ///     Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        LoggerInstance.Log(logLevel, exception, message, args);
    }

    /// <summary>
    ///     Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void Log(LogLevel logLevel, EventId eventId, Exception exception, string message,
        params object[] args)
    {
        LoggerInstance.Log(logLevel, eventId, exception, message, args);
    }

    /// <summary>
    ///     Writes a log entry.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">Id of the event.</param>
    /// <param name="state">The entry to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">
    ///     Function to create a <see cref="T:System.String" /> message of the <paramref name="state" />
    ///     and <paramref name="exception" />.
    /// </param>
    public static void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        LoggerInstance.Log(logLevel, eventId, state, exception, formatter);
    }

    #endregion

    #endregion

    #region JSON Logger Methods

    /// <summary>
    ///     Formats and writes a trace log message as JSON.
    /// </summary>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogTrace(new {User = user, Address = address})</example>
    public static void LogTrace(object message)
    {
        LoggerInstance.LogTrace(message);
    }

    /// <summary>
    ///     Formats and writes an trace log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogTrace(exception)</example>
    public static void LogTrace(Exception exception)
    {
        LoggerInstance.LogTrace(exception);
    }

    /// <summary>
    ///     Formats and writes a debug log message as JSON.
    /// </summary>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogDebug(new {User = user, Address = address})</example>
    public static void LogDebug(object message)
    {
        LoggerInstance.LogDebug(message);
    }

    /// <summary>
    ///     Formats and writes an debug log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogDebug(exception)</example>
    public static void LogDebug(Exception exception)
    {
        LoggerInstance.LogDebug(exception);
    }

    /// <summary>
    ///     Formats and writes an information log message as JSON.
    /// </summary>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogInformation(new {User = user, Address = address})</example>
    public static void LogInformation(object message)
    {
        LoggerInstance.LogInformation(message);
    }

    /// <summary>
    ///     Formats and writes an information log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogInformation(exception)</example>
    public static void LogInformation(Exception exception)
    {
        LoggerInstance.LogInformation(exception);
    }

    /// <summary>
    ///     Formats and writes a warning log message as JSON.
    /// </summary>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogWarning(new {User = user, Address = address})</example>
    public static void LogWarning(object message)
    {
        LoggerInstance.LogWarning(message);
    }

    /// <summary>
    ///     Formats and writes an warning log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogWarning(exception)</example>
    public static void LogWarning(Exception exception)
    {
        LoggerInstance.LogWarning(exception);
    }

    /// <summary>
    ///     Formats and writes a error log message as JSON.
    /// </summary>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogCritical(new {User = user, Address = address})</example>
    public static void LogError(object message)
    {
        LoggerInstance.LogError(message);
    }

    /// <summary>
    ///     Formats and writes an error log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogError(exception)</example>
    public static void LogError(Exception exception)
    {
        LoggerInstance.LogError(exception);
    }

    /// <summary>
    ///     Formats and writes a critical log message as JSON.
    /// </summary>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.LogCritical(new {User = user, Address = address})</example>
    public static void LogCritical(object message)
    {
        LoggerInstance.LogCritical(message);
    }

    /// <summary>
    ///     Formats and writes an critical log message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.LogCritical(exception)</example>
    public static void LogCritical(Exception exception)
    {
        LoggerInstance.LogCritical(exception);
    }

    /// <summary>
    ///     Formats and writes a log message as JSON at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">The object to be serialized as JSON.</param>
    /// <example>logger.Log(LogLevel.Information, new {User = user, Address = address})</example>
    public static void Log(LogLevel logLevel, object message)
    {
        LoggerInstance.Log(logLevel, message);
    }

    /// <summary>
    ///     Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <example>logger.Log(LogLevel.Information, exception)</example>
    public static void Log(LogLevel logLevel, Exception exception)
    {
        LoggerInstance.Log(logLevel, exception);
    }

    #endregion

    #region ExtraKeys Logger Methods

    #region Debug

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebug<T>(T extraKeys, EventId eventId, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogDebug(extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogDebug<T>(T extraKeys, EventId eventId, string message, params object[] args) where T : class
    {
        LoggerInstance.LogDebug(extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebug<T>(T extraKeys, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogDebug(extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogDebug<T>(T extraKeys, string message, params object[] args) where T : class
    {
        LoggerInstance.LogDebug(extraKeys, message, args);
    }

    #endregion

    #region Trace

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogTrace<T>(T extraKeys, EventId eventId, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogTrace(extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogTrace<T>(T extraKeys, EventId eventId, string message, params object[] args) where T : class
    {
        LoggerInstance.LogTrace(extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogTrace<T>(T extraKeys, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogTrace(extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogTrace<T>(T extraKeys, string message, params object[] args) where T : class
    {
        LoggerInstance.LogTrace(extraKeys, message, args);
    }

    #endregion

    #region Information

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformation<T>(T extraKeys, EventId eventId, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogInformation(extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogInformation<T>(T extraKeys, EventId eventId, string message, params object[] args) where T : class
    {
        LoggerInstance.LogInformation(extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformation<T>(T extraKeys, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogInformation(extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogInformation<T>(T extraKeys, string message, params object[] args) where T : class
    {
        LoggerInstance.LogInformation(extraKeys, message, args);
    }

    #endregion

    #region Warning

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarning<T>(T extraKeys, EventId eventId, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogWarning(extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogWarning<T>(T extraKeys, EventId eventId, string message, params object[] args) where T : class
    {
        LoggerInstance.LogWarning(extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarning<T>(T extraKeys, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogWarning(extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogWarning<T>(T extraKeys, string message, params object[] args) where T : class
    {
        LoggerInstance.LogWarning(extraKeys, message, args);
    }

    #endregion

    #region Error

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogError<T>(T extraKeys, EventId eventId, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogError(extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogError<T>(T extraKeys, EventId eventId, string message, params object[] args) where T : class
    {
        LoggerInstance.LogError(extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogError<T>(T extraKeys, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogError(extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogError<T>(T extraKeys, string message, params object[] args) where T : class
    {
        LoggerInstance.LogError(extraKeys, message, args);
    }

    #endregion

    #region Critical

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogCritical<T>(T extraKeys, EventId eventId, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogCritical(extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogCritical<T>(T extraKeys, EventId eventId, string message, params object[] args) where T : class
    {
        LoggerInstance.LogCritical(extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogCritical<T>(T extraKeys, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.LogCritical(extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogCritical<T>(T extraKeys, string message, params object[] args) where T : class
    {
        LoggerInstance.LogCritical(extraKeys, message, args);
    }

    #endregion

    #region Log

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Information, extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void Log<T>(LogLevel logLevel, T extraKeys, EventId eventId, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.Log(logLevel, extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Information, extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void Log<T>(LogLevel logLevel, T extraKeys, EventId eventId, string message, params object[] args) where T : class
    {
        LoggerInstance.Log(logLevel, extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Information, extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void Log<T>(LogLevel logLevel, T extraKeys, Exception exception, string message, params object[] args) where T : class
    {
        LoggerInstance.Log(logLevel, extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Information, extraKeys, "Processing request from {Address}", address)</example>
    public static void Log<T>(LogLevel logLevel, T extraKeys, string message, params object[] args) where T : class
    {
        LoggerInstance.Log(logLevel, extraKeys, message, args);
    }

    #endregion

    #endregion

    #region Custom Log Formatter

    /// <summary>
    ///     Set the log formatter.
    /// </summary>
    /// <param name="logFormatter">The log formatter.</param>
    public static void UseFormatter(ILogFormatter logFormatter)
    {
        _logFormatter = logFormatter ?? throw new ArgumentNullException(nameof(logFormatter));
    }

    /// <summary>
    ///     Set the log formatter to default.
    /// </summary>
    public static void UseDefaultFormatter()
    {
        _logFormatter = null;
    }

    /// <summary>
    ///     Returns the log formatter.
    /// </summary>
    internal static ILogFormatter GetFormatter() => _logFormatter;

    #endregion
}