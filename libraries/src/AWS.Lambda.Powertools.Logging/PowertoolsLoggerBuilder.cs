using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

public class PowertoolsLoggerBuilder
{
    private readonly PowertoolsLoggerConfiguration _configuration = new();

    public PowertoolsLoggerBuilder WithService(string service)
    {
        _configuration.Service = service;
        return this;
    }
    
    public PowertoolsLoggerBuilder WithSamplingRate(double rate)
    {
        _configuration.SamplingRate = rate;
        return this;
    }
    
    public PowertoolsLoggerBuilder WithMinimumLogLevel(LogLevel level)
    {
        _configuration.MinimumLogLevel = level;
        return this;
    }
    
    public PowertoolsLoggerBuilder WithJsonOptions(JsonSerializerOptions options)
    {
        _configuration.JsonOptions = options;
        return this;
    }
    
    public PowertoolsLoggerBuilder WithTimestampFormat(string format)
    {
        _configuration.TimestampFormat = format;
        return this;
    }
    
    public PowertoolsLoggerBuilder WithOutputCase(LoggerOutputCase outputCase)
    {
        _configuration.LoggerOutputCase = outputCase;
        return this;
    }
    
    public PowertoolsLoggerBuilder WithOutput(ISystemWrapper output)
    {
        _configuration.LoggerOutput = output;
        return this;
    }

    public PowertoolsLoggerBuilder WithFormatter(ILogFormatter formatter)
    {
        _configuration.LogFormatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        return this;
    }

    /// <summary>
    /// Enable log buffering with default options
    /// </summary>
    public PowertoolsLoggerBuilder WithLogBuffering(bool enabled = true)
    {
        _configuration.LogBufferingOptions.Enabled = enabled;
        return this;
    }

    /// <summary>
    /// Configure log buffering options
    /// </summary>
    public PowertoolsLoggerBuilder WithLogBuffering(Action<LogBufferingOptions> configure)
    {
        configure?.Invoke(_configuration.LogBufferingOptions);
        return this;
    }
    
    public ILogger Build()
    {
        var factory = LoggerFactoryHelper.CreateAndConfigureFactory(_configuration);
        return factory.CreatePowertoolsLogger();
    }
}