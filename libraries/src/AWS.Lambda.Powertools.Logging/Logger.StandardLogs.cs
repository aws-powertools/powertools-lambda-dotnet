using System;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

public static partial class Logger
{
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

}