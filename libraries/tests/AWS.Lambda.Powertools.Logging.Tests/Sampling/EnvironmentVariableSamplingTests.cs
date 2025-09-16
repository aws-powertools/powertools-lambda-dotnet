using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AWS.Lambda.Powertools.Logging.Tests;

/// <summary>
/// Tests for sampling behavior when using environment variables
/// This covers the specific use case described in the GitHub issue
/// </summary>
public class EnvironmentVariableSamplingTests : IDisposable
{
    private readonly string _originalLogLevel;
    private readonly string _originalSampleRate;

    public EnvironmentVariableSamplingTests()
    {
        // Store original environment variables
        _originalLogLevel = Environment.GetEnvironmentVariable("POWERTOOLS_LOG_LEVEL");
        _originalSampleRate = Environment.GetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE");

        // Reset logger before each test
        Logger.Reset();
    }

    public void Dispose()
    {
        // Restore original environment variables
        if (_originalLogLevel != null)
            Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", _originalLogLevel);
        else
            Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", null);

        if (_originalSampleRate != null)
            Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE", _originalSampleRate);
        else
            Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE", null);

        Logger.Reset();
    }

    /// <summary>
    /// Creates a logger factory that properly processes environment variables
    /// </summary>
    private ILoggerFactory CreateLoggerFactoryWithEnvironmentVariables(TestLoggerOutput output)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = "HelloWorldService";
                config.LoggerOutputCase = LoggerOutputCase.CamelCase;
                config.LogEvent = true;
                config.LogOutput = output;
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ILoggerFactory>();
    }

    /// <summary>
    /// Test the exact scenario described in the GitHub issue:
    /// POWERTOOLS_LOG_LEVEL=Error and POWERTOOLS_LOGGER_SAMPLE_RATE=0.9
    /// Information logs should be elevated to debug and logged when sampling is triggered
    /// </summary>
    [Fact]
    public void EnvironmentVariables_ErrorLevelWithSampling_ShouldLogInfoWhenSamplingTriggered()
    {
        // Arrange
        var originalLogLevel = Environment.GetEnvironmentVariable("POWERTOOLS_LOG_LEVEL");
        var originalSampleRate = Environment.GetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE");

        try
        {
            Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", "Error");
            Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE", "0.9");

            var output = new TestLoggerOutput();
            bool samplingTriggered = false;
            string logOutput = "";

            // Try multiple times to trigger sampling (90% chance each time)
            for (int attempt = 0; attempt < 20 && !samplingTriggered; attempt++)
            {
                output.Clear();
                Logger.Reset();
                Logger.Configure(options => { options.LogOutput = output; });

                Logger.LogError("This is an error message");
                Logger.LogInformation("Another info message");

                logOutput = output.ToString();
                samplingTriggered = logOutput.Contains("Another info message");
            }

            // Assert
            Assert.True(samplingTriggered,
                $"Sampling should have been triggered within 20 attempts with 90% rate. " +
                $"Last output: {logOutput}");

            // Only verify the content if sampling was triggered
            if (samplingTriggered)
            {
                Assert.Contains("This is an error message", logOutput);
                Assert.Contains("Another info message", logOutput);
            }
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", originalLogLevel);
            Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE", originalSampleRate);
            Logger.Reset();
        }
    }


    /// <summary>
    /// Test with POWERTOOLS_LOGGER_SAMPLE_RATE=1.0 (100% sampling)
    /// This should always trigger sampling - guarantees the fix works
    /// </summary>
    [Fact]
    public void EnvironmentVariables_ErrorLevelWithFullSampling_ShouldAlwaysLogInfo()
    {
        // Arrange
        Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", "Error");
        Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE", "1.0");

        var output = new TestLoggerOutput();

        using var loggerFactory = CreateLoggerFactoryWithEnvironmentVariables(output);
        var logger = loggerFactory.CreateLogger<EnvironmentVariableSamplingTests>();

        // Act
        logger.LogError("This is an error message");
        logger.LogInformation("This is an info message — should appear with 100% sampling");

        // Assert
        var logOutput = output.ToString();
        Assert.Contains("Changed log level to DEBUG based on Sampling configuration", logOutput);
        Assert.Contains("This is an error message", logOutput);
        Assert.Contains("This is an info message — should appear with 100% sampling", logOutput);
    }

    /// <summary>
    /// Test with POWERTOOLS_LOGGER_SAMPLE_RATE=0 (no sampling)
    /// Info messages should not be logged - ensures sampling is required
    /// </summary>
    [Fact]
    public void EnvironmentVariables_ErrorLevelWithNoSampling_ShouldNotLogInfo()
    {
        // Arrange
        Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", "Error");
        Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE", "0");

        var output = new TestLoggerOutput();

        using var loggerFactory = CreateLoggerFactoryWithEnvironmentVariables(output);
        var logger = loggerFactory.CreateLogger<EnvironmentVariableSamplingTests>();

        // Act
        logger.LogError("This is an error message");
        logger.LogInformation("This is an info message — should NOT appear with 0% sampling");

        // Assert
        var logOutput = output.ToString();
        Assert.DoesNotContain("Changed log level to DEBUG based on Sampling configuration", logOutput);
        Assert.Contains("This is an error message", logOutput);
        Assert.DoesNotContain("This is an info message — should NOT appear with 0% sampling", logOutput);
    }

    /// <summary>
    /// Test the ShouldEnableDebugSampling() method without out parameter
    /// </summary>
    [Fact]
    public void ShouldEnableDebugSampling_WithoutOutParameter_ShouldReturnCorrectValue()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 1.0 // 100% sampling
        };

        // Act
        var result = config.ShouldEnableDebugSampling();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test the ShouldEnableDebugSampling() method with zero sampling rate
    /// </summary>
    [Fact]
    public void ShouldEnableDebugSampling_WithZeroSamplingRate_ShouldReturnFalse()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 0.0 // 0% sampling
        };

        // Act
        var result = config.ShouldEnableDebugSampling();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test the RefreshSampleRateCalculation() method without out parameter
    /// </summary>
    [Fact]
    public void RefreshSampleRateCalculation_WithoutOutParameter_ShouldReturnCorrectValue()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 1.0, // 100% sampling
            InitialLogLevel = LogLevel.Error,
            MinimumLogLevel = LogLevel.Error
        };

        // Act - First call should return false due to cold start protection
        var firstResult = config.RefreshSampleRateCalculation();

        // Second call should return true with 100% sampling
        var secondResult = config.RefreshSampleRateCalculation();

        // Assert
        Assert.False(firstResult); // Cold start protection
        Assert.True(secondResult); // Should enable sampling
    }

    /// <summary>
    /// Test the RefreshSampleRateCalculation() method with zero sampling rate
    /// </summary>
    [Fact]
    public void RefreshSampleRateCalculation_WithZeroSamplingRate_ShouldReturnFalse()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 0.0 // 0% sampling
        };

        // Act
        var result = config.RefreshSampleRateCalculation();

        // Assert
        Assert.False(result);
    }
}