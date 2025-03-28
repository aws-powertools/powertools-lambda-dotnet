using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal.Helpers;

/// <summary>
/// Helper class for creating and configuring logger factories
/// </summary>
internal static class LoggerFactoryHelper
{
    /// <summary>
    /// Creates and configures a logger factory with the provided configuration
    /// </summary>
    /// <param name="configuration">The Powertools logger configuration to apply</param>
    /// <returns>The configured logger factory</returns>
    public static ILoggerFactory CreateAndConfigureFactory(PowertoolsLoggerConfiguration configuration)
    {
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = configuration.Service;
                config.SamplingRate = configuration.SamplingRate;
                config.MinimumLogLevel = configuration.MinimumLogLevel;
                config.LoggerOutputCase = configuration.LoggerOutputCase;
                config.JsonOptions = configuration.JsonOptions;
                config.TimestampFormat = configuration.TimestampFormat;
                config.LogFormatter = configuration.LogFormatter;
                config.LogLevelKey = configuration.LogLevelKey;
                config.LogBuffering = configuration.LogBuffering;
            });
        });

        // Configure the static logger with the factory
        // Logger.Configure(factory);

        return factory;
    }
}

// Add to a new TestHelpers.cs file
public static class PowertoolsLoggerTestHelpers
{
    private static readonly object _lock = new();
    private static ISystemWrapper _systemWrapper;

    static PowertoolsLoggerTestHelpers()
    {
        _systemWrapper = null;
    }
    
    // Call this at the beginning of your test
    public static TestLoggerOutput EnableTestMode()
    {
        var system = new TestLoggerOutput();
        _systemWrapper = system;
        PowertoolsLoggingBuilderExtensions.UpdateSystemInAllProviders(system);
        return system;
    }

    public static void UseCustomSystem(ISystemWrapper system)
    {
        if (system == null) throw new ArgumentNullException(nameof(system));
        lock (_lock)
        {
            // Store the mock system for later use when providers are created
            _systemWrapper = system;
            // Update all providers to use the mock system
            PowertoolsLoggingBuilderExtensions.UpdateSystemInAllProviders(system);
        }
    }
    
    internal static ISystemWrapper GetSystemWrapper()
    {
        lock (_lock)
        {
            return _systemWrapper;
        }
    }
}