using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
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
                LogBuffering = new LogBufferingOptions(),
                LogOutput = _consoleOut
            };

            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

            // Act

            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
            logger.LogDebug("Debug message from invocation 1");

            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-2");
            logger.LogDebug("Debug message from invocation 2");

            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
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
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
            
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    BufferAtLogLevel = LogLevel.Trace
                },
                LogOutput = _consoleOut
            };
            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

            // Act
            logger.LogTrace("Trace message"); // should buffer
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
            Assert.Contains("Trace message", output); // Now should be visible
        }
        
        [Trait("Category", "BufferedLogger")]
        [Fact]
        public void BufferedLogger_Buffer_Takes_Precedence_Same_Level()
        {
            // Arrange
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
            
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    BufferAtLogLevel = LogLevel.Information
                },
                LogOutput = _consoleOut
            };
            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

            // Act
            logger.LogTrace("Trace message"); // Below buffer threshold, should be ignored
            logger.LogDebug("Debug message"); // Should be buffered
            logger.LogInformation("Info message"); // Above minimum, should be logged directly

            // Assert
            var output = _consoleOut.ToString();
            Assert.Empty(output);

            // Flush the buffer
            Logger.FlushBuffer();

            output = _consoleOut.ToString();
            Assert.Contains("Info message", output); // Now should be visible
        }
        
        [Trait("Category", "BufferedLogger")]
        [Fact]
        public void BufferedLogger_Buffer_Takes_Precedence_Higher_Level()
        {
            // Arrange
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
            
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    BufferAtLogLevel = LogLevel.Warning
                },
                LogOutput = _consoleOut
            };
            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

            // Act
            logger.LogWarning("Warning message"); // Should be buffered
            logger.LogInformation("Info message"); // Should be buffered

            // Assert
            var output = _consoleOut.ToString();
            Assert.Empty(output);

            // Flush the buffer
            Logger.FlushBuffer();

            output = _consoleOut.ToString();
            Assert.DoesNotContain("Info message", output); // Now should be visible
            Assert.Contains("Warning message", output);
        }
        
        [Trait("Category", "BufferedLogger")]
        [Fact]
        public void BufferedLogger_Buffer_Log_Level_Error_Does_Not_Buffer()
        {
            // Arrange
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
            
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    BufferAtLogLevel = LogLevel.Error
                },
                LogOutput = _consoleOut
            };
            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

            // Act
            logger.LogError("Error message"); // Should be buffered
            logger.LogInformation("Info message"); // Should be buffered

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Error message", output);
            Assert.Contains("Info message", output);
        }

        [Trait("Category", "BufferedLogger")]
        [Fact]
        public void FlushOnErrorLog_FlushesBufferWhenEnabled()
        {
            // Arrange
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    BufferAtLogLevel = LogLevel.Debug,
                    FlushOnErrorLog = true
                },
                LogOutput = _consoleOut
            };

            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

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
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    BufferAtLogLevel = LogLevel.Debug
                },
                LogOutput = _consoleOut
            };
            
            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();
            

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
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    BufferAtLogLevel = LogLevel.Debug,
                    MaxBytes = 1000 // Small buffer size to force overflow
                },
                LogOutput = _consoleOut
            };
            
            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

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
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    BufferAtLogLevel = LogLevel.Debug
                },
                LogOutput = _consoleOut
            };

            var provider = LoggerFactoryHelper.CreateAndConfigureFactory(config);
            var logger = provider.CreatePowertoolsLogger();

            // Act
            logger.LogDebug("Debug message before disposal"); // Should be buffered
            provider.Dispose(); // Should flush buffer

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug message before disposal", output);
        }

        [Trait("Category", "LoggerConfiguration")]
        [Fact]
        public void LoggerInitialization_RegistersWithBufferManager()
        {
            // Arrange
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "test-id");
            var config = new PowertoolsLoggerConfiguration
            {
                LogBuffering = new LogBufferingOptions(),
                LogOutput = _consoleOut
            };

            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

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

            var logger = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();

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
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "shared-invocation");
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
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
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "test-invocation");
            // Arrange
            var config = new PowertoolsLoggerConfiguration
            {
                LogBuffering = new LogBufferingOptions(),
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
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "empty-test");
            var config = new PowertoolsLoggerConfiguration
            {
                LogBuffering = new LogBufferingOptions(),
                LogOutput = _consoleOut
            };
            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);

            // Act - flush without any logs
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
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "threshold-test");
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions
                {
                    BufferAtLogLevel = LogLevel.Debug
                },
                LogOutput = _consoleOut
            };
            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");

            // Act
            logger.LogDebug("Debug message exactly at threshold"); // Should be buffered

            // Assert before flush
            Assert.DoesNotContain("Debug message exactly at threshold", _consoleOut.ToString());

            // After flush
            Logger.FlushBuffer();
            Assert.Contains("Debug message exactly at threshold", _consoleOut.ToString());
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
                LogBuffering = new LogBufferingOptions(),
                LogOutput = _consoleOut
            };
            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            var provider = new BufferingLoggerProvider(config, powertoolsConfig);
            var logger = provider.CreateLogger("TestLogger");

            // Act
            // First invocation
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-A");
            logger.LogDebug("Debug for invocation A");

            // Switch to second invocation
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-B");
            logger.LogDebug("Debug for invocation B");
            Logger.FlushBuffer(); // Only flush B

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug for invocation B", output);
            Assert.DoesNotContain("Debug for invocation A", output);

            // Now flush A
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-A");
            Logger.FlushBuffer();

            output = _consoleOut.ToString();
            Assert.Contains("Debug for invocation A", output);
        }

        public void Dispose()
        {
            // Clean up all state between tests
            Logger.ClearBuffer();
            LogBufferManager.ResetForTesting();
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", null);
        }
    }
}