using System;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

internal sealed class PowertoolsLoggerFactory : IDisposable
{
    private readonly ILoggerFactory _factory;

    internal PowertoolsLoggerFactory(ILoggerFactory loggerFactory)
    {
        _factory = loggerFactory;
    }
    
    internal PowertoolsLoggerFactory() : this(LoggerFactory.Create(builder => { builder.AddPowertoolsLogger(); }))
    {
    }
    
    internal static PowertoolsLoggerFactory Create(Action<PowertoolsLoggerConfiguration> configureOptions)
    {
        var options = new PowertoolsLoggerConfiguration();
        configureOptions(options);
        var factory = Create(options);
        return new PowertoolsLoggerFactory(factory);
    }
    
    internal static ILoggerFactory Create(PowertoolsLoggerConfiguration options)
    {
        return LoggerFactoryHelper.CreateAndConfigureFactory(options);
    }

    // Add builder pattern support
    internal static PowertoolsLoggerBuilder CreateBuilder()
    {
        return new PowertoolsLoggerBuilder();
    }
    
    internal ILogger CreateLogger<T>() => CreateLogger(typeof(T).FullName ?? typeof(T).Name);

    internal ILogger CreateLogger(string category)
    {
        return _factory.CreateLogger(category);
    }
    
    internal ILogger CreatePowertoolsLogger()
    {
        return _factory.CreatePowertoolsLogger();
    }
    
    public void Dispose()
    {
        _factory?.Dispose();
    }
}