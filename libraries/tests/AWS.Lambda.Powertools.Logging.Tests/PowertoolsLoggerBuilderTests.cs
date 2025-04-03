using System;
using System.Text.Json;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Tests.Formatter;
using AWS.Lambda.Powertools.Logging.Tests.Handlers;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.Logging.Tests;

public class PowertoolsLoggerBuilderTests
{
    private readonly ITestOutputHelper _output;

    public PowertoolsLoggerBuilderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void WithService_SetsServiceName()
    {
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("test-builder-service")
            .Build();

        logger.LogInformation("Testing service name");

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        Assert.Contains("\"service\":\"test-builder-service\"", logOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithSamplingRate_SetsSamplingRate()
    {
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("sampling-test")
            .WithSamplingRate(0.5)
            .Build();

        // We can't directly test sampling rate in a deterministic way,
        // but we can verify the logger is created successfully
        logger.LogInformation("Testing sampling rate");

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        Assert.Contains("\"message\":\"Testing sampling rate\"", logOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithMinimumLogLevel_FiltersLowerLevels()
    {
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("log-level-test")
            .WithMinimumLogLevel(LogLevel.Warning)
            .Build();

        logger.LogDebug("Debug message");
        logger.LogInformation("Info message");
        logger.LogWarning("Warning message");

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        Assert.DoesNotContain("Debug message", logOutput);
        Assert.DoesNotContain("Info message", logOutput);
        Assert.Contains("Warning message", logOutput);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void WithJsonOptions_AppliesFormatting()
    {
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithService("json-options-test")
            .WithLogOutput(output)
            .WithJsonOptions(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            })
            .Build();

        var testObject = new ExampleClass
        {
            Name = "TestName",
            ThisIsBig = "BigValue"
        };

        logger.LogInformation("Test object: {@testObject}", testObject);

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        Assert.Contains("\"this_is_big\":\"BigValue\"", logOutput);
        Assert.Contains("\"name\":\"TestName\"", logOutput);
        Assert.Contains("\n", logOutput); // Indentation includes newlines
    }
#endif

    [Fact]
    public void WithTimestampFormat_FormatsTimestamp()
    {
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("timestamp-test")
            .WithTimestampFormat("yyyy-MM-dd")
            .Build();

        logger.LogInformation("Testing timestamp format");

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        // Should match yyyy-MM-dd format (e.g., "2023-04-25")
        Assert.Matches("\"timestamp\":\"\\d{4}-\\d{2}-\\d{2}\"", logOutput);
    }

    [Fact]
    public void WithOutputCase_ChangesPropertyCasing()
    {
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("case-test")
            .WithOutputCase(LoggerOutputCase.PascalCase)
            .Build();

        logger.LogInformation("Testing output case");

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        Assert.Contains("\"Service\":\"case-test\"", logOutput);
        Assert.Contains("\"Level\":\"Information\"", logOutput);
        Assert.Contains("\"Message\":\"Testing output case\"", logOutput);
    }

    [Fact]
    public void WithLogBuffering_BuffersLowLevelLogs()
    {
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("buffer-test")
            .WithLogBuffering(options =>
            {
                options.BufferAtLogLevel = LogLevel.Debug;
            })
            .Build();

        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "config-test");
        logger.LogDebug("Debug buffered message");
        logger.LogInformation("Info message");

        // Without FlushBuffer(), the debug message should be buffered
        var initialOutput = output.ToString();
        _output.WriteLine("Before flush: " + initialOutput);

        Assert.DoesNotContain("Debug buffered message", initialOutput);
        Assert.Contains("Info message", initialOutput);

        // After flushing, the debug message should appear
        logger.FlushBuffer();
        var afterFlushOutput = output.ToString();
        _output.WriteLine("After flush: " + afterFlushOutput);

        Assert.Contains("Debug buffered message", afterFlushOutput);
    }

    [Fact]
    public void BuilderChaining_ConfiguresAllProperties()
    {
        var output = new TestLoggerOutput();
        var customFormatter = new CustomLogFormatter();

        var logger = new PowertoolsLoggerBuilder()
            .WithService("chained-config-service")
            .WithSamplingRate(0.1)
            .WithMinimumLogLevel(LogLevel.Information)
            .WithOutputCase(LoggerOutputCase.SnakeCase)
            .WithFormatter(customFormatter)
            .WithLogOutput(output)
            .Build();

        logger.LogInformation("Testing fully configured logger");

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        // Verify multiple configured properties are applied
        Assert.Contains("\"service\":\"chained-config-service\"", logOutput);
        Assert.Contains("\"message\":\"Testing fully configured logger\"", logOutput);
        Assert.Contains("\"sample_rate\":0.1", logOutput);
    }
}