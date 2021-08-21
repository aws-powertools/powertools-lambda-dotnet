using System;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging
{
    public class PowertoolsLogger : ILogger
    {
        private readonly LoggerOptions _loggerOptions;
        private static bool ColdStart = true;
        public PowertoolsLogger(LoggerOptions loggerOptions)
        {
            _loggerOptions = loggerOptions;
        }
        

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            
            LogEntry logEntry = new LogEntry()
            {
                ColdStart = ColdStart,
                FunctionArn = "functionArn",
                FunctionName = "functionName",
                FunctionMemorySize = "FunctionMemorySize",
                FunctionRequestId = "FunctionRequestId",
                Level = logLevel.ToString(),
                Location = Assembly.GetCallingAssembly().FullName,
                Message = formatter(state, exception),
                SamplingRate = _loggerOptions.SamplingRate,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            
            Console.WriteLine(JsonSerializer.Serialize(logEntry));
            
            //Console.WriteLine($"{logLevel.ToString()}, {eventId.ToString()}, {state.ToString()}, {exception.ToString()}, {formatter}");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _loggerOptions.LogLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }
}

