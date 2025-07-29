using System;
using System.Linq;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests;

public class PowertoolsLoggerExtensionsTests
{
    [Fact]
    public void AddPowertoolsLogger_WithClearExistingProviders_False_KeepsExistingProviders()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            // Add a mock existing provider first
            builder.Services.AddSingleton<ILoggerProvider, MockLoggerProvider>();
        
            // Act
            builder.AddPowertoolsLogger(clearExistingProviders: false);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var loggerProviders = serviceProvider.GetServices<ILoggerProvider>();

        // Assert
        var collection = loggerProviders as ILoggerProvider[] ?? loggerProviders.ToArray();
        Assert.Contains(collection, p => p is MockLoggerProvider);
        Assert.Contains(collection, p => p is PowertoolsLoggerProvider);
        Assert.True(collection.Count() >= 2); // Should have both providers
    }

    [Fact]
    public void AddPowertoolsLogger_WithClearExistingProviders_True_RemovesExistingProviders()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            // Add a mock existing provider first
            builder.Services.AddSingleton<ILoggerProvider, MockLoggerProvider>();
        
            // Act
            builder.AddPowertoolsLogger(clearExistingProviders: true);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var loggerProviders = serviceProvider.GetServices<ILoggerProvider>();

        // Assert
        var collection = loggerProviders as ILoggerProvider[] ?? loggerProviders.ToArray();
        Assert.DoesNotContain(collection, p => p is MockLoggerProvider);
        Assert.Contains(collection, p => p is PowertoolsLoggerProvider);
        Assert.Single(collection); // Should only have Powertools provider
    }
    
    private class MockLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new MockLogger();
        public void Dispose() { }
    }

    private class MockLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    }
}