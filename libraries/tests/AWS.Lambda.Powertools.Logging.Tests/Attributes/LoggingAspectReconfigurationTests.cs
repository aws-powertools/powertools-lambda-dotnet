using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Attributes;

[Collection("Sequential")]
public class LoggingAspectReconfigurationTests : IDisposable
{
    private const string AlcWarning =
        "Current log level (Information) does not match AWS Lambda Advanced Logging Controls minimum log level (Warning)";

    private readonly TestLoggerOutput _output = new();

    public LoggingAspectReconfigurationTests()
    {
        ResetAllState();
    }

    [Fact]
    public void AlcWarning_IsWrittenOnce_AcrossWarmInvocations()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL", "Warn");
        ConfigureOutput();

        var attribute = new LoggingAttribute { Service = "svc", LogLevel = LogLevel.Information };

        for (var i = 0; i < 4; i++)
        {
            Invoke(attribute);
        }

        Assert.Equal(1, CountOccurrences(_output.ToString(), AlcWarning));
    }

    [Fact]
    public void Factory_IsNotRebuilt_WhenAttributeSettingsDoNotChange()
    {
        ConfigureOutput();
        var attribute = new LoggingAttribute { Service = "svc", LogLevel = LogLevel.Information };

        Invoke(attribute);
        var factoryAfterFirstInvocation = LoggerFactoryHolder.Current;
        var configAfterFirstInvocation = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();

        for (var i = 0; i < 3; i++)
        {
            // A new attribute instance with the same values, as on a warm invocation
            Invoke(new LoggingAttribute { Service = "svc", LogLevel = LogLevel.Information });
        }

        Assert.NotNull(factoryAfterFirstInvocation);
        Assert.Same(factoryAfterFirstInvocation, LoggerFactoryHolder.Current);
        Assert.Same(configAfterFirstInvocation, PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration());
    }

    [Fact]
    public void Factory_IsRebuilt_WhenAttributeSettingsChange()
    {
        ConfigureOutput();

        Invoke(new LoggingAttribute { Service = "svc-a", LogLevel = LogLevel.Information });
        var firstFactory = LoggerFactoryHolder.Current;

        Invoke(new LoggingAttribute { Service = "svc-b", LogLevel = LogLevel.Debug });

        Assert.NotSame(firstFactory, LoggerFactoryHolder.Current);
        var config = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();
        Assert.Equal("svc-b", config.Service);
        Assert.Equal(LogLevel.Debug, config.MinimumLogLevel);
    }

    [Fact]
    public void Factory_IsRebuilt_WhenConfigurationIsReplacedBetweenInvocations()
    {
        ConfigureOutput();
        var attribute = new LoggingAttribute { Service = "svc", LogLevel = LogLevel.Information };

        Invoke(attribute);
        var firstFactory = LoggerFactoryHolder.Current;

        // Replaces the shared configuration, as Logger.Configure does
        ConfigureOutput();
        Invoke(attribute);

        Assert.NotSame(firstFactory, LoggerFactoryHolder.Current);
        Assert.Equal("svc", PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration().Service);
    }

    [Fact]
    public void Logs_UseAttributeSettings_OnWarmInvocations()
    {
        ConfigureOutput();
        var attribute = new LoggingAttribute { Service = "svc", LogLevel = LogLevel.Debug };

        for (var i = 0; i < 3; i++)
        {
            Invoke(attribute, () => Logger.LogDebug($"debug message {i}"));
        }

        var debugLines = Array.FindAll(
            _output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries),
            line => line.Contains("debug message"));

        Assert.Equal(3, debugLines.Length);
        Assert.All(debugLines, line => Assert.Contains("\"service\":\"svc\"", line));
    }

    private void ConfigureOutput()
    {
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogOutput = _output
        };
        PowertoolsLoggerFactory.Create(config);
    }

    private static void Invoke(LoggingAttribute attribute, Action handler = null)
    {
        var args = new AspectEventArgs
        {
            Args = new object[] { new object() },
            Triggers = new Attribute[] { attribute }
        };

        // The attribute creates a new aspect for every invocation
        var aspect = new LoggingAspect(LoggerFactoryHolder.GetOrCreateFactory().CreatePowertoolsLogger());
        aspect.OnEntry(args);
        handler?.Invoke();
        aspect.OnExit(args);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    public void Dispose()
    {
        ResetAllState();
    }

    private static void ResetAllState()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_CASE", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", null);

        LoggingAspect.ResetForTest();
        Logger.Reset();
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
        LoggerFactoryHolder.Reset();
    }
}
