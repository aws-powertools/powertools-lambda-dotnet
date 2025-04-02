using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AWS.Lambda.Powertools.Logging.Tests.Buffering
{
    [Collection("Sequential")]
    public class LambdaContextBufferingTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly TestLoggerOutput _consoleOut;

        public LambdaContextBufferingTests(ITestOutputHelper output)
        {
            _output = output;
            _consoleOut = new TestLoggerOutput();
            LogBufferManager.ResetForTesting();
        }

        [Fact]
        public void DisabledBuffering_LogsAllLevelsDirectly()
        {
            // Arrange
            var logger = CreateLogger(LogLevel.Debug, false, LogLevel.Debug);
            var handler = new LambdaHandler(logger);
            var context = CreateTestContext("test-request-2");

            // Act
            handler.TestMethod("Event", context);

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Information message", output);
            Assert.Contains("Debug message", output);
            Assert.Contains("Error message", output);
        }

        [Fact]
        public void FlushOnErrorEnabled_AutomaticallyFlushesBuffer()
        {
            // Arrange
            var logger = CreateLoggerWithFlushOnError(true);
            var handler = new ErrorOnlyHandler(logger);
            var context = CreateTestContext("test-request-3");

            // Act
            handler.TestMethod("Event", context);

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug message", output);
            Assert.Contains("Error triggering flush", output);
        }

        [Fact]
        public async Task AsyncOperations_MaintainBufferContext()
        {
            // Arrange
            var logger = CreateLogger(LogLevel.Information, true, LogLevel.Debug);
            var handler = new AsyncLambdaHandler(logger);
            var context = CreateTestContext("async-test");

            // Act
            await handler.TestMethodAsync("Event", context);

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Async info message", output);
            Assert.Contains("Debug from task 1", output);
            Assert.Contains("Debug from task 2", output);
        }

        private TestLambdaContext CreateTestContext(string requestId)
        {
            return new TestLambdaContext
            {
                FunctionName = "test-function",
                FunctionVersion = "1",
                AwsRequestId = requestId,
                InvokedFunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:test-function"
            };
        }

        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }

            return count;
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
                        BufferAtLogLevel = bufferAtLevel
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
            Logger.ClearBuffer();
            LogBufferManager.ResetForTesting();
        }
    }


    [Collection("Sequential")]
    public class StaticLoggerBufferingTests : IDisposable
    {
        private readonly TestLoggerOutput _consoleOut;
        private readonly ITestOutputHelper _output;

        public StaticLoggerBufferingTests(ITestOutputHelper output)
        {
            _output = output;
            _consoleOut = new TestLoggerOutput();

            // Configure static Logger with our test output
            Logger.Configure(options =>
                options.LogOutput = _consoleOut);
        }

        [Fact]
        public void StaticLogger_BasicBufferingBehavior()
        {
            // Arrange - explicitly configure Logger for this test
            // First reset any existing configuration
            Logger.Reset();

            // Configure the logger with the test output
            Logger.Configure(options =>
            {
                options.LogOutput = _consoleOut;
                options.MinimumLogLevel = LogLevel.Information;
                options.LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug,
                    FlushOnErrorLog = false // Disable auto-flush to test manual flush
                };
            });

            // Set invocation ID manually
            LogBufferManager.SetInvocationId("test-static-request-1");

            // Act - log messages
            Logger.AppendKey("custom-key", "custom-value");
            Logger.LogInformation("Information message");
            Logger.LogDebug("Debug message"); // Should be buffered

            // Check the internal state before flush
            var outputBeforeFlush = _consoleOut.ToString();
            _output.WriteLine($"Before flush: {outputBeforeFlush}");
            Assert.DoesNotContain("Debug message", outputBeforeFlush);

            // Flush the buffer
            Logger.FlushBuffer();

            // Assert after flush
            var outputAfterFlush = _consoleOut.ToString();
            _output.WriteLine($"After flush: {outputAfterFlush}");
            Assert.Contains("Debug message", outputAfterFlush);
        }

        [Fact]
        public void StaticLogger_WithLoggingDecoratedHandler()
        {
            // Arrange
            Logger.Configure(options =>
            {
                options.LogOutput = _consoleOut;
                options.LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug,
                    FlushOnErrorLog = true
                };
            });
            
            var handler = new StaticLambdaHandler();
            var context = new TestLambdaContext
            {
                AwsRequestId = "test-static-request-2",
                FunctionName = "test-function"
            };

            // Act
            handler.TestMethod("test-event", context);

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Information message", output);
            Assert.Contains("Debug message", output);
            Assert.Contains("Error message", output);
            Assert.Contains("custom-key", output);
            Assert.Contains("custom-value", output);
        }

        [Fact]
        public void StaticLogger_ClearBufferRemovesLogs()
        {
            // Arrange
            Logger.Configure(options =>
            {
                options.LogOutput = _consoleOut;
                options.MinimumLogLevel = LogLevel.Information;
                options.LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug
                };
            });

            // Set invocation ID
            LogBufferManager.SetInvocationId("test-static-request-3");

            // Act - log message and clear buffer
            Logger.LogDebug("Debug message before clear");
            Logger.ClearBuffer();
            Logger.LogDebug("Debug message after clear");
            Logger.FlushBuffer();

            // Assert
            var output = _consoleOut.ToString();
            Assert.DoesNotContain("Debug message before clear", output);
            Assert.Contains("Debug message after clear", output);
        }

        [Fact]
        public void StaticLogger_FlushOnErrorLogEnabled()
        {
            // Arrange
            Logger.Configure(options =>
            {
                options.LogOutput = _consoleOut;
                options.MinimumLogLevel = LogLevel.Information;
                options.LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug,
                    FlushOnErrorLog = true
                };
            });

            // Set invocation ID
            LogBufferManager.SetInvocationId("test-static-request-4");

            // Act - log debug then error
            Logger.LogDebug("Debug message");
            Logger.LogError("Error message");

            // Assert - error should trigger flush
            var output = _consoleOut.ToString();
            Assert.Contains("Debug message", output);
            Assert.Contains("Error message", output);
        }

        [Fact]
        public void StaticLogger_MultipleInvocationsIsolated()
        {
            // Arrange
            Logger.Configure(options =>
            {
                options.LogOutput = _consoleOut;
                options.MinimumLogLevel = LogLevel.Information;
                options.LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug
                };
            });

            // Act - first invocation
            LogBufferManager.SetInvocationId("test-static-request-5A");
            Logger.LogDebug("Debug from invocation A");

            // Switch to second invocation
            LogBufferManager.SetInvocationId("test-static-request-5B");
            Logger.LogDebug("Debug from invocation B");
            Logger.FlushBuffer(); // Only flush B

            // Assert - after first flush
            var outputAfterFirstFlush = _consoleOut.ToString();
            Assert.Contains("Debug from invocation B", outputAfterFirstFlush);
            Assert.DoesNotContain("Debug from invocation A", outputAfterFirstFlush);

            // Switch back to first invocation and flush
            LogBufferManager.SetInvocationId("test-static-request-5A");
            Logger.FlushBuffer();

            // Assert - after second flush
            var outputAfterSecondFlush = _consoleOut.ToString();
            Assert.Contains("Debug from invocation A", outputAfterSecondFlush);
        }

        [Fact]
        public void StaticLogger_FlushOnErrorDisabled()
        {
            // Arrange
            Logger.Reset();
            Logger.Configure(options =>
            {
                options.LogOutput = _consoleOut;
                options.MinimumLogLevel = LogLevel.Information;
                options.LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug,
                    FlushOnErrorLog = false
                };
            });

            LogBufferManager.SetInvocationId("test-static-request-6");

            // Act - log debug then error
            Logger.LogDebug("Debug message with auto-flush disabled");
            Logger.LogError("Error message that should not trigger flush");

            // Assert - debug message should remain buffered
            var output = _consoleOut.ToString();
            Assert.DoesNotContain("Debug message with auto-flush disabled", output);
            Assert.Contains("Error message that should not trigger flush", output);

            // Now manually flush and verify debug message appears
            Logger.FlushBuffer();
            output = _consoleOut.ToString();
            Assert.Contains("Debug message with auto-flush disabled", output);
        }

        [Fact]
        public void StaticLogger_AsyncOperationsMaintainContext()
        {
            // Arrange
            // Logger.Reset();
            Logger.Configure(options =>
            {
                options.LogOutput = _consoleOut;
                options.MinimumLogLevel = LogLevel.Information;
                options.LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug,
                    FlushOnErrorLog = false
                };
            });

            LogBufferManager.SetInvocationId("test-static-request-8");

            // Act - simulate async operations
            Task.Run(() => { Logger.LogDebug("Debug from task 1"); }).Wait();

            Task.Run(() => { Logger.LogDebug("Debug from task 2"); }).Wait();

            Logger.LogInformation("Main thread info message");

            // Flush buffers
            Logger.FlushBuffer();

            // Assert
            var output = _consoleOut.ToString();
            Assert.Contains("Debug from task 1", output);
            Assert.Contains("Debug from task 2", output);
            Assert.Contains("Main thread info message", output);
        }

        public void Dispose()
        {
            // Clean up all state between tests
            Logger.ClearBuffer();
            LogBufferManager.ResetForTesting();
            LoggerFactoryHolder.Reset();
            _consoleOut.Clear();
        }
    }

    public class StaticLambdaHandler
    {
        [Logging(LogEvent = true)]
        public void TestMethod(string message, ILambdaContext lambdaContext)
        {
            Logger.AppendKey("custom-key", "custom-value");
            Logger.LogInformation("Information message");
            Logger.LogDebug("Debug message");
            Logger.LogError("Error message");
            Logger.FlushBuffer();
        }
    }

    // Lambda handlers for testing
    public class LambdaHandler
    {
        private readonly ILogger _logger;

        public LambdaHandler(ILogger logger)
        {
            _logger = logger;
        }

        [Logging(LogEvent = true)]
        public void TestMethod(string message, ILambdaContext lambdaContext)
        {
            _logger.AppendKey("custom-key", "custom-value");
            _logger.LogInformation("Information message");
            _logger.LogDebug("Debug message");
            _logger.LogError("Error message");
            _logger.FlushBuffer();
        }
    }

    public class ErrorOnlyHandler
    {
        private readonly ILogger _logger;

        public ErrorOnlyHandler(ILogger logger)
        {
            _logger = logger;
        }

        [Logging(LogEvent = true)]
        public void TestMethod(string message, ILambdaContext lambdaContext)
        {
            _logger.LogDebug("Debug message");
            _logger.LogError("Error triggering flush");
        }
    }

    public class AsyncLambdaHandler
    {
        private readonly ILogger _logger;

        public AsyncLambdaHandler(ILogger logger)
        {
            _logger = logger;
        }

        [Logging(LogEvent = true)]
        public async Task TestMethodAsync(string message, ILambdaContext lambdaContext)
        {
            _logger.LogInformation("Async info message");
            _logger.LogDebug("Async debug message");

            var task1 = Task.Run(() => { _logger.LogDebug("Debug from task 1"); });

            var task2 = Task.Run(() => { _logger.LogDebug("Debug from task 2"); });

            await Task.WhenAll(task1, task2);
            _logger.FlushBuffer();
        }
    }
}