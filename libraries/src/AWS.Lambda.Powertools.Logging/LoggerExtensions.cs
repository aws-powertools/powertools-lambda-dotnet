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
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using LoggerExt = Microsoft.Extensions.Logging.LoggerExtensions;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class LoggerExtensions.
/// </summary>
public static class LoggerExtensions
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

    #region ExtraKeys Logger Extentions

    #region Debug
    
    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebug<T>(this ILogger logger, T extraKeys, EventId eventId, Exception exception,
        string message, params object[] args)
    {
        Log(logger, LogLevel.Debug, extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogDebug<T>(this ILogger logger, T extraKeys, EventId eventId, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Debug, extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebug<T>(this ILogger logger, T extraKeys, Exception exception, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Debug, extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogDebug<T>(this ILogger logger, T extraKeys, string message, params object[] args)
    {
        Log(logger, LogLevel.Debug, extraKeys, message, args);
    }
    
    #endregion

    #region Trace
    
    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogTrace<T>(this ILogger logger, T extraKeys, EventId eventId, Exception exception,
        string message, params object[] args)
    {
        Log(logger, LogLevel.Trace, extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogTrace<T>(this ILogger logger, T extraKeys, EventId eventId, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Trace, extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogTrace<T>(this ILogger logger, T extraKeys, Exception exception, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Trace, extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogTrace<T>(this ILogger logger, T extraKeys, string message, params object[] args)
    {
        Log(logger, LogLevel.Trace, extraKeys, message, args);
    }

    #endregion

    #region Information
    
    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformation<T>(this ILogger logger, T extraKeys, EventId eventId, Exception exception,
        string message, params object[] args)
    {
        Log(logger, LogLevel.Information, extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogInformation<T>(this ILogger logger, T extraKeys, EventId eventId, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Information, extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformation<T>(this ILogger logger, T extraKeys, Exception exception, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Information, extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogInformation<T>(this ILogger logger, T extraKeys, string message, params object[] args)
    {
        Log(logger, LogLevel.Information, extraKeys, message, args);
    }
    
    #endregion

    #region Warning

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarning<T>(this ILogger logger, T extraKeys, EventId eventId, Exception exception,
        string message, params object[] args)
    {
        Log(logger, LogLevel.Warning, extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogWarning<T>(this ILogger logger, T extraKeys, EventId eventId, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Warning, extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarning<T>(this ILogger logger, T extraKeys, Exception exception, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Warning, extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogWarning<T>(this ILogger logger, T extraKeys, string message, params object[] args)
    {
        Log(logger, LogLevel.Warning, extraKeys, message, args);
    }
    
    #endregion

    #region Error

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogError<T>(this ILogger logger, T extraKeys, EventId eventId, Exception exception,
        string message, params object[] args)
    {
        Log(logger, LogLevel.Error, extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogError<T>(this ILogger logger, T extraKeys, EventId eventId, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Error, extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogError<T>(this ILogger logger, T extraKeys, Exception exception, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Error, extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogError<T>(this ILogger logger, T extraKeys, string message, params object[] args)
    {
        Log(logger, LogLevel.Error, extraKeys, message, args);
    }

    #endregion

    #region Critical

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogCritical<T>(this ILogger logger, T extraKeys, EventId eventId, Exception exception,
        string message, params object[] args)
    {
        Log(logger, LogLevel.Critical, extraKeys, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void LogCritical<T>(this ILogger logger, T extraKeys, EventId eventId, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Critical, extraKeys, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void LogCritical<T>(this ILogger logger, T extraKeys, Exception exception, string message,
        params object[] args)
    {
        Log(logger, LogLevel.Critical, extraKeys, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(extraKeys, "Processing request from {Address}", address)</example>
    public static void LogCritical<T>(this ILogger logger, T extraKeys, string message, params object[] args)
    {
        Log(logger, LogLevel.Critical, extraKeys, message, args);
    }

    #endregion

    #region Log

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Information, extraKeys, 0, exception, "Error while processing request from {Address}", address)</example>
    public static void Log<T>(this ILogger logger, LogLevel logLevel, T extraKeys, EventId eventId, Exception exception,
        string message, params object[] args)
    {
        if (extraKeys is Exception ex && exception is null)
            LoggerExt.Log(logger, logLevel, eventId, ex, message, args);
        else if (extraKeys is EventId evid && evid == 0)
            LoggerExt.Log(logger, logLevel, evid, exception, message, args);
        else if (extraKeys is not null)
            using (logger.BeginScope(extraKeys))
                LoggerExt.Log(logger, logLevel, eventId, exception, message, args);
        else
            LoggerExt.Log(logger, logLevel, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Information, extraKeys, 0, "Processing request from {Address}", address)</example>
    public static void Log<T>(this ILogger logger, LogLevel logLevel, T extraKeys, EventId eventId, string message,
        params object[] args)
    {
        Log(logger, logLevel, extraKeys, eventId, null, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Information, extraKeys, exception, "Error while processing request from {Address}", address)</example>
    public static void Log<T>(this ILogger logger, LogLevel logLevel, T extraKeys, Exception exception, string message,
        params object[] args)
    {
        Log(logger, logLevel, extraKeys, 0, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="extraKeys">Additional keys will be appended to the log entry.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.Log(LogLevel.Information, extraKeys, "Processing request from {Address}", address)</example>
    public static void Log<T>(this ILogger logger, LogLevel logLevel, T extraKeys, string message, params object[] args)
    {
        Log(logger, logLevel, extraKeys, 0, null, message, args);
    }
    
    #endregion

    #endregion
}