using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging
{
    public class Logger
    {
        private static ILogger _loggerInstance;
        private static ILogger LoggerInstance => _loggerInstance ??= Create<Logger>();
        internal static ILoggerProvider LoggerProvider { get; set; }
        private static IDictionary<string, object> _scope { get; } = new Dictionary<string, object>(StringComparer.Ordinal);
        
        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the categoryName is not provided.</exception>
        /// <exception cref="LoggerException">Thrown when the [Logging] attribute is not added to the Lambda handler.</exception>
        public static ILogger Create(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentNullException(nameof(categoryName));

            if (LoggerProvider == null)
                throw new LoggerException(
                    "LoggerProvider has not been initialized. Add [Logging] attribute to the Lambda handler");
         
            return LoggerProvider.CreateLogger(categoryName);
        }

        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
        /// </summary>
        /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
        /// <exception cref="LoggerException">Thrown when the [Logging] attribute is not added to the Lambda handler.</exception>
        public static ILogger Create<T>()
        {
            return Create(typeof(T).FullName);
        }

        #region Scope Variables

        /// <summary>
        /// Appending additional key to the log context.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">Thrown when the key is not provided.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the value is not provided.</exception>
        public static void AppendKey(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (_scope.ContainsKey(key))
                _scope[key] = value;
            else
                _scope.Add(key, value);
        }

        /// <summary>
        /// Appending additional key to the log context.
        /// </summary>
        /// <param name="keys">The list of keys.</param>
        public static void AppendKeys(IDictionary<string, object> keys)
        {
            foreach (var (key, value) in keys)
                AppendKey(key, value);
        }

        /// <summary>
        /// Remove additional key from the log context.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <exception cref="ArgumentNullException">Thrown when the key is not provided.</exception>
        public static void RemoveKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (_scope.ContainsKey(key))
                _scope.Remove(key);
        }

        
        /// <summary>
        /// Remove additional keys from the log context.
        /// </summary>
        /// <param name="keys">The list of keys.</param>
        public static void RemoveKeys(params string[] keys)
        {
            if (keys == null) return;
            foreach (var key in keys)
                RemoveKey(key);
        }

        /// <summary>
        /// Returns all additional keys added to the log context.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, object>> GetAllKeys()
        {
            return _scope.AsEnumerable();
        }
        
        /// <summary>
        /// Removes all additional keys from the log context.
        /// </summary>
        internal static void RemoveAllKeys()
        {
            _scope.Clear();
        }

        #endregion
        
        #region Logger Functions

        #region Debug
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogDebug(eventId, exception, message, args);
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogDebug(0, "Processing request from {Address}", address)</example>
        public static void LogDebug(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogDebug(eventId, message, args);
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogDebug(exception, message, args);
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogDebug("Processing request from {Address}", address)</example>
        public static void LogDebug(string message, params object[] args)
        {
            LoggerInstance.LogDebug(message, args);
        }
        
        /// <summary>
        /// Formats and writes a debug log message as JSON.
        /// </summary>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>Logger.LogDebug(new {User = user, Address = address})</example>
        public static void LogDebug(object message)
        {
            LoggerInstance.LogDebug(message);
        }

        #endregion
        
        #region Trace

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogTrace(eventId, exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogTrace(0, "Processing request from {Address}", address)</example>
        public static void LogTrace(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogTrace(eventId, message, args);
        }
        
        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogTrace(exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogTrace("Processing request from {Address}", address)</example>
        public static void LogTrace(string message, params object[] args)
        {
            LoggerInstance.LogTrace(message, args);
        }
        
        /// <summary>
        /// Formats and writes a trace log message as JSON.
        /// </summary>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>Logger.LogTrace(new {User = user, Address = address})</example>
        public static void LogTrace(object message)
        {
            LoggerInstance.LogTrace(message);
        }
        
        #endregion
        
        #region Information

        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogInformation(eventId, exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogInformation(0, "Processing request from {Address}", address)</example>
        public static void LogInformation(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogInformation(eventId, message, args);
        }
        
        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogInformation(exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogInformation("Processing request from {Address}", address)</example>
        public static void LogInformation(string message, params object[] args)
        {
            LoggerInstance.LogInformation(message, args);
        }
        
        /// <summary>
        /// Formats and writes an informational log message as JSON.
        /// </summary>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>Logger.LogInformation(new {User = user, Address = address})</example>
        public static void LogInformation(object message)
        {
            LoggerInstance.LogInformation(message);
        }

        #endregion
        
        #region Warning
        
        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogWarning(eventId, exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogWarning(0, "Processing request from {Address}", address)</example>
        public static void LogWarning(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogWarning(eventId, message, args);
        }
        
        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogWarning(exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogWarning("Processing request from {Address}", address)</example>
        public static void LogWarning(string message, params object[] args)
        {
            LoggerInstance.LogWarning(message, args);
        }
        
        /// <summary>
        /// Formats and writes a warning log message as JSON.
        /// </summary>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>Logger.LogWarning(new {User = user, Address = address})</example>
        public static void LogWarning(object message)
        {
            LoggerInstance.LogWarning(message);
        }
        
        #endregion
        
        #region Error
        
        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogError(eventId, exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogError(0, "Processing request from {Address}", address)</example>
        public static void LogError(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogError(eventId, message, args);
        }
        
        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogError(exception, "Error while processing request from {Address}", address)</example>
        public static void LogError(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogError(exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogError("Processing request from {Address}", address)</example>
        public static void LogError(string message, params object[] args)
        {
            LoggerInstance.LogError(message, args);
        }
        
        /// <summary>
        /// Formats and writes an error log message as JSON.
        /// </summary>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>Logger.LogError(new {User = user, Address = address})</example>
        public static void LogError(object message)
        {
            LoggerInstance.LogError(message);
        }
        
        #endregion
        
        #region Critical
        
        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogCritical(eventId, exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogCritical(0, "Processing request from {Address}", address)</example>
        public static void LogCritical(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogCritical(eventId, message, args);
        }
        
        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogCritical(exception, message, args);
        }
        
        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>Logger.LogCritical("Processing request from {Address}", address)</example>
        public static void LogCritical(string message, params object[] args)
        {
            LoggerInstance.LogCritical(message, args);
        }
        
        /// <summary>
        /// Formats and writes a critical log message as JSON.
        /// </summary>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>Logger.LogCritical(new {User = user, Address = address})</example>
        public static void LogCritical(object message)
        {
            LoggerInstance.LogCritical(message);
        }
        
        #endregion
        
        #region Log
        
        /// <summary>
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(LogLevel logLevel, string message, params object[] args)
        {
            LoggerInstance.Log(logLevel, message, args);
        }
        
        /// <summary>
        /// Formats and writes a log message at the specified log level.
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
        /// Formats and writes a log message at the specified log level.
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
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(LogLevel logLevel, EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.Log(logLevel, eventId, exception, message, args);
        }

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <see cref="T:System.String" /> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        public static void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            LoggerInstance.Log(logLevel, eventId, state, exception, formatter);
        }
        
        /// <summary>
        /// Formats and writes a log message as JSON at the specified log level.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>Logger.Log(LogLevel.Information, new {User = user, Address = address})</example>
        public static void Log(LogLevel logLevel, object message)
        {
            LoggerInstance.Log(logLevel, message);
        }
        
        #endregion
        
        #endregion
    }
}