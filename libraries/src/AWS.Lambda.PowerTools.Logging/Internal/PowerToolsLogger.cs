using System;
using System.Collections.Generic;
using System.Text.Json;
using AWS.Lambda.PowerTools.Core;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    public sealed class PowerToolsLogger : ILogger
    {
        private readonly string _name;
        private readonly Func<LoggerConfiguration> _getCurrentConfig;
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        private readonly ISystemWrapper _systemWrapper;
        private readonly LogLevelHelper _logLevelHelper;

        private LoggerConfiguration _currentConfig;
        private LoggerConfiguration CurrentConfig => 
            _currentConfig ??= GetCurrentConfig();
        
        private LogLevel MinimumLevel => 
            CurrentConfig.MinimumLevel ?? LogLevelHelper.DefaultLogLevel;

        private string ServiceName => 
            !string.IsNullOrWhiteSpace(CurrentConfig.ServiceName)
            ? CurrentConfig.ServiceName
            : _powerToolsConfigurations.ServiceName;

        private LoggerConfiguration GetCurrentConfig()
        {
            var currConfig = _getCurrentConfig();

            var config = new LoggerConfiguration
            {
                MinimumLevel = _logLevelHelper.GetLogLevel(currConfig?.MinimumLevel),
                SamplingRate = currConfig?.SamplingRate ?? _powerToolsConfigurations.LoggerSampleRate
            };

            if (!config.SamplingRate.HasValue)
                return config;

            if (config.SamplingRate.Value < 0 || config.SamplingRate.Value > 1)
            {
                if (IsEnabled(LogLevel.Debug))
                    _systemWrapper.LogLine(
                        $"Skipping sampling rate configuration because of invalid value. Sampling rate: {config.SamplingRate.Value}");
                config.SamplingRate = null;
                return config;
            }
            
            if(config.SamplingRate.Value == 0)
                return config;

            var sample = _systemWrapper.GetRandom();
            if (config.SamplingRate.Value > sample)
            {
                if (IsEnabled(LogLevel.Debug))
                    _systemWrapper.LogLine(
                        $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {config.SamplingRate.Value}, Sampler Value: {sample}.");
                config.MinimumLevel = LogLevel.Debug;
            }

            return config;
        }

        public PowerToolsLogger(
            string name,
            Func<LoggerConfiguration> getCurrentConfig, IPowerToolsConfigurations powerToolsConfigurations,
            ISystemWrapper systemWrapper) =>
            (_name, _getCurrentConfig, _powerToolsConfigurations, _systemWrapper, _logLevelHelper) = (name,
                getCurrentConfig, powerToolsConfigurations, systemWrapper,
                new LogLevelHelper(powerToolsConfigurations));

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= MinimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (formatter is null)
                throw new ArgumentNullException(nameof(formatter));
            
            if(!IsEnabled(logLevel))
                return;

            var message = new Dictionary<string, object>(StringComparer.Ordinal);
            
            // Add Scope Variables
            foreach (var keyValuePair in Logger.GetAllKeys())
                message.TryAdd(keyValuePair.Key, keyValuePair.Value);
            
            message.TryAdd("Timestamp", DateTime.UtcNow.ToString("o"));
            message.TryAdd("Level", logLevel.ToString());
            message.TryAdd("Service", ServiceName);
            message.TryAdd("Name", _name);
            message.TryAdd("Message", formatter(state, exception));
            if(CurrentConfig.SamplingRate.HasValue)
                message.TryAdd("SamplingRate", CurrentConfig.SamplingRate.Value);
            if (exception != null)
                message.TryAdd("Exception", exception.Message);
            if(state != null)
                message.TryAdd("State", state);

            _systemWrapper.LogLine(JsonSerializer.Serialize(message));
        }
    }
}