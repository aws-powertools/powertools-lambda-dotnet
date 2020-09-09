using System;
using Microsoft.Extensions.Logging;

namespace Amazon.LambdaPowertools.Logging
{
    public class PowertoolsLogger : ILogger
    {
        private readonly LoggerOptions _loggerOptions;
        private PowertoolsLogger(LoggerOptions loggerOptions)
        {
            _loggerOptions = loggerOptions;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }
}