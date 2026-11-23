using System;
using System.Linq;
using System.Text.Json;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Buffering
{
    [Collection("Sequential")]
    public class LogBufferingFallbackTests : IDisposable
    {
        private readonly TestLoggerOutput _consoleOut = new();

        public LogBufferingFallbackTests()
        {
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", "invocation-1");
        }

        [Fact]
        public void Log_WhenFormatterFailsOnce_WritesOriginalEntry()
        {
            var logger = CreateLogger(new ThrowingDebugFormatter(failures: 1));

            logger.LogDebug("Debug message");

            var output = _consoleOut.ToString();
            Assert.Contains("Failed to buffer log entry", output);
            Assert.Contains("\"message\":\"Debug message\"", output);
        }

        [Fact]
        public void Log_WhenFormatterAlwaysFails_WritesEntryWithoutMessage()
        {
            var logger = CreateLogger(new ThrowingDebugFormatter(failures: int.MaxValue));

            logger.LogDebug("secret value");

            var output = _consoleOut.ToString();
            Assert.DoesNotContain("secret value", output);

            var entry = ParseLines(output).Single(e =>
                e.TryGetProperty("message", out var message) &&
                message.GetString() == PowertoolsBufferingLogger.UnformattedEntryMessage);
            Assert.Equal("Debug", entry.GetProperty("level").GetString());
            Assert.Equal("test-service", entry.GetProperty("service").GetString());
            Assert.True(entry.TryGetProperty("timestamp", out _));
        }

        [Fact]
        public void Log_WhenFormatterAlwaysFails_UsesConfiguredOutputCase()
        {
            var logger = CreateLogger(new ThrowingDebugFormatter(failures: int.MaxValue), LoggerOutputCase.PascalCase);

            logger.LogDebug("secret value");

            var entry = ParseLines(_consoleOut.ToString()).Single(e =>
                e.TryGetProperty("Message", out var message) &&
                message.GetString() == PowertoolsBufferingLogger.UnformattedEntryMessage);
            Assert.Equal("Debug", entry.GetProperty("Level").GetString());
            Assert.Equal("test-service", entry.GetProperty("Service").GetString());
            Assert.True(entry.TryGetProperty("Timestamp", out _));
        }

        [Fact]
        public void Log_WhenFormatterWorks_BuffersUntilFlush()
        {
            var logger = CreateLogger(new ThrowingDebugFormatter(failures: 0));

            logger.LogDebug("Debug message");
            Assert.DoesNotContain("Debug message", _consoleOut.ToString());

            logger.FlushBuffer();
            var output = _consoleOut.ToString();
            Assert.Contains("\"message\":\"Debug message\"", output);
            Assert.DoesNotContain("Failed to buffer log entry", output);
        }

        private ILogger CreateLogger(ILogFormatter formatter, LoggerOutputCase outputCase = LoggerOutputCase.SnakeCase)
        {
            var config = new PowertoolsLoggerConfiguration
            {
                Service = "test-service",
                MinimumLogLevel = LogLevel.Information,
                LoggerOutputCase = outputCase,
                LogFormatter = formatter,
                LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug
                },
                LogOutput = _consoleOut
            };
            return LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();
        }

        private static JsonElement[] ParseLines(string output) =>
            output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => line.StartsWith('{'))
                .Select(line => JsonDocument.Parse(line).RootElement.Clone())
                .ToArray();

        public void Dispose()
        {
            Logger.ClearBuffer();
            LogBufferManager.ResetForTesting();
            Environment.SetEnvironmentVariable("_X_AMZN_TRACE_ID", null);
        }

        /// <summary>
        /// Throws for the first <c>failures</c> Debug entries, formats everything else.
        /// </summary>
        private sealed class ThrowingDebugFormatter : ILogFormatter
        {
            private int _remainingFailures;

            public ThrowingDebugFormatter(int failures) => _remainingFailures = failures;

            public object FormatLogEntry(LogEntry logEntry)
            {
                if (logEntry.Level == LogLevel.Debug && _remainingFailures > 0)
                {
                    _remainingFailures--;
                    throw new InvalidOperationException("Simulated formatter failure");
                }

                return new
                {
                    level = logEntry.Level.ToString(),
                    message = logEntry.Message,
                    service = logEntry.Service
                };
            }
        }
    }
}
