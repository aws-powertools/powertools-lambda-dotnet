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

        public static ILogger Create(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentNullException(nameof(categoryName));
                    
            if (LoggerProvider == null)
                throw new ArgumentNullException(nameof(LoggerProvider));
         
            return LoggerProvider.CreateLogger(categoryName);
        }

        public static ILogger Create<T>()
        {
            return Create(typeof(T).FullName);
        }

        #region Scope Variables

        public static void AppendKey(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key)) 
                return;
            
            if (_scope.ContainsKey(key))
                _scope[key] = value;
            else
                _scope.Add(key, value);
        }

        public static void AppendKeys(IDictionary<string, object> keys)
        {
            foreach (var keyValuePair in keys)
                AppendKey(keyValuePair.Key, keyValuePair.Value);
        }

        public static void RemoveKey(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && _scope.ContainsKey(key))
                _scope.Remove(key);
        }

        public static void RemoveKeys(params string[] keys)
        {
            if (keys == null) return;
            foreach (var key in keys)
                RemoveKey(key);
        }

        public static IEnumerable<KeyValuePair<string, object>> GetAllKeys()
        {
            return _scope.AsEnumerable();
        }
        
        internal static void RemoveAllKeys()
        {
            _scope.Clear();
        }

        #endregion
        
        #region Logger Functions

        #region Debug
        
        public static void LogDebug(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogDebug(eventId, exception, message, args);
        }

        public static void LogDebug(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogDebug(eventId, message, args);
        }

        public static void LogDebug(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogDebug(exception, message, args);
        }

        public static void LogDebug(string message, params object[] args)
        {
            LoggerInstance.LogDebug(message, args);
        }
        
        public static void LogDebug(object message)
        {
            LoggerInstance.LogDebug(message);
        }

        #endregion
        
        #region Trace

        public static void LogTrace(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogTrace(eventId, exception, message, args);
        }
        
        public static void LogTrace(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogTrace(eventId, message, args);
        }
        
        public static void LogTrace(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogTrace(exception, message, args);
        }
        
        public static void LogTrace(string message, params object[] args)
        {
            LoggerInstance.LogTrace(message, args);
        }
        
        public static void LogTrace(object message)
        {
            LoggerInstance.LogTrace(message);
        }
        
        #endregion
        
        #region Information

        public static void LogInformation(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogInformation(eventId, exception, message, args);
        }
        
        public static void LogInformation(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogInformation(eventId, message, args);
        }
        
        public static void LogInformation(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogInformation(exception, message, args);
        }
        
        public static void LogInformation(string message, params object[] args)
        {
            LoggerInstance.LogInformation(message, args);
        }
        
        public static void LogInformation(object message)
        {
            LoggerInstance.LogInformation(message);
        }

        #endregion
        
        #region Warning
        
        public static void LogWarning(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogWarning(eventId, exception, message, args);
        }
        
        public static void LogWarning(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogWarning(eventId, message, args);
        }
        
        public static void LogWarning(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogWarning(exception, message, args);
        }
        
        public static void LogWarning(string message, params object[] args)
        {
            LoggerInstance.LogWarning(message, args);
        }
        
        public static void LogWarning(object message)
        {
            LoggerInstance.LogWarning(message);
        }
        
        #endregion
        
        #region Error
        
        public static void LogError(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogError(eventId, exception, message, args);
        }
        
        public static void LogError(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogError(eventId, message, args);
        }
        
        public static void LogError(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogError(exception, message, args);
        }
        
        public static void LogError(string message, params object[] args)
        {
            LoggerInstance.LogError(message, args);
        }
        
        public static void LogError(object message)
        {
            LoggerInstance.LogError(message);
        }
        
        #endregion
        
        #region Critical
        
        public static void LogCritical(EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogCritical(eventId, exception, message, args);
        }
        
        public static void LogCritical(EventId eventId, string message, params object[] args)
        {
            LoggerInstance.LogCritical(eventId, message, args);
        }
        
        public static void LogCritical(Exception exception, string message, params object[] args)
        {
            LoggerInstance.LogCritical(exception, message, args);
        }
        
        public static void LogCritical(string message, params object[] args)
        {
            LoggerInstance.LogCritical(message, args);
        }
        
        public static void LogCritical(object message)
        {
            LoggerInstance.LogCritical(message);
        }
        
        #endregion
        
        #region Log
        
        public static void Log(LogLevel logLevel, string message, params object[] args)
        {
            LoggerInstance.Log(logLevel, message, args);
        }
        
        public static void Log(LogLevel logLevel, EventId eventId, string message, params object[] args)
        {
            LoggerInstance.Log(logLevel, eventId, message, args);
        }
        
        public static void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
        {
            LoggerInstance.Log(logLevel, exception, message, args);
        }
        
        public static void Log(LogLevel logLevel, EventId eventId, Exception exception, string message, params object[] args)
        {
            LoggerInstance.Log(logLevel, eventId, exception, message, args);
        }

        public static void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            LoggerInstance.Log(logLevel, eventId, state, exception, formatter);
        }
        
        public static void Log(LogLevel logLevel, object message)
        {
            LoggerInstance.Log(logLevel, message);
        }
        
        #endregion
        
        #endregion
    }
}