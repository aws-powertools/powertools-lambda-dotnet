using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Tests.Handlers;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Attributes;

[Collection("A Sequential")]
public class ServiceTests : IDisposable
{
    private readonly TestServiceHandler _testHandler;

    public ServiceTests()
    {
        _testHandler = new TestServiceHandler();
    }

    [Fact]
    public void When_Setting_Service_Should_Override_Env()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "Environment Service");
            
        var consoleOut = Substitute.For<IConsoleWrapper>();
        Logger.Configure(options => 
            options.LogOutput = consoleOut);

        // Act
        _testHandler.LogWithEnv();
        _testHandler.Handler();

        // Assert
            
        consoleOut.Received(1).WriteLine(
            Arg.Is<string>(i => i.Contains("\"level\":\"Information\",\"service\":\"Environment Service\",\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":\"Service: Environment Service\""))
        );
        consoleOut.Received(1).WriteLine(
            Arg.Is<string>(i => i.Contains("\"level\":\"Information\",\"service\":\"Attribute Service\",\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":\"Service: Attribute Service\""))
        );            
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_CASE", "");
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "");
        LoggingAspect.ResetForTest();
        Logger.Reset();
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
    }
}