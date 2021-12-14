using AWS.Lambda.PowerTools.Logging.Internal;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging
{
    public static class LoggerExtensions
    {
        /// <summary>
        /// Formats and writes a trace log message as JSON.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>logger.LogTrace(new {User = user, Address = address})</example>
        public static void LogTrace(this ILogger logger, object message)
        {
            logger.LogTrace(LoggingConstants.KeyJsonFormatter, message);
        }
        
        /// <summary>
        /// Formats and writes a debug log message as JSON.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>logger.LogDebug(new {User = user, Address = address})</example>
        public static void LogDebug(this ILogger logger, object message)
        {
            logger.LogDebug(LoggingConstants.KeyJsonFormatter, message);
        }
        
        /// <summary>
        /// Formats and writes an information log message as JSON.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>logger.LogInformation(new {User = user, Address = address})</example>
        public static void LogInformation(this ILogger logger, object message)
        {
            logger.LogInformation(LoggingConstants.KeyJsonFormatter, message);
        }
        
        /// <summary>
        /// Formats and writes a warning log message as JSON.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>logger.LogWarning(new {User = user, Address = address})</example>
        public static void LogWarning(this ILogger logger, object message)
        {
            logger.LogWarning(LoggingConstants.KeyJsonFormatter, message);
        }
        
        /// <summary>
        /// Formats and writes an error log message as JSON.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>logger.LogError(new {User = user, Address = address})</example>
        public static void LogError(this ILogger logger, object message)
        {
            logger.LogError(LoggingConstants.KeyJsonFormatter, message);
        }
        
        /// <summary>
        /// Formats and writes a critical log message as JSON.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>logger.LogCritical(new {User = user, Address = address})</example>
        public static void LogCritical(this ILogger logger, object message)
        {
            logger.LogCritical(LoggingConstants.KeyJsonFormatter, message);
        }
        
        /// <summary>
        /// Formats and writes a log message as JSON at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="message">The object to be serialized as JSON.</param>
        /// <example>logger.Log(LogLevel.Information, new {User = user, Address = address})</example>
        public static void Log(this ILogger logger, LogLevel logLevel, object message)
        {
            logger.Log(logLevel, LoggingConstants.KeyJsonFormatter, message);
        }
    }
}