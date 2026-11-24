using System;
using System.Text.Json;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Formatter;

[Collection("Sequential")]
public class LocalTimeZoneTimestampTests : IDisposable
{
    private const string NewYork = "America/New_York";

    private readonly string _previousTz;
    private readonly string _previousUseLocalTimeZone;

    public LocalTimeZoneTimestampTests()
    {
        _previousTz = Environment.GetEnvironmentVariable("TZ");
        _previousUseLocalTimeZone = Environment.GetEnvironmentVariable(LoggingConstants.UseLocalTimeZoneEnv);
        Environment.SetEnvironmentVariable(LoggingConstants.UseLocalTimeZoneEnv, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("TZ", _previousTz);
        Environment.SetEnvironmentVariable(LoggingConstants.UseLocalTimeZoneEnv, _previousUseLocalTimeZone);
        TimeZoneInfo.ClearCachedData();
        Logger.Reset();
    }

    private static void SetTimeZone(string tz)
    {
        Environment.SetEnvironmentVariable("TZ", tz);
        TimeZoneInfo.ClearCachedData();
    }

    private static string GetTimestamp(TestLoggerOutput output)
    {
        using var doc = JsonDocument.Parse(output.ToString());
        return doc.RootElement.GetProperty("timestamp").GetString();
    }

    private static TimeSpan NewYorkOffsetNow() =>
        TimeZoneInfo.FindSystemTimeZoneById(NewYork).GetUtcOffset(DateTime.UtcNow);

    [Fact]
    public void Disabled_WithNonUtcTimeZone_WritesUtc()
    {
        SetTimeZone(NewYork);
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("tz-test")
            .Build();

        logger.LogInformation("hello");

        Assert.EndsWith("Z", GetTimestamp(output));
    }

    [Fact]
    public void Enabled_WithNonUtcTimeZone_WritesLocalTimeWithOffset()
    {
        SetTimeZone(NewYork);
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("tz-test")
            .WithLocalTimeZone()
            .Build();

        logger.LogInformation("hello");

        var timestamp = GetTimestamp(output);
        Assert.DoesNotContain("Z", timestamp);

        var parsed = DateTimeOffset.Parse(timestamp);
        Assert.Equal(NewYorkOffsetNow(), parsed.Offset);
        Assert.InRange(DateTimeOffset.UtcNow - parsed, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData(":UTC")]
    [InlineData("UTC")]
    [InlineData("Etc/UTC")]
    public void Enabled_WithUtcTimeZone_WritesUtc(string tz)
    {
        SetTimeZone(tz);
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("tz-test")
            .WithLocalTimeZone()
            .Build();

        logger.LogInformation("hello");

        Assert.EndsWith("Z", GetTimestamp(output));
    }

    [Fact]
    public void EnvironmentVariable_EnablesLocalTimeZone()
    {
        SetTimeZone(NewYork);
        Environment.SetEnvironmentVariable(LoggingConstants.UseLocalTimeZoneEnv, "true");
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("tz-test")
            .Build();

        logger.LogInformation("hello");

        Assert.Equal(NewYorkOffsetNow(), DateTimeOffset.Parse(GetTimestamp(output)).Offset);
    }

    [Fact]
    public void Enabled_WithCustomTimestampFormat_AppliesFormatInLocalTime()
    {
        SetTimeZone(NewYork);
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("tz-test")
            .WithTimestampFormat("yyyy-MM-dd HH:mm zzz")
            .WithLocalTimeZone()
            .Build();

        logger.LogInformation("hello");

        var offset = NewYorkOffsetNow();
        var expectedSuffix = $"{(offset < TimeSpan.Zero ? "-" : "+")}{offset:hh\\:mm}";
        var timestamp = GetTimestamp(output);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2} [+-]\d{2}:\d{2}$", timestamp);
        Assert.EndsWith(expectedSuffix, timestamp);
    }

    [Theory]
    [InlineData(false, DateTimeKind.Utc)]
    [InlineData(true, DateTimeKind.Local)]
    public void CustomFormatter_ReceivesTimestampInConfiguredTimeZone(bool useLocalTimeZone, DateTimeKind expectedKind)
    {
        SetTimeZone(NewYork);
        var output = new TestLoggerOutput();
        var logger = new PowertoolsLoggerBuilder()
            .WithLogOutput(output)
            .WithService("tz-test")
            .WithFormatter(new TimestampKindFormatter())
            .WithLocalTimeZone(useLocalTimeZone)
            .Build();

        logger.LogInformation("hello");

        Assert.Equal(expectedKind.ToString(), GetTimestamp(output));
    }

    private class TimestampKindFormatter : ILogFormatter
    {
        public object FormatLogEntry(LogEntry logEntry) => new { timestamp = logEntry.Timestamp.Kind.ToString() };
    }
}
