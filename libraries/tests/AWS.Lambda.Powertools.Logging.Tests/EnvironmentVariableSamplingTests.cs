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
        // Arrange - Set environment variables as described in the issue
        Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", "Error");
        Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_SAMPLE_RATE", "0.9");
        
        var output = new TestLoggerOutput();
        
        // Act - Try multiple times to trigger sampling (90% chance each time)
        bool samplingTriggered = false;
        string logOutput = "";
        
        // Try up to 20 times to trigger sampling (probability of not triggering in 20 tries with 90% rate is ~0.000001%)
        for (int i = 0; i < 20 && !samplingTriggered; i++)
        {
            output.Clear();
            
            using var loggerFactory = CreateLoggerFactoryWithEnvironmentVariables(output);
            var logger = loggerFactory.CreateLogger<EnvironmentVariableSamplingTests>();
            
            // Log an error first (should always be logged)
            logger.LogError("This is an error message");
            
            // First info log will be skipped due to cold start protection
            logger.LogInformation("First info - skipped due to cold start protection");
            
            // Second info log should trigger sampling (90% chance)
            logger.LogInformation("This is an info message — should appear when sampling is triggered");
            
            // Third info log should also be logged if sampling was triggered
            logger.LogInformation("Another info message");
            
            logOutput = output.ToString();
            samplingTriggered = logOutput.Contains("Changed log level to DEBUG based on Sampling configuration");
        }

        // Assert
        Assert.True(samplingTriggered, "Sampling should have been triggered within 20 attempts with 90% rate");
        Assert.Contains("This is an error message", logOutput);
        Assert.Contains("This is an info message — should appear when sampling is triggered", logOutput);
        Assert.Contains("Another info message", logOutput);
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
}