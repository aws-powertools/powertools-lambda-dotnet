using System;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

public sealed class PowertoolsLoggerFactory : IDisposable
{
    private readonly ILoggerFactory _factory;

    public PowertoolsLoggerFactory(ILoggerFactory? loggerFactory = null)
    {
        _factory = loggerFactory ?? LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger();
        });
    }
    
    public PowertoolsLoggerFactory() : this(LoggerFactory.Create(builder => { builder.AddPowertoolsLogger(); }))
    {
    }
    
    public static PowertoolsLoggerFactory Create(Action<PowertoolsLoggerConfiguration> configureOptions)
    {
        var options = new PowertoolsLoggerConfiguration();
        configureOptions(options);

        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                // Copy basic properties
                config.Service = options.Service;
                config.MinimumLevel = options.MinimumLevel;
                config.LoggerOutputCase = options.LoggerOutputCase;
                config.SamplingRate = options.SamplingRate;
        
                // // Copy additional contexts using the public API
                // foreach (var ctx in options.GetAdditionalContexts())
                // {
                //     config.AddJsonContext(ctx);
                // }
                //
                // // Copy log level colors
                // foreach (var kvp in options.LogLevelToColorMap)
                // {
                //     config.LogLevelToColorMap[kvp.Key] = kvp.Value;
                // }
            });
        });
        
        Logger.Configure(factory);
        return new PowertoolsLoggerFactory(factory);
    }

    // Add builder pattern support
    public static PowertoolsLoggerFactoryBuilder CreateBuilder()
    {
        return new PowertoolsLoggerFactoryBuilder();
    }
    
    public ILogger CreateLogger<T>() => CreateLogger(typeof(T).FullName ?? typeof(T).Name);

    public ILogger CreateLogger(string category)
    {
        return _factory.CreateLogger(category);
    }
    
    public void Dispose()
    {
        _factory?.Dispose();
    }
}