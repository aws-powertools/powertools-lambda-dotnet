using System;
using System.Text.Json;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
/// Builder class for creating configured PowertoolsLogger instances.
/// Provides a fluent interface for configuring logging options.
/// </summary>
public class PowertoolsLoggerBuilder
{
    private readonly PowertoolsLoggerConfiguration _configuration = new();

    /// <summary>
    /// Sets the service name for the logger.
    /// </summary>
    /// <param name="service">The service name to be included in logs.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PowertoolsLoggerBuilder WithService(string service)
    {
        _configuration.Service = service;
        return this;
    }

    /// <summary>
    /// Sets the sampling rate for logs.
    /// </summary>
    /// <param name="rate">The sampling rate between 0 and 1.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PowertoolsLoggerBuilder WithSamplingRate(double rate)
    {
        _configuration.SamplingRate = rate;
        return this;
    }

    /// <summary>
    /// Sets the minimum log level for the logger.
    /// </summary>
    /// <param name="level">The minimum LogLevel to capture.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PowertoolsLoggerBuilder WithMinimumLogLevel(LogLevel level)
    {
        _configuration.MinimumLogLevel = level;
        return this;
    }

    /// <summary>
    /// Sets custom JSON serialization options.
    /// </summary>
    /// <param name="options">JSON serializer options to use for log formatting.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PowertoolsLoggerBuilder WithJsonOptions(JsonSerializerOptions options)
    {
        _configuration.JsonOptions = options;
        return this;
    }

    /// <summary>
    /// Sets the timestamp format for log entries.
    /// </summary>
    /// <param name="format">The timestamp format string.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PowertoolsLoggerBuilder WithTimestampFormat(string format)
    {
        _configuration.TimestampFormat = format;
        return this;
    }

    /// <summary>
    /// Sets the output casing style for log properties.
    /// </summary>
    /// <param name="outputCase">The casing style to use for log output.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PowertoolsLoggerBuilder WithOutputCase(LoggerOutputCase outputCase)
    {
        _configuration.LoggerOutputCase = outputCase;
        return this;
    }

    /// <summary>
    /// Sets a custom log formatter.
    /// </summary>
    /// <param name="formatter">The formatter to use for log formatting.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when formatter is null.</exception>
    public PowertoolsLoggerBuilder WithFormatter(ILogFormatter formatter)
    {
        _configuration.LogFormatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        return this;
    }

    /// <summary>
    /// Enables or disables log buffering with default options.
    /// </summary>
    /// <param name="enabled">Whether log buffering should be enabled.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PowertoolsLoggerBuilder WithLogBuffering(bool enabled = true)
    {
        _configuration.LogBuffering.Enabled = enabled;
        return this;
    }

    /// <summary>
    /// Configures log buffering with custom options.
    /// </summary>
    /// <param name="configure">Action to configure the log buffering options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PowertoolsLoggerBuilder WithLogBuffering(Action<LogBufferingOptions> configure)
    {
        configure?.Invoke(_configuration.LogBuffering);
        return this;
    }
    
    public PowertoolsLoggerBuilder WithLogOutput(IConsoleWrapper console)
    {
        _configuration.LogOutput = console ?? throw new ArgumentNullException(nameof(console));
        return this;
    }
    

    /// <summary>
    /// Builds and returns a configured logger instance.
    /// </summary>
    /// <returns>An ILogger configured with the specified options.</returns>
    public ILogger Build()
    {
        var factory = LoggerFactoryHelper.CreateAndConfigureFactory(_configuration);
        return factory.CreatePowertoolsLogger();
    }
}