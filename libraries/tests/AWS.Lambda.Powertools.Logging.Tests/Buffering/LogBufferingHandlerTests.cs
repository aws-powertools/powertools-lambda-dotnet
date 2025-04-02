using System;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.Logging.Tests.Buffering
{
    [Collection("Sequential")]
    public class LogBufferingHandlerTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly TestLoggerOutput _consoleOut;

        public LogBufferingHandlerTests(ITestOutputHelper output)
        {
            _output = output;
            _consoleOut = new TestLoggerOutput();
            LogBufferManager.ResetForTesting();
        }

        [Fact]
        public void BasicBufferingBehavior_BuffersDebugLogsOnly()
        {
            // Arrange
            var logger = CreateLogger(LogLevel.Information, true, LogLevel.Debug);
            var handler = new HandlerWithoutFlush(logger); // Use a handler that doesn't flush
            LogBufferManager.SetInvocationId("test-invocation");

            // Act - log messages without flushing
            handler.TestMethod();

            // Assert - before flush
            var outputBeforeFlush = _consoleOut.ToString();
            Assert.Contains("Information message", outputBeforeFlush);
            Assert.Contains("Error message", outputBeforeFlush);
            Assert.Contains("custom-key", outputBeforeFlush);
            Assert.Contains("custom-value", outputBeforeFlush);
            Assert.DoesNotContain("Debug message", outputBeforeFlush); // Debug should be buffered

            // Now flush the buffer
            Logger.FlushBuffer();
    
            // Assert - after flush
            var outputAfterFlush = _consoleOut.ToString();
            Assert.Contains("Debug message", outputAfterFlush); // Debug should now be present
        }

        [Fact]
        public void DisabledBuffering_LogsAllLevelsDirectly()
        {
            // Arrange
            var logger = CreateLogger(LogLevel.Debug, false, LogLevel.Debug);
            var handler = new Handlers(logger);
            LogBufferManager.SetInvocationId("test-invocation");

            // Act
            handler.TestMethod();

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Information message", output);
            Assert.Contains("Error message", output);
            Assert.Contains("Debug message", output); // Should be logged directly
        }

        [Fact]
        public void FlushOnErrorEnabled_AutomaticallyFlushesBuffer()
        {
            // Arrange
            var logger = CreateLoggerWithFlushOnError(true);
            LogBufferManager.SetInvocationId("test-invocation");

            // Act - with custom handler that doesn't manually flush
            var handler = new CustomHandlerWithoutFlush(logger);
            handler.TestMethod();

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug message", output); // Should be flushed by error log
            Assert.Contains("Error triggering flush", output);
        }

        [Fact]
        public void FlushOnErrorDisabled_DoesNotAutomaticallyFlushBuffer()
        {
            // Arrange
            var logger = CreateLoggerWithFlushOnError(false);
            LogBufferManager.SetInvocationId("test-invocation");

            // Act
            var handler = new CustomHandlerWithoutFlush(logger);
            handler.TestMethod();

            // Assert
            var output = _consoleOut.ToString();
            Assert.DoesNotContain("Debug message", output); // Should remain buffered
            Assert.Contains("Error triggering flush", output);
        }

        [Fact]
        public void ClearingBuffer_RemovesBufferedLogs()
        {
            // Arrange
            var logger = CreateLogger(LogLevel.Information, true, LogLevel.Debug);
            LogBufferManager.SetInvocationId("test-invocation");

            // Act
            var handler = new ClearBufferHandler(logger);
            handler.TestMethod();

            // Assert
            var output = _consoleOut.ToString();
            Assert.DoesNotContain("Debug message before clear", output);
            Assert.Contains("Debug message after clear", output);
        }

        [Fact]
        public void MultipleInvocations_IsolateLogBuffers()
        {
            // Arrange
            var logger = CreateLogger(LogLevel.Information, true, LogLevel.Debug);
            var handler = new Handlers(logger);

            // Act
            LogBufferManager.SetInvocationId("invocation-1");
            handler.TestMethod();

            LogBufferManager.SetInvocationId("invocation-2");
            // Create a custom handler that logs different messages
            var customHandler = new MultipleInvocationHandler(logger);
            customHandler.TestMethod();

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Information message", output); // From first invocation
            Assert.Contains("Second invocation info", output); // From second invocation
        }

        [Fact]
        public void MultipleProviders_AllProvidersReceiveLogs()
        {
            // Arrange
            var config = new PowertoolsLoggerConfiguration
            {
                MinimumLogLevel = LogLevel.Information,
                LogBuffering = new LogBufferingOptions { Enabled = true, BufferAtLogLevel = LogLevel.Debug },
                LogOutput = _consoleOut
            };

            var powertoolsConfig = new PowertoolsConfigurations(new PowertoolsEnvironment());
            
            // Create two separate providers
            var provider1 = new BufferingLoggerProvider(config, powertoolsConfig);
            var provider2 = new BufferingLoggerProvider(config, powertoolsConfig);
            
            var logger1 = provider1.CreateLogger("Provider1");
            var logger2 = provider2.CreateLogger("Provider2");
            
            LogBufferManager.SetInvocationId("multi-provider-test");

            // Act
            logger1.LogDebug("Debug from provider 1");
            logger2.LogDebug("Debug from provider 2");
            
            // Flush logs from all providers
            Logger.FlushBuffer();

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug from provider 1", output);
            Assert.Contains("Debug from provider 2", output);
        }

        [Fact]
        public async Task AsyncOperations_MaintainBufferContext()
        {
            // Arrange
            var logger = CreateLogger(LogLevel.Information, true, LogLevel.Debug);
            var handler = new AsyncHandler(logger);
            LogBufferManager.SetInvocationId("async-test");

            // Act
            await handler.TestMethodAsync();

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Async info message", output);
            Assert.Contains("Debug from task 1", output); 
            Assert.Contains("Debug from task 2", output);
        }

        private ILogger CreateLogger(LogLevel minimumLevel, bool enableBuffering, LogLevel bufferAtLevel)
        {
            return LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "test-service";
                    config.MinimumLogLevel = minimumLevel;
                    config.LogOutput = _consoleOut;
                    config.LogBuffering = new LogBufferingOptions
                    {
                        Enabled = enableBuffering,
                        BufferAtLogLevel = bufferAtLevel,
                        FlushOnErrorLog = false
                    };
                });
            }).CreatePowertoolsLogger();
        }

        private ILogger CreateLoggerWithFlushOnError(bool flushOnError)
        {
            return LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "test-service";
                    config.MinimumLogLevel = LogLevel.Information;
                    config.LogOutput = _consoleOut;
                    config.LogBuffering = new LogBufferingOptions
                    {
                        Enabled = true,
                        BufferAtLogLevel = LogLevel.Debug,
                        FlushOnErrorLog = flushOnError
                    };
                });
            }).CreatePowertoolsLogger();
        }

        public void Dispose()
        {
            // Clean up all state between tests
            Logger.ClearBuffer();
            LogBufferManager.ResetForTesting();
        }
    }

    // Additional test handlers with specific behavior
    public class CustomHandlerWithoutFlush
    {
        private readonly ILogger _logger;

        public CustomHandlerWithoutFlush(ILogger logger)
        {
            _logger = logger;
        }

        public void TestMethod()
        {
            _logger.LogDebug("Debug message");
            _logger.LogError("Error triggering flush");
            // No manual flush
        }
    }

    public class ClearBufferHandler
    {
        private readonly ILogger _logger;

        public ClearBufferHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void TestMethod()
        {
            _logger.LogDebug("Debug message before clear");
            Logger.ClearBuffer(); // Clear the buffer
            _logger.LogDebug("Debug message after clear");
            Logger.FlushBuffer(); // Flush only second message
        }
    }

    public class MultipleInvocationHandler
    {
        private readonly ILogger _logger;

        public MultipleInvocationHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void TestMethod()
        {
            _logger.LogInformation("Second invocation info");
            _logger.LogDebug("Second invocation debug");
            _logger.FlushBuffer();
        }
    }
    
    public class Handlers
    {
        private readonly ILogger _logger;

        public Handlers(ILogger logger)
        {
            _logger = logger;
        }

        public void TestMethod()
        {
            _logger.AppendKey("custom-key", "custom-value");
            _logger.LogInformation("Information message");
            _logger.LogDebug("Debug message");

            _logger.LogError("Error message");

            _logger.FlushBuffer();
        }
    }
    
    public class HandlerWithoutFlush
    {
        private readonly ILogger _logger;

        public HandlerWithoutFlush(ILogger logger)
        {
            _logger = logger;
        }

        public void TestMethod()
        {
            _logger.AppendKey("custom-key", "custom-value");
            _logger.LogInformation("Information message");
            _logger.LogDebug("Debug message");
            _logger.LogError("Error message");
            // No flush here
        }
    }

    public class AsyncHandler
    {
        private readonly ILogger _logger;

        public AsyncHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task TestMethodAsync()
        {
            _logger.LogInformation("Async info message");
            _logger.LogDebug("Async debug message");
            
            var task1 = Task.Run(() => {
                _logger.LogDebug("Debug from task 1");
            });
            
            var task2 = Task.Run(() => {
                _logger.LogDebug("Debug from task 2");
            });
            
            await Task.WhenAll(task1, task2);
            _logger.FlushBuffer();
        }
    }
}