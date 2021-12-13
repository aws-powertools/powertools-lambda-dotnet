using AWS.Lambda.PowerTools.Logging.Internal;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging
{
    public static class LoggerExtensions
    {
        public static void LogTrace(this ILogger logger, object message)
        {
            logger.LogTrace(LoggingConstants.KeyJsonFormatter, message);
        }
        
        public static void LogDebug(this ILogger logger, object message)
        {
            logger.LogDebug(LoggingConstants.KeyJsonFormatter, message);
        }
        
        public static void LogInformation(this ILogger logger, object message)
        {
            logger.LogInformation(LoggingConstants.KeyJsonFormatter, message);
        }
        
        public static void LogWarning(this ILogger logger, object message)
        {
            logger.LogWarning(LoggingConstants.KeyJsonFormatter, message);
        }
        
        public static void LogError(this ILogger logger, object message)
        {
            logger.LogError(LoggingConstants.KeyJsonFormatter, message);
        }
        
        public static void LogCritical(this ILogger logger, object message)
        {
            logger.LogCritical(LoggingConstants.KeyJsonFormatter, message);
        }
        
        public static void Log(this ILogger logger, LogLevel logLevel, object message)
        {
            logger.Log(logLevel, LoggingConstants.KeyJsonFormatter, message);
        }
    }
}