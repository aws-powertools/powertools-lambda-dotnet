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

        private LoggerConfiguration _currentConfig;
        private LoggerConfiguration CurrentConfig => 
            _currentConfig ??= GetCurrentConfig();
        
        private LogLevel MinimumLevel => 
            CurrentConfig.MinimumLevel ?? LoggingConstants.DefaultLogLevel;

        private string ServiceName => 
            !string.IsNullOrWhiteSpace(CurrentConfig.ServiceName)
            ? CurrentConfig.ServiceName
            : _powerToolsConfigurations.ServiceName;

        private LoggerConfiguration GetCurrentConfig()
        {
            var currConfig = _getCurrentConfig();
            var minimumLevel = _powerToolsConfigurations.GetLogLevel(currConfig?.MinimumLevel);
            var samplingRate = currConfig?.SamplingRate ?? _powerToolsConfigurations.LoggerSampleRate;

            var config = new LoggerConfiguration
            {
                ServiceName = currConfig?.ServiceName,
                MinimumLevel = minimumLevel,
                SamplingRate = samplingRate
            };

            if (!samplingRate.HasValue)
                return config;

            if (samplingRate.Value < 0 || samplingRate.Value > 1)
            {
                if (minimumLevel is LogLevel.Debug or LogLevel.Trace)
                    _systemWrapper.LogLine(
                        $"Skipping sampling rate configuration because of invalid value. Sampling rate: {samplingRate.Value}");
                config.SamplingRate = null;
                return config;
            }
            
            if(samplingRate.Value == 0)
                return config;

            var sample = _systemWrapper.GetRandom();
            if (samplingRate.Value > sample)
            {
                _systemWrapper.LogLine(
                    $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {samplingRate.Value}, Sampler Value: {sample}.");
                config.MinimumLevel = LogLevel.Debug;
            }

            return config;
        }

        public PowerToolsLogger(
            string name,
            IPowerToolsConfigurations powerToolsConfigurations,
            ISystemWrapper systemWrapper,
            Func<LoggerConfiguration> getCurrentConfig) =>
            (_name, _powerToolsConfigurations, _systemWrapper, _getCurrentConfig) = (name,
                powerToolsConfigurations, systemWrapper, getCurrentConfig);

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

            // Add Custom Keys
            foreach (var (key, value) in Logger.GetAllKeys())
                message.TryAdd(key, value);
            
            // Add Lambda Context Keys
            foreach (var (key, value) in LoggingAspectHandler.GetLambdaContextKeys())
                message.TryAdd(key, value);
            
            message.TryAdd(LoggingConstants.KeyTimestamp, DateTime.UtcNow.ToString("o"));
            message.TryAdd(LoggingConstants.KeyLogLevel, logLevel.ToString());
            message.TryAdd(LoggingConstants.KeyServiceName, ServiceName);
            message.TryAdd(LoggingConstants.KeyLoggerName, _name);
            message.TryAdd(LoggingConstants.KeyMessage, formatter(state, exception));
            if(CurrentConfig.SamplingRate.HasValue)
                message.TryAdd(LoggingConstants.KeySamplingRate, CurrentConfig.SamplingRate.Value);
            if (exception != null)
                message.TryAdd(LoggingConstants.KeyException, exception.Message);
            if(state != null)
                message.TryAdd(LoggingConstants.KeyState, state);

            _systemWrapper.LogLine(JsonSerializer.Serialize(message));
        }
    }
}