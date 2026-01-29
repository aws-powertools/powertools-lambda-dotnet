using System;
using System.IO;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Buffering;

public class LogBufferCircularCacheTests : IDisposable
{
    private readonly TestLoggerOutput _consoleOut;

    public LogBufferCircularCacheTests()
    {
        _consoleOut = new TestLoggerOutput();
        LogBufferManager.ResetForTesting();
    }

    [Trait("Category", "CircularBuffer")]
    [Fact]
    public void Buffer_WhenMaxSizeExceeded_DiscardOldestEntries()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogBuffering = new LogBufferingOptions
            {
                Enabled = true,
                BufferAtLogLevel = LogLevel.Debug,
                MaxBytes = 1200 // Small buffer size to trigger overflow - Needs to be adjusted based on the log message size
            },
            LogOutput = _consoleOut
        };

        var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "circular-buffer-test");

        // Act - add many debug logs to fill buffer
        for (int i = 0; i < 5; i++)
        {
            logger.LogDebug($"Old debug message {i} that should be removed");
        }
        
        // Add more logs that should push out the older ones
        for (int i = 0; i < 5; i++)
        {
            logger.LogDebug($"New debug message {i} that should remain");
        }
        
        // Flush buffer
        logger.FlushBuffer();

        // Assert
        var output = _consoleOut.ToString();
        
        // First entries should be discarded
        Assert.DoesNotContain("Old debug message 0", output);
        Assert.DoesNotContain("Old debug message 1", output);
        
        // Later entries should be present
        Assert.Contains("New debug message 3", output);
        Assert.Contains("New debug message 4", output);
    }
    
    [Trait("Category", "CircularBuffer")]
    [Fact]
    public void Buffer_WhenMaxSizeExceeded_DiscardOldestEntries_Warn()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogBuffering = new LogBufferingOptions
            {
                Enabled = true,
                BufferAtLogLevel = LogLevel.Debug,
                MaxBytes = 1024 // Small buffer size to trigger overflow
            },
            LogOutput = _consoleOut
        };

        var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "circular-buffer-test");

        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        
        // Act - add many debug logs to fill buffer
        for (int i = 0; i < 5; i++)
        {
            logger.LogDebug($"Old debug message {i} that should be removed");
        }
        
        // Add more logs that should push out the older ones
        for (int i = 0; i < 5; i++)
        {
            logger.LogDebug($"New debug message {i} that should remain");
        }
        
        // Flush buffer
        logger.FlushBuffer();

        // Assert
        var st = stringWriter.ToString();
        Assert.Contains("Some logs are not displayed because they were evicted from the buffer. Increase buffer size to store more logs in the buffer", st);
    }
    
    [Trait("Category", "CircularBuffer")]
    [Fact]
    public void Buffer_WhenMaxSizeExceeded_DiscardOldestEntries_Warn_With_Warning_Level()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogBuffering = new LogBufferingOptions
            {
                Enabled = true,
                BufferAtLogLevel = LogLevel.Warning,
                MaxBytes = 1024 // Small buffer size to trigger overflow
            },
            LogOutput = _consoleOut
        };

        var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "circular-buffer-test");

        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        
        // Act - add many debug logs to fill buffer
        for (int i = 0; i < 5; i++)
        {
            logger.LogDebug($"Old debug message {i} that should be removed");
        }
        
        // Add more logs that should push out the older ones
        for (int i = 0; i < 5; i++)
        {
            logger.LogDebug($"New debug message {i} that should remain");
        }
        
        // Flush buffer
        logger.FlushBuffer();

        // Assert
        var st = stringWriter.ToString();
        Assert.Contains("Some logs are not displayed because they were evicted from the buffer. Increase buffer size to store more logs in the buffer", st);
    }

    [Trait("Category", "CircularBuffer")]
    [Fact]
    public void Buffer_WithLargeLogEntry_DiscardsManySmallEntries()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogBuffering = new LogBufferingOptions
            {
                Enabled = true,
                BufferAtLogLevel = LogLevel.Debug,
                MaxBytes = 2048 // Small buffer size to trigger overflow
            },
            LogOutput = _consoleOut
        };

        var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "large-entry-test");

        // Act - add many small entries first
        for (int i = 0; i < 10; i++)
        {
            logger.LogDebug($"Small message {i}");
        }
        
        // Add one very large entry that should displace many small ones
        var largeMessage = new string('X', 80); // Large enough to push out multiple small entries
        logger.LogDebug($"Large message: {largeMessage}");
        
        // Flush buffer
        logger.FlushBuffer();

        // Assert
        var output = _consoleOut.ToString();
        
        // Several early small messages should be discarded
        for (int i = 0; i < 5; i++)
        {
            Assert.DoesNotContain($"Small message {i}", output);
        }
        
        // Large message should be present
        Assert.Contains("Large message: XXXX", output);
        
        // Some later small messages should remain
        Assert.Contains("Small message 9", output);
    }

    [Trait("Category", "CircularBuffer")]
    [Fact]
    public void Buffer_WithExtremelyLargeEntry_Logs_Directly_And_Warning()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogBuffering = new LogBufferingOptions
            {
                Enabled = true,
                BufferAtLogLevel = LogLevel.Debug,
                MaxBytes = 5096 // Even with a larger buffer
            },
            LogOutput = _consoleOut
        };

        var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "extreme-entry-test");

        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        
        // Act - add some small entries first
        for (int i = 0; i < 4; i++)
        {
            logger.LogDebug($"Initial message {i}");
        }
        
        // Add entry larger than the entire buffer - should displace everything
        var hugeMessage = new string('X', 3000);
        logger.LogDebug($"Huge message: {hugeMessage}");
        
        var st = stringWriter.ToString();
        Assert.Contains("Cannot add item to the buffer", st);
        
        // Add more entries after
        for (int i = 0; i < 4; i++)
        {
            logger.LogDebug($"Final message {i}");
        }
        
        // Flush buffer
        logger.FlushBuffer();

        // Assert
        var output = _consoleOut.ToString();
        
        // Initial messages should be discarded
        for (int i = 0; i < 4; i++)
        {
            Assert.Contains($"Initial message {i}", output);
        }
        
        // Some of the final messages should be present
        Assert.Contains("Final message 3", output);
    }

    public void Dispose()
    {
        // Clean up all state between tests
        Logger.ClearBuffer();
        LogBufferManager.ResetForTesting();
        Logger.Reset();
        Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", null);
    }
}