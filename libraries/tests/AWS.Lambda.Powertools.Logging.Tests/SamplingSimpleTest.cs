using System;
using System.Collections.Generic;
using System.Linq;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests;

/// <summary>
/// Simple tests for log sampling functionality without complex mocks
/// </summary>
public class SamplingSimpleTest
{
    /// <summary>
    /// Test that proves the GetSafeRandom method works correctly
    /// </summary>
    [Fact]
    public void GetSafeRandom_ShouldReturnValueBetweenZeroAndOne()
    {
        // Act & Assert - Test multiple times to ensure consistency
        for (int i = 0; i < 100; i++)
        {
            var randomValue = PowertoolsLoggerConfiguration.GetSafeRandom();
            
            Assert.True(randomValue >= 0.0, $"Random value {randomValue} should be >= 0.0");
            Assert.True(randomValue <= 1.0, $"Random value {randomValue} should be <= 1.0");
        }
    }

    /// <summary>
    /// Test that proves GetSafeRandom generates different values
    /// </summary>
    [Fact]
    public void GetSafeRandom_ShouldReturnDifferentValues()
    {
        // Arrange
        var values = new HashSet<double>();
        
        // Act - Generate multiple random values
        for (int i = 0; i < 100; i++)
        {
            values.Add(PowertoolsLoggerConfiguration.GetSafeRandom());
        }
        
        // Assert - Should have generated multiple different values
        Assert.True(values.Count > 50, "Should generate diverse random values");
    }

    /// <summary>
    /// Test that RefreshSampleRateCalculation method exists and can be called
    /// </summary>
    [Fact]
    public void RefreshSampleRateCalculation_ShouldExistAndBeCallable()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 0.5,
            MinimumLogLevel = LogLevel.Warning,
            InitialLogLevel = LogLevel.Warning,
            LogOutput = new TestLoggerOutput()
        };

        // Act & Assert - Should not throw exception
        config.RefreshSampleRateCalculation();
        
        // First call should be skipped (cold start protection)
        Assert.Equal(1, config.SamplingRefreshCount);
        
        // Second call should potentially change log level
        var initialLogLevel = config.MinimumLogLevel;
        config.RefreshSampleRateCalculation();
        
        // The log level might change or stay the same depending on random value
        // But the method should execute without error
        Assert.True(config.SamplingRefreshCount >= 1);
    }

    /// <summary>
    /// Test that sampling with 0% rate never changes log level
    /// </summary>
    [Fact]
    public void RefreshSampleRateCalculation_WithZeroRate_ShouldNeverChangeLogLevel()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 0.0, // 0% sampling
            MinimumLogLevel = LogLevel.Warning,
            InitialLogLevel = LogLevel.Warning,
            LogOutput = new TestLoggerOutput()
        };

        var initialLogLevel = config.MinimumLogLevel;

        // Act - Call multiple times
        for (int i = 0; i < 10; i++)
        {
            config.RefreshSampleRateCalculation();
        }

        // Assert - Log level should never change with 0% sampling
        Assert.Equal(initialLogLevel, config.MinimumLogLevel);
    }

    /// <summary>
    /// Test that sampling with 100% rate should change log level after first call
    /// </summary>
    [Fact]
    public void RefreshSampleRateCalculation_With100PercentRate_ShouldEventuallyChangeLogLevel()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 1.0, // 100% sampling
            MinimumLogLevel = LogLevel.Warning,
            InitialLogLevel = LogLevel.Warning,
            LogOutput = new TestLoggerOutput()
        };

        // Act - First call should be skipped (cold start protection)
        config.RefreshSampleRateCalculation();
        Assert.Equal(LogLevel.Warning, config.MinimumLogLevel); // Should remain unchanged

        // Second call should change to Debug with 100% sampling
        config.RefreshSampleRateCalculation();

        // Assert - With 100% sampling, should change to Debug
        Assert.Equal(LogLevel.Debug, config.MinimumLogLevel);
    }

    /// <summary>
    /// Test that demonstrates the fix works - sampling should vary over multiple calls
    /// </summary>
    [Fact]
    public void RefreshSampleRateCalculation_WithMediumRate_ShouldShowVariation()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            SamplingRate = 0.5, // 50% sampling
            MinimumLogLevel = LogLevel.Warning,
            InitialLogLevel = LogLevel.Warning,
            LogOutput = new TestLoggerOutput()
        };

        var debugActivations = 0;
        const int iterations = 100;

        // Act - Skip first call (cold start protection)
        config.RefreshSampleRateCalculation();

        // Now test multiple calls
        for (int i = 0; i < iterations; i++)
        {
            config.RefreshSampleRateCalculation();
            if (config.MinimumLogLevel == LogLevel.Debug)
            {
                debugActivations++;
            }
        }

        // Assert - With 50% sampling, should see some variation
        // Allow for reasonable variance in randomness
        Assert.True(debugActivations > iterations * 0.2, 
            $"Expected at least 20% debug activations, got {debugActivations}/{iterations}");
        Assert.True(debugActivations < iterations * 0.8, 
            $"Expected at most 80% debug activations, got {debugActivations}/{iterations}");
    }
}
