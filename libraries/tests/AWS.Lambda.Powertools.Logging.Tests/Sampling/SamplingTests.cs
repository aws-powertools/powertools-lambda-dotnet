using System;
using System.Collections.Generic;
using System.Linq;
using AWS.Lambda.Powertools.Common.Tests;
using Xunit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AWS.Lambda.Powertools.Logging.Tests.Sampling;

public class SamplingTests : IDisposable
{
    private readonly string _originalLogLevel;
    private readonly string _originalSampleRate;

    public SamplingTests()
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

    [Fact]
    public void SamplingRate_WhenConfigured_ShouldEnableDebugSampling()
    {
        // Arrange
        var output = new TestLoggerOutput();
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 1.0, // 100% sampling rate
            LogOutput = output
        };

        // Act
        var result = config.ShouldEnableDebugSampling();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SamplingRate_WhenZero_ShouldNotEnableDebugSampling()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 0.0 // 0% sampling rate
        };

        // Act
        var result = config.ShouldEnableDebugSampling();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RefreshSampleRateCalculation_FirstCall_ShouldReturnFalseDueToColdStartProtection()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 1.0, // 100% sampling rate
            InitialLogLevel = LogLevel.Error,
            MinimumLogLevel = LogLevel.Error
        };

        // Act
        var result = config.RefreshSampleRateCalculation();

        // Assert
        Assert.False(result); // Cold start protection should prevent sampling on first call
    }

    [Fact]
    public void RefreshSampleRateCalculation_SecondCall_WithFullSampling_ShouldReturnTrue()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 1.0, // 100% sampling rate
            InitialLogLevel = LogLevel.Error,
            MinimumLogLevel = LogLevel.Error
        };

        // Act
        var firstResult = config.RefreshSampleRateCalculation(); // Cold start protection
        var secondResult = config.RefreshSampleRateCalculation(); // Should enable sampling

        // Assert
        Assert.False(firstResult); // Cold start protection
        Assert.True(secondResult); // Should enable sampling
        Assert.Equal(LogLevel.Debug, config.MinimumLogLevel); // Should have changed to Debug
    }

    [Fact]
    public void RefreshSampleRateCalculation_WithZeroSampling_ShouldNeverEnableSampling()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 0.0, // 0% sampling rate
            InitialLogLevel = LogLevel.Error,
            MinimumLogLevel = LogLevel.Error
        };

        // Act
        var firstResult = config.RefreshSampleRateCalculation();
        var secondResult = config.RefreshSampleRateCalculation();

        // Assert
        Assert.False(firstResult);
        Assert.False(secondResult);
        Assert.Equal(LogLevel.Error, config.MinimumLogLevel); // Should remain unchanged
    }

    [Fact]
    public void Logger_WithSamplingEnabled_ShouldLogDebugWhenSamplingTriggered()
    {
        // Arrange
        var output = new TestLoggerOutput();
        Logger.Configure(options =>
        {
            options.Service = "TestService";
            options.SamplingRate = 1.0; // 100% sampling
            options.MinimumLogLevel = LogLevel.Error;
            options.LogOutput = output;
        });

        // Act
        Logger.LogError("This is an error"); // Trigger first call (cold start protection)
        Logger.LogInformation("This should be logged due to sampling"); // Should trigger sampling

        // Assert
        var logOutput = output.ToString();
        Assert.Contains("This is an error", logOutput);
        Assert.Contains("This should be logged due to sampling", logOutput);
        Assert.Contains("Changed log level to DEBUG based on Sampling configuration", logOutput);
    }

    [Fact]
    public void Logger_WithNoSampling_ShouldNotLogDebugMessages()
    {
        // Arrange
        var output = new TestLoggerOutput();
        Logger.Configure(options =>
        {
            options.Service = "TestService";
            options.SamplingRate = 0.0; // 0% sampling
            options.MinimumLogLevel = LogLevel.Error;
            options.LogOutput = output;
        });

        // Act
        Logger.LogError("This is an error");
        Logger.LogInformation("This should NOT be logged");

        // Assert
        var logOutput = output.ToString();
        Assert.Contains("This is an error", logOutput);
        Assert.DoesNotContain("This should NOT be logged", logOutput);
        Assert.DoesNotContain("Changed log level to DEBUG based on Sampling configuration", logOutput);
    }

    [Fact]
    public void ShouldEnableDebugSampling_WithOutParameter_ShouldReturnSamplerValue()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 1.0 // 100% sampling
        };

        // Act
        var result = config.ShouldEnableDebugSampling(out double samplerValue);

        // Assert
        Assert.True(result);
        Assert.True(samplerValue >= 0.0 && samplerValue <= 1.0);
        Assert.True(samplerValue <= config.SamplingRate);
    }

    [Fact]
    public void RefreshSampleRateCalculation_WithOutParameter_ShouldProvideSamplerValue()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 1.0, // 100% sampling
            InitialLogLevel = LogLevel.Error,
            MinimumLogLevel = LogLevel.Error
        };

        // Act
        var firstResult = config.RefreshSampleRateCalculation(out double firstSamplerValue);
        var secondResult = config.RefreshSampleRateCalculation(out double secondSamplerValue);

        // Assert
        Assert.False(firstResult); // Cold start protection
        Assert.Equal(0.0, firstSamplerValue); // Should be 0 during cold start protection

        Assert.True(secondResult); // Should enable sampling
        Assert.True(secondSamplerValue >= 0.0 && secondSamplerValue <= 1.0);
    }

    [Fact]
    public void GetSafeRandom_ShouldReturnValueBetweenZeroAndOne()
    {
        // Act
        var randomValue = PowertoolsLoggerConfiguration.GetSafeRandom();

        // Assert
        Assert.True(randomValue >= 0.0);
        Assert.True(randomValue <= 1.0);
    }

    [Fact]
    public void SamplingRefreshCount_ShouldIncrementCorrectly()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 1.0
        };

        // Act & Assert
        Assert.Equal(0, config.SamplingRefreshCount);

        config.RefreshSampleRateCalculation();
        Assert.Equal(1, config.SamplingRefreshCount);

        config.RefreshSampleRateCalculation();
        Assert.Equal(2, config.SamplingRefreshCount);
    }
    
    [Fact]
    public void RefreshSampleRateCalculation_ShouldEnableDebugLogging()
    {
        // Arrange
        var output = new TestLoggerOutput();
        Logger.Configure(options =>
        {
            options.Service = "TestService";
            options.SamplingRate = 1.0; // 100% sampling
            options.MinimumLogLevel = LogLevel.Error;
            options.LogOutput = output;
        });

        // Act - First refresh (cold start protection)
        Logger.RefreshSampleRateCalculation();
        Logger.LogDebug("This should not appear");
    
        // Clear output from first attempt
        output.Clear();
    
        // Second refresh (should enable sampling)
        Logger.RefreshSampleRateCalculation();
        Logger.LogDebug("This should appear after sampling");

        // Assert
        var logOutput = output.ToString();
        Assert.Contains("This should appear after sampling", logOutput);
        Assert.Contains("Changed log level to DEBUG based on Sampling configuration", logOutput);
    }

    [Fact]
    public void Logger_RefreshSampleRateCalculation_ShouldTriggerConfigurationUpdate()
    {
        // Arrange
        var output = new TestLoggerOutput();
        Logger.Configure(options =>
        {
            options.Service = "TestService";
            options.SamplingRate = 1.0; // 100% sampling  
            options.MinimumLogLevel = LogLevel.Warning;
            options.LogOutput = output;
        });

        // Verify initial state - debug logs should not appear (sampling not yet triggered)
        Logger.LogDebug("Initial debug - should not appear");
        Assert.DoesNotContain("Initial debug", output.ToString());

        output.Clear();

        // Act - Trigger sampling refresh
        // First call is protected by cold start logic, second call should enable sampling
        Logger.RefreshSampleRateCalculation(); // Cold start protection - no effect
        var samplingEnabled = Logger.RefreshSampleRateCalculation(); // Should enable debug sampling

        // Verify sampling was enabled
        Assert.True(samplingEnabled, "Sampling should be enabled with 100% rate");

        // Now debug logs should appear because sampling elevated the log level
        Logger.LogDebug("Debug after sampling - should appear");

        // Assert
        var logOutput = output.ToString();
        Assert.Contains("Debug after sampling - should appear", logOutput);
        Assert.Contains("Changed log level to DEBUG based on Sampling configuration", logOutput);
    }


    
    

    [Fact]
    public void RefreshSampleRateCalculation_ShouldGenerateRandomValues_OverMultipleIterations()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 0.5, // 50% sampling rate
            MinimumLogLevel = LogLevel.Error,
            InitialLogLevel = LogLevel.Error
        };

        var samplerValues = new List<double>();
        bool samplingTriggeredAtLeastOnce = false;
        bool samplingNotTriggeredAtLeastOnce = false;

        // Act - Try up to 20 times to verify random behavior
        for (int i = 0; i < 20; i++)
        {
            // Reset for each iteration
            config.SamplingRefreshCount = 1; // Skip cold start protection

            bool wasTriggered = config.RefreshSampleRateCalculation(out double samplerValue);
            samplerValues.Add(samplerValue);

            if (wasTriggered)
            {
                samplingTriggeredAtLeastOnce = true;
            }
            else
            {
                samplingNotTriggeredAtLeastOnce = true;
            }
        }

        // Assert
        // Verify that we got different random values (not all the same)
        var uniqueValues = samplerValues.Distinct().Count();
        Assert.True(uniqueValues > 1, "Should generate different random values across iterations");

        // With 50% sampling rate over 20 iterations, we should see both triggered and not triggered cases
        // (probability of all same outcome is extremely low: 0.5^20 ≈ 0.000001)
        Assert.True(samplingTriggeredAtLeastOnce,
            "Sampling should have been triggered at least once in 20 iterations with 50% rate");
        Assert.True(samplingNotTriggeredAtLeastOnce,
            "Sampling should have been skipped at least once in 20 iterations with 50% rate");

        // Verify all sampler values are within valid range [0, 1]
        Assert.True((bool)samplerValues.All(v => v >= 0.0 && v <= 1.0), "All sampler values should be between 0 and 1");
    }
}