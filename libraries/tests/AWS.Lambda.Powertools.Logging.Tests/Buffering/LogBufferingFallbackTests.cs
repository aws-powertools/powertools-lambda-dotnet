using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
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

        [Theory]
        [InlineData(LoggerOutputCase.SnakeCase, "level", "message", "timestamp", "service")]
        [InlineData(LoggerOutputCase.CamelCase, "level", "message", "timestamp", "service")]
        [InlineData(LoggerOutputCase.PascalCase, "Level", "Message", "Timestamp", "Service")]
        public void Log_WhenFormatterAlwaysFails_WritesEntryWithoutMessage(
            LoggerOutputCase outputCase, string levelKey, string messageKey, string timestampKey, string serviceKey)
        {
            var logger = CreateLogger(new ThrowingDebugFormatter(failures: int.MaxValue), outputCase);

            logger.LogDebug("secret value");

            var output = _consoleOut.ToString();
            Assert.DoesNotContain("secret value", output);

            var entry = FindUnformattedEntry(output, messageKey);
            Assert.Equal("Debug", entry.GetProperty(levelKey).GetString());
            Assert.Equal("test-service", entry.GetProperty(serviceKey).GetString());
            Assert.True(entry.TryGetProperty(timestampKey, out _));
        }

        [Fact]
        public void Log_WhenTimestampFormatIsInvalid_WritesEntryWithIsoTimestamp()
        {
            // An unterminated quote makes DateTime.ToString throw, so the regular entry can't be built
            var logger = CreateLogger(formatter: null, timestampFormat: "'");

            logger.LogDebug("secret value");

            var output = _consoleOut.ToString();
            Assert.DoesNotContain("secret value", output);

            var entry = FindUnformattedEntry(output, "message");
            var timestamp = entry.GetProperty("timestamp").GetString();
            Assert.True(DateTime.TryParse(timestamp, out _), $"Not an ISO timestamp: {timestamp}");
        }

        [Fact]
        public void Log_WhenOutputThrows_DoesNotThrow()
        {
            var logger = CreateLogger(new ThrowingDebugFormatter(failures: int.MaxValue), output: new ThrowingOutput());

            var exception = Record.Exception(() => logger.LogDebug("Debug message"));

            Assert.Null(exception);
        }

        [Fact]
        public void Log_WhenEntryIsWrittenBeforeFailure_DoesNotWriteItAgain()
        {
            // Entries larger than MaxBytes are written directly; make the warning that follows throw
            var logger = CreateLogger(new ThrowingDebugFormatter(failures: 0), maxBytes: 10);
            var originalOut = Console.Out;
            Console.SetOut(new ThrowingTextWriter());
            try
            {
                logger.LogDebug("Debug message");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = _consoleOut.ToString();
            Assert.Contains("Failed to buffer log entry", output);
            Assert.Single(ParseLines(output), e =>
                e.TryGetProperty("message", out var message) && message.GetString() == "Debug message");
        }

        [Fact]
        public void Log_WhenInnerLoggerIsNotPowertoolsLogger_DoesNothing()
        {
            var inner = Substitute.For<ILogger>();
            var config = new PowertoolsLoggerConfiguration
            {
                LogBuffering = new LogBufferingOptions { Enabled = true, BufferAtLogLevel = LogLevel.Debug }
            };
            var logger = new PowertoolsBufferingLogger(inner, () => config, Substitute.For<IPowertoolsConfigurations>());

            logger.LogDebug("Debug message");

            Assert.Empty(inner.ReceivedCalls());
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

        private ILogger CreateLogger(
            ILogFormatter formatter,
            LoggerOutputCase outputCase = LoggerOutputCase.SnakeCase,
            string timestampFormat = null,
            int maxBytes = 20480,
            IConsoleWrapper output = null)
        {
            var config = new PowertoolsLoggerConfiguration
            {
                Service = "test-service",
                MinimumLogLevel = LogLevel.Information,
                LoggerOutputCase = outputCase,
                LogFormatter = formatter,
                TimestampFormat = timestampFormat,
                LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug,
                    MaxBytes = maxBytes
                },
                LogOutput = output ?? _consoleOut
            };
            return LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();
        }

        private static JsonElement FindUnformattedEntry(string output, string messageKey) =>
            ParseLines(output).Single(e =>
                e.TryGetProperty(messageKey, out var message) &&
                message.GetString() == PowertoolsBufferingLogger.UnformattedEntryMessage);

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

        private sealed class ThrowingOutput : IConsoleWrapper
        {
            public void WriteLine(string message) => throw new IOException("Simulated output failure");
            public void Debug(string message) => throw new IOException("Simulated output failure");
            public void Error(string message) => throw new IOException("Simulated output failure");
        }

        private sealed class ThrowingTextWriter : StringWriter
        {
            public override void WriteLine(string value) => throw new IOException("Simulated console failure");
        }
    }
}
