using System.Collections.Concurrent;
using AWS.Lambda.PowerTools.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    public sealed class LoggerProvider : ILoggerProvider
    {
        private readonly LoggerConfiguration _currentConfig;
        private readonly ConcurrentDictionary<string, PowerToolsLogger> _loggers = new();

        public LoggerProvider(IOptions<LoggerConfiguration> config)
        {
            _currentConfig = config.Value;
        }

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName,
                name => new PowerToolsLogger(name, GetCurrentConfig, 
                    PowerToolsConfigurations.Instance,
                    SystemWrapper.Instance));

        private LoggerConfiguration GetCurrentConfig() => _currentConfig;

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}