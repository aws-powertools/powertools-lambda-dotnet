using System;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Buffering;

[Collection("Sequential")]
public class LogBufferingAlcWarningTests : IDisposable
{
    private readonly TestLoggerOutput _output = new();

    public LogBufferingAlcWarningTests()
    {
        ResetAllState();
        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
    }

    [Fact]
    public void Warns_Once_AtSetup_WhenAlcIsLessVerboseThanBufferLevel()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL", "INFO");

        // Two factories, as when the configuration is applied again later in the environment
        CreateBufferingLogger(LogLevel.Debug);
        CreateBufferingLogger(LogLevel.Debug);

        Assert.Equal(1, CountOccurrences(_output.ToString(), BufferingLoggerProvider.AlcFiltersBufferWarning));
    }

    [Fact]
    public void Warns_Once_OnFlush_WhenAlcIsLessVerboseThanBufferLevel()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL", "INFO");
        var logger = CreateBufferingLogger(LogLevel.Debug);

        logger.LogDebug("first buffered entry");
        logger.FlushBuffer();
        logger.LogDebug("second buffered entry");
        logger.FlushBuffer();

        var output = _output.ToString();
        Assert.Contains("first buffered entry", output);
        Assert.Contains("second buffered entry", output);
        Assert.Equal(1, CountOccurrences(output, BufferingLoggerProvider.AlcFiltersFlushWarning));
        Assert.Contains("\"level\":\"Warning\"", output);
    }

    [Fact]
    public void DoesNotWarn_OnFlush_WhenBufferIsEmpty()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL", "INFO");
        var logger = CreateBufferingLogger(LogLevel.Debug);

        logger.FlushBuffer();

        Assert.DoesNotContain(BufferingLoggerProvider.AlcFiltersFlushWarning, _output.ToString());
    }

    [Theory]
    [InlineData("DEBUG", LogLevel.Debug)]
    [InlineData("TRACE", LogLevel.Debug)]
    [InlineData("INFO", LogLevel.Information)]
    [InlineData(null, LogLevel.Debug)]
    public void DoesNotWarn_WhenAlcIsAtOrBelowBufferLevel(string alcLevel, LogLevel bufferAtLogLevel)
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL", alcLevel);
        var logger = CreateBufferingLogger(bufferAtLogLevel);

        logger.Log(bufferAtLogLevel, "buffered entry");
        logger.FlushBuffer();

        var output = _output.ToString();
        Assert.DoesNotContain(BufferingLoggerProvider.AlcFiltersBufferWarning, output);
        Assert.DoesNotContain(BufferingLoggerProvider.AlcFiltersFlushWarning, output);
    }

    [Fact]
    public void DoesNotWarn_WhenBufferingIsDisabled()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL", "ERROR");
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogOutput = _output
        };

        LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

        Assert.DoesNotContain(BufferingLoggerProvider.AlcFiltersBufferWarning, _output.ToString());
    }

    private ILogger CreateBufferingLogger(LogLevel bufferAtLogLevel)
    {
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogBuffering = new LogBufferingOptions
            {
                Enabled = true,
                BufferAtLogLevel = bufferAtLogLevel
            },
            LogOutput = _output
        };

        return LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();
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
        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", null);
    }

    private static void ResetAllState()
    {
        Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL", null);
        LoggingAspect.ResetForTest();
        Logger.Reset();
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
        LogBufferManager.ResetForTesting();
    }
}
