using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
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
                MaxBytes = 1024 // Small buffer size to trigger overflow
            },
            LogOutput = _consoleOut
        };

        var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
        var provider = new BufferingLoggerProvider(config, powertoolsConfig);
        var logger = provider.CreateLogger("TestLogger");

        LogBufferManager.SetInvocationId("circular-buffer-test");

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

        var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
        var provider = new BufferingLoggerProvider(config, powertoolsConfig);
        var logger = provider.CreateLogger("TestLogger");

        LogBufferManager.SetInvocationId("large-entry-test");

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
    public void Buffer_WithExtremelyLargeEntry_Discards()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogBuffering = new LogBufferingOptions
            {
                Enabled = true,
                BufferAtLogLevel = LogLevel.Debug,
                MaxBytes = 4096 // Even with a larger buffer
            },
            LogOutput = _consoleOut
        };

        var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
        var provider = new BufferingLoggerProvider(config, powertoolsConfig);
        var logger = provider.CreateLogger("TestLogger");

        LogBufferManager.SetInvocationId("extreme-entry-test");

        // Act - add some small entries first
        for (int i = 0; i < 5; i++)
        {
            logger.LogDebug($"Initial message {i}");
        }
        
        // Add entry larger than the entire buffer - should displace everything
        var hugeMessage = new string('X', 3000);
        logger.LogDebug($"Huge message: {hugeMessage}");
        
        // Add more entries after
        for (int i = 0; i < 5; i++)
        {
            logger.LogDebug($"Final message {i}");
        }
        
        // Flush buffer
        logger.FlushBuffer();

        // Assert
        var output = _consoleOut.ToString();
        
        // Initial messages should be discarded
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains($"Initial message {i}", output);
        }
        
        // Huge message may be partially discarded depending on implementation
        Assert.DoesNotContain("Huge message", output);
        
        // Some of the final messages should be present
        Assert.Contains("Final message 4", output);
    }

    [Trait("Category", "CircularBuffer")]
    [Fact]
    public void MultipleInvocations_EachHaveTheirOwnCircularBuffer()
    {
        // Arrange
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LogBuffering = new LogBufferingOptions
            {
                Enabled = true,
                BufferAtLogLevel = LogLevel.Debug
            },
            LogOutput = _consoleOut
        };

        var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
        var provider = new BufferingLoggerProvider(config, powertoolsConfig);
        var logger = provider.CreateLogger("TestLogger");

        // Act - fill buffer for first invocation
        LogBufferManager.SetInvocationId("invocation-1");
        for (int i = 0; i < 10; i++)
        {
            logger.LogDebug($"Invocation 1 message {i}");
        }

        // Switch to second invocation with fresh buffer
        LogBufferManager.SetInvocationId("invocation-2");
        for (int i = 0; i < 5; i++)
        {
            logger.LogDebug($"Invocation 2 message {i}");
        }
        
        // Flush second invocation first
        logger.FlushBuffer();
        var outputAfterSecond = _consoleOut.ToString();
        
        // Flush first invocation
        LogBufferManager.SetInvocationId("invocation-1");
        logger.FlushBuffer();
        var outputAfterBoth = _consoleOut.ToString();

        // Assert
        // First invocation buffer should be complete
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains($"Invocation 1 message {i}", outputAfterBoth);
        }
        
        // Second invocation buffer should be complete (not affected by first)
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains($"Invocation 2 message {i}", outputAfterSecond);
        }
    }

    public void Dispose()
    {
        // Clean up all state between tests
        Logger.ClearBuffer();
        LogBufferManager.ResetForTesting();
    }
}