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

public static partial class Logger
{
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
}
