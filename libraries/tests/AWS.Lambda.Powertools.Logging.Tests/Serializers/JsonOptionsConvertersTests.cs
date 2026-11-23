using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.Logging.Tests.Serializers;

[Collection("Sequential")]
public class JsonOptionsConvertersTests : IDisposable
{
    private readonly ITestOutputHelper _output;

    public JsonOptionsConvertersTests(ITestOutputHelper output)
    {
        _output = output;
        ResetState();
    }

    [Fact]
    public void UserConverter_IsUsedForAppendedKey()
    {
        var output = new TestLoggerOutput();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new BigIntegerStringConverter());
        var logger = BuildLogger(output, options);

        logger.AppendKey("big_int", BigInteger.Parse("12345678901234567890"));
        logger.LogInformation("Big number");

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        Assert.Contains("\"big_int\":\"12345678901234567890\"", logOutput);
    }

    [Fact]
    public void UserEnumConverter_DoesNotChangeLevelField()
    {
        var output = new TestLoggerOutput();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
        var logger = BuildLogger(output, options);

        logger.AppendKey("min_level", LogLevel.Warning);
        logger.LogInformation("Enum converter");

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        Assert.Contains("\"level\":\"Information\"", logOutput);
        // Powertools' LogLevel converter runs before the user's enum converter
        Assert.Contains("\"min_level\":\"Warning\"", logOutput);
        Assert.DoesNotContain("\"WARNING\"", logOutput);
    }

    [Fact]
    public void UserExceptionConverter_DoesNotReplaceBuiltInFormat()
    {
        var output = new TestLoggerOutput();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ReplacingExceptionConverter());
        var logger = BuildLogger(output, options);

        logger.LogError(new InvalidOperationException("Something failed"), "Error happened");

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        Assert.Contains("\"type\":\"System.InvalidOperationException\"", logOutput);
        Assert.Contains("\"message\":\"Something failed\"", logOutput);
        Assert.DoesNotContain("replaced", logOutput);
    }

    private static ILogger BuildLogger(TestLoggerOutput output, JsonSerializerOptions options)
    {
        return new PowertoolsLoggerBuilder()
            .WithService("converters-test")
            .WithLogOutput(output)
            .WithJsonOptions(options)
            .Build();
    }

    private static void ResetState()
    {
        Logger.Reset();
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
        LoggerFactoryHolder.Reset();
    }

    public void Dispose()
    {
        ResetState();
    }

    private class BigIntegerStringConverter : JsonConverter<BigInteger>
    {
        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => BigInteger.Parse(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }

    private class ReplacingExceptionConverter : JsonConverter<Exception>
    {
        public override bool CanConvert(Type typeToConvert) => typeof(Exception).IsAssignableFrom(typeToConvert);

        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
            => writer.WriteStringValue("replaced");
    }
}
