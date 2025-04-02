using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Buffering
{
    [Collection("Sequential")]
    public class LogBufferingTests : IDisposable
    {
        private readonly TestLoggerOutput _consoleOut;

        public LogBufferingTests()
        {
            _consoleOut = new TestLoggerOutput();
        }

        [Trait("Category", "BufferManager")]
        [Fact]
        public void SetInvocationId_IsolatesLogsBetweenInvocations()
        {
            // Arrange
            var config = new PowertoolsLoggerConfiguration
            {
                LogBuffering = new LogBufferingOptions { Enabled = true },
                LogOutput = _consoleOut
            };

            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");

            // Act
            LogBufferManager.SetInvocationId("invocation-1");
            logger.LogDebug("Debug message from invocation 1");

            LogBufferManager.SetInvocationId("invocation-2");
            logger.LogDebug("Debug message from invocation 2");

            LogBufferManager.SetInvocationId("invocation-1");
            logger.LogError("Error message from invocation 1");

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Error message from invocation 1", output);
            Assert.Contains("Debug message from invocation 1", output);
            Assert.DoesNotContain("Debug message from invocation 2", output);
        }

        [Trait("Category", "BufferedLogger")]
        [Fact]
        public void BufferedLogger_OnlyBuffersConfiguredLogLevels()
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
            LogBufferManager.SetInvocationId("invocation-1");

            // Act
            logger.LogTrace("Trace message"); // Below buffer threshold, should be ignored
            logger.LogDebug("Debug message"); // Should be buffered
            logger.LogInformation("Info message"); // Above minimum, should be logged directly

            // Assert
            var output = _consoleOut.ToString();
            Assert.DoesNotContain("Trace message", output);
            Assert.DoesNotContain("Debug message", output); // Not flushed yet
            Assert.Contains("Info message", output);

            // Flush the buffer
            Logger.FlushBuffer();

            output = _consoleOut.ToString();
            Assert.Contains("Debug message", output); // Now should be visible
        }

        [Trait("Category", "BufferedLogger")]
        [Fact]
        public void FlushOnErrorLog_FlushesBufferWhenEnabled()
        {
            // Arrange
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug,
                    FlushOnErrorLog = true
                },
                LogOutput = _consoleOut
            };
            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");
            LogBufferManager.SetInvocationId("invocation-1");

            // Act
            logger.LogDebug("Debug message 1"); // Should be buffered
            logger.LogDebug("Debug message 2"); // Should be buffered
            logger.LogError("Error message"); // Should trigger flush of buffer

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug message 1", output);
            Assert.Contains("Debug message 2", output);
            Assert.Contains("Error message", output);
        }

        [Trait("Category", "BufferedLogger")]
        [Fact]
        public void ClearBuffer_RemovesAllBufferedLogs()
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
            LogBufferManager.SetInvocationId("invocation-1");

            // Act
            logger.LogDebug("Debug message 1"); // Should be buffered
            logger.LogDebug("Debug message 2"); // Should be buffered

            Logger.ClearBuffer(); // Should clear all buffered logs
            Logger.FlushBuffer(); // No logs should be output

            logger.LogDebug("Debug message 3"); // Should be buffered
            Logger.FlushBuffer(); // Should output debug message 3

            // Assert
            var output = _consoleOut.ToString();
            Assert.DoesNotContain("Debug message 1", output);
            Assert.DoesNotContain("Debug message 2", output);
            Assert.Contains("Debug message 3", output);
        }

        [Trait("Category", "BufferedLogger")]
        [Fact]
        public void BufferSizeLimit_DiscardOldestEntriesWhenExceeded()
        {
            // Arrange
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug,
                    MaxBytes = 1000 // Small buffer size to force overflow
                },
                LogOutput = _consoleOut
            };
            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");
            LogBufferManager.SetInvocationId("invocation-1");

            // Act
            // Add enough logs to exceed buffer size
            for (int i = 0; i < 20; i++)
            {
                logger.LogDebug($"Debug message {i} with enough characters to consume space in the buffer");
            }

            Logger.FlushBuffer();

            // Assert
            var output = _consoleOut.ToString();
            Assert.DoesNotContain("Debug message 0", output); // Older messages should be discarded
            Assert.Contains("Debug message 19", output); // Newest messages should be kept
        }

        [Trait("Category", "LoggerLifecycle")]
        [Fact]
        public void DisposingProvider_FlushesBufferedLogs()
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
            LogBufferManager.SetInvocationId("invocation-1");

            // Act
            logger.LogDebug("Debug message before disposal"); // Should be buffered
            provider.Dispose(); // Should flush buffer

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug message before disposal", output);
        }

        [Trait("Category", "LoggerIntegration")]
        [Fact]
        public void DirectLoggerAndBufferedLogger_WorkTogether()
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

            // Create both standard and buffering providers
            var standardProvider = new PowertoolsLoggerProvider(config, powertoolsConfig);
            var bufferingProvider = new BufferingLoggerProvider(config, powertoolsConfig);

            var standardLogger = standardProvider.CreateLogger("StandardLogger");
            var bufferedLogger = bufferingProvider.CreateLogger("BufferedLogger");

            LogBufferManager.SetInvocationId("test-invocation");

            // Act
            standardLogger.LogInformation("Direct info message");
            bufferedLogger.LogDebug("Buffered debug message");
            bufferedLogger.LogInformation("Direct info from buffered logger");

            // Assert - before flush
            var output = _consoleOut.ToString();
            Assert.Contains("Direct info message", output);
            Assert.Contains("Direct info from buffered logger", output);
            Assert.DoesNotContain("Buffered debug message", output);

            // Flush and check again
            Logger.FlushBuffer();
            output = _consoleOut.ToString();
            Assert.Contains("Buffered debug message", output);
        }

        [Trait("Category", "LoggerConfiguration")]
        [Fact]
        public void LoggerInitialization_RegistersWithBufferManager()
        {
            // Arrange
            var config = new PowertoolsLoggerConfiguration
            {
                LogBuffering = new LogBufferingOptions { Enabled = true },
                LogOutput = _consoleOut
            };

            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());

            // Act
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");

            LogBufferManager.SetInvocationId("test-id");
            logger.LogDebug("Test message");
            Logger.FlushBuffer();

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Test message", output);
        }

        [Trait("Category", "LoggerOutput")]
        [Fact]
        public void CustomLogOutput_ReceivesLogs()
        {
            // Arrange
            var customOutput = new TestLoggerOutput();
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Debug, // Set to Debug to ensure we log directly
                LogOutput = customOutput
            };

            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new PowertoolsLoggerProvider(config, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");

            // Act
            logger.LogDebug("Direct debug message");

            // Assert
            var output = customOutput.ToString();
            Assert.Contains("Direct debug message", output);
        }

        [Trait("Category", "LoggerIntegration")]
        [Fact]
        public void RegisteringMultipleProviders_AllWorkCorrectly()
        {
            // Arrange - create a clean configuration for this test
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

            PowertoolsLoggingBuilderExtensions.UpdateConfiguration(config);

            // Create providers using the shared configuration
            var env = new PowertoolsEnvironment();
            var powertoolsConfig = new PowertoolsConfigurations(env);

            var provider1 = new BufferingLoggerProvider(config, powertoolsConfig);
            var provider2 = new BufferingLoggerProvider(config, powertoolsConfig);

            var logger1 = provider1.CreateLogger("Logger1");
            var logger2 = provider2.CreateLogger("Logger2");

            LogBufferManager.SetInvocationId("shared-invocation");

            // Act
            logger1.LogDebug("Debug from logger1");
            logger2.LogDebug("Debug from logger2");
            Logger.FlushBuffer();

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug from logger1", output);
            Assert.Contains("Debug from logger2", output);
        }

        [Trait("Category", "LoggerLifecycle")]
        [Fact]
        public void RegisteringLogBufferManager_HandlesMultipleProviders()
        {
            // Ensure we start with clean state
            LogBufferManager.ResetForTesting();
            // Arrange
            var config = new PowertoolsLoggerConfiguration
            {
                LogBuffering = new LogBufferingOptions { Enabled = true },
                LogOutput = _consoleOut
            };

            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());

            // Create and register first provider
            var provider1 = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger1 = provider1.CreateLogger("Logger1");
            // Explicitly dispose and unregister first provider
            provider1.Dispose();

            // Now create and register a second provider
            var provider2 = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger2 = provider2.CreateLogger("Logger2");

            LogBufferManager.SetInvocationId("test-invocation");

            // Act
            logger1.LogDebug("Debug from first provider");
            logger2.LogDebug("Debug from second provider");

            // Only the second provider should be registered with the LogBufferManager
            Logger.FlushBuffer();

            // Assert
            var output = _consoleOut.ToString();
            // Only the second provider's logs should be flushed
            Assert.DoesNotContain("Debug from first provider", output);
            Assert.Contains("Debug from second provider", output);
        }

        [Trait("Category", "BufferEmpty")]
        [Fact]
        public void FlushingEmptyBuffer_DoesNotCauseErrors()
        {
            // Arrange
            LogBufferManager.ResetForTesting();
            var config = new PowertoolsLoggerConfiguration
            {
                LogBuffering = new LogBufferingOptions { Enabled = true },
                LogOutput = _consoleOut
            };
            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);

            // Act - flush without any logs
            LogBufferManager.SetInvocationId("empty-test");
            Logger.FlushBuffer();

            // Assert - should not throw exceptions
            Assert.Empty(_consoleOut.ToString());
        }

        [Trait("Category", "LogLevelThreshold")]
        [Fact]
        public void LogsAtExactBufferThreshold_AreBuffered()
        {
            // Arrange
            LogBufferManager.ResetForTesting();
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

            // Act
            LogBufferManager.SetInvocationId("threshold-test");
            logger.LogDebug("Debug message exactly at threshold"); // Should be buffered

            // Assert before flush
            Assert.DoesNotContain("Debug message exactly at threshold", _consoleOut.ToString());

            // After flush
            Logger.FlushBuffer();
            Assert.Contains("Debug message exactly at threshold", _consoleOut.ToString());
        }

        [Trait("Category", "LoggerDisabling")]
        [Fact]
        public void DisablingBuffering_StillLogsNormally()
        {
            // Arrange
            LogBufferManager.ResetForTesting();
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Debug,
                LogBuffering = new LogBufferingOptions
                {
                    Enabled = false // Buffering disabled
                },
                LogOutput = _consoleOut
            };
            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");

            // Act
            LogBufferManager.SetInvocationId("disabled-test");
            logger.LogDebug("Debug message with buffering disabled");

            // Assert - should log immediately even without flushing
            Assert.Contains("Debug message with buffering disabled", _consoleOut.ToString());
        }

        [Trait("Category", "MultipleInvocations")]
        [Fact]
        public void SwitchingBetweenInvocations_PreservesSeparateBuffers()
        {
            // Arrange
            LogBufferManager.ResetForTesting();
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions { Enabled = true },
                LogOutput = _consoleOut
            };
            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");

            // Act
            // First invocation
            LogBufferManager.SetInvocationId("invocation-A");
            logger.LogDebug("Debug for invocation A");

            // Switch to second invocation
            LogBufferManager.SetInvocationId("invocation-B");
            logger.LogDebug("Debug for invocation B");
            Logger.FlushBuffer(); // Only flush B

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug for invocation B", output);
            Assert.DoesNotContain("Debug for invocation A", output);

            // Now flush A
            LogBufferManager.SetInvocationId("invocation-A");
            Logger.FlushBuffer();

            output = _consoleOut.ToString();
            Assert.Contains("Debug for invocation A", output);
        }

        [Trait("Category", "ConfigurationUpdate")]
        [Fact]
        public void ChangingConfigurationDynamically_UpdatesBufferingBehavior()
        {
            // Arrange
            LogBufferManager.ResetForTesting();
            var initialConfig = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Warning, // Keep this as Warning
                LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Information // Buffer at info level
                },
                LogOutput = _consoleOut
            };

            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
    
            // Create provider with initial config
            var provider = new BufferingLoggerProvider(initialConfig, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");

            LogBufferManager.SetInvocationId("config-test");

            // Act - with initial config
            logger.LogInformation("Info message with initial config");
    
            // Should be buffered (Info < Warning minimum level)
            Assert.DoesNotContain("Info message with initial config", _consoleOut.ToString());

            // Update config to not buffer info anymore
            var updatedConfig = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information, // Changed to Information
                LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug // Only buffer debug level now
                },
                LogOutput = _consoleOut
            };

            // Directly update the provider's configuration
            provider.UpdateConfiguration(updatedConfig);
    
            // Log with updated config
            logger.LogInformation("Info message with updated config");
    
            // Assert - should log immediately with updated config
            Assert.Contains("Info message with updated config", _consoleOut.ToString());
    
            // Flush and check if first message appears
            Logger.FlushBuffer();
            Assert.Contains("Info message with initial config", _consoleOut.ToString());
        }

        public void Dispose()
        {
            // Clean up all state between tests
            Logger.ClearBuffer();
            LogBufferManager.ResetForTesting();
        }
    }
}