using System.Collections.Concurrent;
using AWS.Lambda.PowerTools.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    internal sealed class LoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, PowerToolsLogger> _loggers = new();

        internal LoggerProvider(IOptions<LoggerConfiguration> config)
        {
            _currentConfig = config?.Value;
        }

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName,
                name => new PowerToolsLogger(name, 
                    PowerToolsConfigurations.Instance,
                    SystemWrapper.Instance,
                    GetCurrentConfig));

        
        private LoggerConfiguration _currentConfig;
        private LoggerConfiguration GetCurrentConfig() => _currentConfig;

        public void Dispose()
        {
            _loggers.Clear();
        }

        internal void Configure(IOptions<LoggerConfiguration> config)
        {
            if (_currentConfig is not null || config is null)
                return;
            
            _currentConfig = config.Value;
            foreach (var logger in _loggers.Values)
                logger.ClearConfig();
        }
    }
}