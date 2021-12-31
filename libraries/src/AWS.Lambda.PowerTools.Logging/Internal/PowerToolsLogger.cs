/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AWS.Lambda.PowerTools.Core;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    internal sealed class PowerToolsLogger : ILogger
    {
        private readonly string _name;
        private readonly Func<LoggerConfiguration> _getCurrentConfig;
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        private readonly ISystemWrapper _systemWrapper;

        private LoggerConfiguration _currentConfig;
        private LoggerConfiguration CurrentConfig => 
            _currentConfig ??= GetCurrentConfig();

        internal void ClearConfig()
        {
            _currentConfig = null;
        }
        
        private LogLevel MinimumLevel => 
            CurrentConfig.MinimumLevel ?? LoggingConstants.DefaultLogLevel;

        private string Service => 
            !string.IsNullOrWhiteSpace(CurrentConfig.Service)
            ? CurrentConfig.Service
            : _powerToolsConfigurations.Service;

        private LoggerConfiguration GetCurrentConfig()
        {
            var currConfig = _getCurrentConfig();
            var minimumLevel = _powerToolsConfigurations.GetLogLevel(currConfig?.MinimumLevel);
            var samplingRate = currConfig?.SamplingRate ?? _powerToolsConfigurations.LoggerSampleRate;

            var config = new LoggerConfiguration
            {
                Service = currConfig?.Service,
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
            message.TryAdd(LoggingConstants.KeyService, Service);
            message.TryAdd(LoggingConstants.KeyLoggerName, _name);
            message.TryAdd(LoggingConstants.KeyMessage,
                CustomFormatter(state, exception, out var customMessage) && customMessage is not null
                    ? customMessage
                    : formatter(state, exception));
            if(CurrentConfig.SamplingRate.HasValue)
                message.TryAdd(LoggingConstants.KeySamplingRate, CurrentConfig.SamplingRate.Value);
            if (exception != null)
                message.TryAdd(LoggingConstants.KeyException, exception.Message);

            _systemWrapper.LogLine(JsonSerializer.Serialize(message));
        }

        private static bool CustomFormatter<TState>(TState state, Exception exception, out object message)
        {
            message = null;
            if (exception is not null)
                return false;

            var stateKeys = (state as IEnumerable<KeyValuePair<string, object>>)?
                .ToDictionary(i => i.Key, i => i.Value);

            if (stateKeys is null || stateKeys.Count != 2)
                return false;

            if (!stateKeys.TryGetValue("{OriginalFormat}", out var originalFormat))
                return false;
            
            if(originalFormat?.ToString() != LoggingConstants.KeyJsonFormatter)
                return false;

            message = stateKeys.First(k => k.Key != "{OriginalFormat}").Value;
            return true;
        }
    }
}