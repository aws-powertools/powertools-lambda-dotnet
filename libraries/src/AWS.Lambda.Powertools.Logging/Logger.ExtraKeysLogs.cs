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
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

public partial class Logger
{
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
    public static void LogDebug<T>(T extraKeys, EventId eventId, Exception exception, string message,
        params object[] args) where T : class
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
    public static void LogDebug<T>(T extraKeys, Exception exception, string message, params object[] args)
        where T : class
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
    public static void LogTrace<T>(T extraKeys, EventId eventId, Exception exception, string message,
        params object[] args) where T : class
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
    public static void LogTrace<T>(T extraKeys, Exception exception, string message, params object[] args)
        where T : class
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
    public static void LogInformation<T>(T extraKeys, EventId eventId, Exception exception, string message,
        params object[] args) where T : class
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
    public static void LogInformation<T>(T extraKeys, EventId eventId, string message, params object[] args)
        where T : class
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
    public static void LogInformation<T>(T extraKeys, Exception exception, string message, params object[] args)
        where T : class
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
    public static void LogWarning<T>(T extraKeys, EventId eventId, Exception exception, string message,
        params object[] args) where T : class
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
    public static void LogWarning<T>(T extraKeys, Exception exception, string message, params object[] args)
        where T : class
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
    public static void LogError<T>(T extraKeys, EventId eventId, Exception exception, string message,
        params object[] args) where T : class
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
    public static void LogError<T>(T extraKeys, Exception exception, string message, params object[] args)
        where T : class
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
    public static void LogCritical<T>(T extraKeys, EventId eventId, Exception exception, string message,
        params object[] args) where T : class
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
    public static void LogCritical<T>(T extraKeys, EventId eventId, string message, params object[] args)
        where T : class
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
    public static void LogCritical<T>(T extraKeys, Exception exception, string message, params object[] args)
        where T : class
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
    public static void Log<T>(LogLevel logLevel, T extraKeys, EventId eventId, Exception exception, string message,
        params object[] args) where T : class
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
    public static void Log<T>(LogLevel logLevel, T extraKeys, EventId eventId, string message, params object[] args)
        where T : class
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
    public static void Log<T>(LogLevel logLevel, T extraKeys, Exception exception, string message, params object[] args)
        where T : class
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
}