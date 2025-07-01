using System;
using System.Collections.Concurrent;
using AWS.Lambda.Powertools.Common;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class LoggerProvider. This class cannot be inherited.
///     Implements the <see cref="T:Microsoft.Extensions.Logging.ILoggerProvider" />
/// </summary>
/// <seealso cref="T:Microsoft.Extensions.Logging.ILoggerProvider" />
[ProviderAlias("PowertoolsLogger")]
internal class PowertoolsLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, PowertoolsLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private PowertoolsLoggerConfiguration _currentConfig;
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;
    private bool _environmentConfigured;

    public PowertoolsLoggerProvider(
        PowertoolsLoggerConfiguration config,
        IPowertoolsConfigurations powertoolsConfigurations)
    {
        _powertoolsConfigurations = powertoolsConfigurations;
        _currentConfig = config;
        
        // Set execution environment
        _powertoolsConfigurations.SetExecutionEnvironment(this);
        
        // Apply environment configurations if available
        ConfigureFromEnvironment();
    }

    public void ConfigureFromEnvironment()
    {
        var logLevel = _powertoolsConfigurations.GetLogLevel(_currentConfig.MinimumLogLevel);
        var lambdaLogLevel = _powertoolsConfigurations.GetLambdaLogLevel();
        var lambdaLogLevelEnabled = _powertoolsConfigurations.LambdaLogLevelEnabled();

        // Warn if Lambda log level doesn't match
        if (lambdaLogLevelEnabled && logLevel < lambdaLogLevel)
        {
            _currentConfig.LogOutput.WriteLine(
                $"Current log level ({logLevel}) does not match AWS Lambda Advanced Logging Controls minimum log level ({lambdaLogLevel}). This can lead to data loss, consider adjusting them.");
        }

        // Set service from environment if not explicitly set
        if (string.IsNullOrEmpty(_currentConfig.Service))
        {
            _currentConfig.Service = _powertoolsConfigurations.Service;
        }

        // Set output case from environment if not explicitly set
        if (_currentConfig.LoggerOutputCase == LoggerOutputCase.Default)
        {
            var loggerOutputCase = _powertoolsConfigurations.GetLoggerOutputCase(_currentConfig.LoggerOutputCase);
            _currentConfig.LoggerOutputCase = loggerOutputCase;
        }

        // Set log level from environment ONLY if not explicitly set
        var minLogLevel = lambdaLogLevelEnabled ? lambdaLogLevel : logLevel;
        _currentConfig.MinimumLogLevel = minLogLevel != LogLevel.None ? minLogLevel : LoggingConstants.DefaultLogLevel;
        _currentConfig.XRayTraceId = _powertoolsConfigurations.XRayTraceId;
        _currentConfig.LogEvent = _powertoolsConfigurations.LoggerLogEvent;
        
        // Configure the log level key based on output case
        _currentConfig.LogLevelKey = _powertoolsConfigurations.LambdaLogLevelEnabled() &&
                                     _currentConfig.LoggerOutputCase == LoggerOutputCase.PascalCase
            ? "LogLevel"
            : LoggingConstants.KeyLogLevel;
            
        ProcessSamplingRate(_currentConfig, _powertoolsConfigurations);
        _environmentConfigured = true;
    }
    
    /// <summary>
    /// Process sampling rate configuration
    /// </summary>
    private void ProcessSamplingRate(PowertoolsLoggerConfiguration config, IPowertoolsConfigurations configurations)
    {
        var samplingRate = config.SamplingRate > 0 
            ? config.SamplingRate 
            : configurations.LoggerSampleRate;
            
        samplingRate = ValidateSamplingRate(samplingRate, config);
        config.SamplingRate = samplingRate;

        // Only notify if sampling is configured
        if (samplingRate > 0)
        {
            double sample = config.GetRandom();
            
            // Instead of changing log level, just indicate sampling status
            if (sample <= samplingRate)
            {
                config.LogOutput.WriteLine(
                    $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {samplingRate}, Sampler Value: {sample}.");
                config.MinimumLogLevel = LogLevel.Debug;
            }
        }
    }

    /// <summary>
    /// Validate sampling rate
    /// </summary>
    private double ValidateSamplingRate(double samplingRate, PowertoolsLoggerConfiguration config)
    {
        if (samplingRate < 0 || samplingRate > 1)
        {
            if (config.MinimumLogLevel is LogLevel.Debug or LogLevel.Trace)
            {
                config.LogOutput.WriteLine(
                    $"Skipping sampling rate configuration because of invalid value. Sampling rate: {samplingRate}");
            }

            return 0;
        }

        return samplingRate;
    }

    public virtual ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new PowertoolsLogger(
            name,
            GetCurrentConfig,
            _powertoolsConfigurations));
    }

    internal PowertoolsLoggerConfiguration GetCurrentConfig() => _currentConfig;
    
    public void UpdateConfiguration(PowertoolsLoggerConfiguration config)
    {
        _currentConfig = config;
        
        // Apply environment configurations if available
        if (_powertoolsConfigurations != null && !_environmentConfigured)
        {
            ConfigureFromEnvironment();
        }
    }

    public virtual void Dispose()
    {
        _loggers.Clear();
    }
}