using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests;

/// <summary>
///     The X-Ray trace ID is read from LambdaTraceProvider or the _X_AMZN_TRACE_ID environment variable on every
///     access, so it can change or disappear between two reads.
/// </summary>
[Collection("Sequential")]
public class TraceIdTests : IDisposable
{
    private const string TraceId = "Root=1-5759e988-bd862e3fe1be46a994272793;Sampled=1";

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", null);
        LogBufferManager.ResetForTesting();
    }

    [Fact]
    public void Log_WhenTraceIdIsClearedWhileTheEntryIsBuilt_DoesNotThrowAndWritesTheTraceIdItRead()
    {
        var configurations = Substitute.For<IPowertoolsConfigurations>();
        var output = Substitute.For<IConsoleWrapper>();
        var provider = new PowertoolsLoggerProvider(new PowertoolsLoggerConfiguration
        {
            Service = "trace-id-test",
            MinimumLogLevel = LogLevel.Information,
            LogOutput = output
        }, configurations);
        var logger = provider.CreateLogger("test");

        // Set after the provider is created, so both values are seen while the log entry is built:
        // the trace ID is present on the first read and gone on any later read
        configurations.XRayTraceId.Returns(TraceId, (string)null);

        var exception = Record.Exception(() => logger.LogInformation("message"));

        Assert.Null(exception);
        output.Received(1).WriteLine(Arg.Is<string>(s =>
            s.Contains("\"xray_trace_id\":\"1-5759e988-bd862e3fe1be46a994272793\"")));
    }

    [Fact]
    public void LogBuffer_WhenThereIsNoTraceId_ReportsNoEntriesAndClearingDoesNotThrow()
    {
        var configurations = Substitute.For<IPowertoolsConfigurations>();
        configurations.XRayTraceId.Returns((string)null);
        var buffer = new LogBuffer(configurations);

        Assert.False(buffer.HasEntries);
        Assert.False(buffer.HasEvictions);
        Assert.Null(Record.Exception(() => buffer.ClearCurrentInvocation()));
    }

    [Fact]
    public void LogBuffer_WhenThereIsATraceId_ReportsAndClearsEntriesForThatInvocation()
    {
        var configurations = Substitute.For<IPowertoolsConfigurations>();
        configurations.XRayTraceId.Returns(TraceId);
        var buffer = new LogBuffer(configurations);

        buffer.Add("entry", maxBytes: 1024, size: 5);

        Assert.True(buffer.HasEntries);
        Assert.False(buffer.HasEvictions);

        buffer.ClearCurrentInvocation();

        Assert.False(buffer.HasEntries);
    }

    [Fact]
    public void ClearBuffer_WhenBufferingIsEnabledAndThereIsNoTraceId_DoesNotThrow()
    {
        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", null);
        var logger = CreateBufferingLogger(new TestLoggerOutput());

        logger.LogDebug("buffered");

        Assert.Null(Record.Exception(() => logger.ClearBuffer()));
    }

    [Fact]
    public void FlushBuffer_WhenBufferingIsEnabledAndThereIsNoTraceId_DoesNotLogAFlushFailure()
    {
        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", null);
        var output = new TestLoggerOutput();
        var logger = CreateBufferingLogger(output);

        logger.LogDebug("buffered");
        logger.FlushBuffer();

        Assert.DoesNotContain("Failed to flush log buffer", output.ToString());
    }

    private static ILogger CreateBufferingLogger(TestLoggerOutput output)
    {
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogOutput = output
        };
        config.LogBuffering.Enabled = true;
        config.LogBuffering.BufferAtLogLevel = LogLevel.Debug;
        return LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();
    }
}
