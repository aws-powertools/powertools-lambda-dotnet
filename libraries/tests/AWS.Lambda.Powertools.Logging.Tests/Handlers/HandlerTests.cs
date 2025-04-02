using System.Text.Json.Serialization;
#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using AWS.Lambda.Powertools.Logging.Tests.Formatter;
using AWS.Lambda.Powertools.Logging.Tests.Utilities;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AWS.Lambda.Powertools.Logging.Tests.Handlers;

public class Handlers
{
    private readonly ILogger _logger;

    public Handlers(ILogger logger)
    {
        _logger = logger;
    }

    [Logging(LogEvent = true)]
    public void TestMethod(string message, ILambdaContext lambdaContext)
    {
        _logger.AppendKey("custom-key", "custom-value");
        _logger.LogInformation("Information message");
        _logger.LogDebug("debug message");

        var example = new ExampleClass
        {
            Name = "test",
            Price = 1.999,
            ThisIsBig = "big",
            ThisIsHidden = "hidden"
        };

        _logger.LogInformation("Example object: {example}", example);
        _logger.LogInformation("Another JSON log {d:0.000}", 1.2333);

        _logger.LogDebug(example);
        _logger.LogInformation(example);
    }

    [Logging(LogEvent = true, CorrelationIdPath = "price")]
    public void TestMethodCorrelation(ExampleClass message, ILambdaContext lambdaContext)
    {
    }
}

public class StaticHandler
{
    [Logging(LogEvent = true, LoggerOutputCase = LoggerOutputCase.PascalCase, Service = "my-service122")]
    public void TestMethod(string message, ILambdaContext lambdaContext)
    {
        Logger.LogInformation("Static method");
    }
}

public class HandlerTests
{
    private readonly ITestOutputHelper _output;

    public HandlerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestMethod()
    {
        var output = new TestLoggerOutput();

        var logger = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = "my-service122";
                config.SamplingRate = 0.002;
                config.MinimumLogLevel = LogLevel.Debug;
                config.LoggerOutputCase = LoggerOutputCase.PascalCase;
                config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                config.JsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                    // PropertyNamingPolicy = null,
                    // DictionaryKeyPolicy = PascalCaseNamingPolicy.Instance,
                };
                config.LogOutput = output;
            });
        }).CreateLogger<Handlers>();


        var handler = new Handlers(logger);

        handler.TestMethod("Event", new TestLambdaContext
        {
            FunctionName = "test-function",
            FunctionVersion = "1",
            AwsRequestId = "123",
            InvokedFunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:test-function"
        });

        handler.TestMethodCorrelation(new ExampleClass
        {
            Name = "test-function",
            Price = 1.999,
            ThisIsBig = "big",
        }, null);

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        // Check if the output contains newlines and spacing (indentation)
        Assert.Contains("\n", logOutput);
        Assert.Contains("  ", logOutput);
        
        // Verify write indented JSON
        Assert.Contains("  \"Level\": \"Information\",\n  \"Service\": \"my-service122\",", logOutput);
    }

    [Fact]
    public void TestMethodCustom()
    {
        var output = new TestLoggerOutput();
        var logger = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = "my-service122";
                config.SamplingRate = 0.002;
                config.MinimumLogLevel = LogLevel.Debug;
                config.LoggerOutputCase = LoggerOutputCase.CamelCase;
                config.JsonOptions = new JsonSerializerOptions
                {
                    // PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    // DictionaryKeyPolicy = JsonNamingPolicy.KebabCaseLower
                };

                config.LogFormatter = new CustomLogFormatter();
                config.LogOutput = output;
            });
        }).CreatePowertoolsLogger();

        var handler = new Handlers(logger);

        handler.TestMethod("Event", new TestLambdaContext
        {
            FunctionName = "test-function",
            FunctionVersion = "1",
            AwsRequestId = "123",
            InvokedFunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:test-function"
        });

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        // Verify CamelCase formatting (custom formatter)
        Assert.Contains("\"service\":\"my-service122\"", logOutput);
        Assert.Contains("\"level\":\"Information\"", logOutput);
        Assert.Contains("\"message\":\"Information message\"", logOutput);
        Assert.Contains("\"correlationIds\":{\"awsRequestId\":\"123\"}", logOutput);
        
    }

    [Fact]
    public void TestBuffer()
    {
        var output = new TestLoggerOutput();
        var logger = LoggerFactory.Create(builder =>
        {
            // builder.AddFilter("AWS.Lambda.Powertools.Logging.Tests.Handlers.Handlers", LogLevel.Debug);
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = "my-service122";
                config.SamplingRate = 0.002;
                config.MinimumLogLevel = LogLevel.Information;
                config.JsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    // PropertyNamingPolicy = JsonNamingPolicy.KebabCaseUpper,
                    DictionaryKeyPolicy = JsonNamingPolicy.KebabCaseUpper
                };
                config.LogOutput = output;
                config.LogBuffering = new LogBufferingOptions
                {
                    Enabled = true,
                    BufferAtLogLevel = LogLevel.Debug,
                };
            });
        }).CreatePowertoolsLogger();

        var handler = new Handlers(logger);

        handler.TestMethod("Event", new TestLambdaContext
        {
            FunctionName = "test-function",
            FunctionVersion = "1",
            AwsRequestId = "123",
            InvokedFunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:test-function"
        });

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        // Verify buffering behavior - only Information logs or higher should be in output
        Assert.Contains("Information message", logOutput);
        Assert.DoesNotContain("debug message", logOutput); // Debug should be buffered

        // Verify JSON options with indentation
        Assert.Contains("\n", logOutput);
        Assert.Contains("  ", logOutput); // Check for indentation

        // Check that kebab-case dictionary keys are working
        Assert.Contains("\"CUSTOM-KEY\"", logOutput);
    }

    [Fact]
    public void TestMethodStatic()
    {
        var output = new TestLoggerOutput();
        var handler = new StaticHandler();

        Logger.Configure(options =>
        {
            options.LogOutput = output;
            options.LoggerOutputCase = LoggerOutputCase.CamelCase;
        });

        handler.TestMethod("Event", new TestLambdaContext
        {
            FunctionName = "test-function",
            FunctionVersion = "1",
            AwsRequestId = "123",
            InvokedFunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:test-function"
        });

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        // Verify static logger configuration
        // Verify override of LoggerOutputCase from attribute
        Assert.Contains("\"Service\":\"my-service122\"", logOutput);
        Assert.Contains("\"Level\":\"Information\"", logOutput);
        Assert.Contains("\"Message\":\"Static method\"", logOutput);
    }

    [Fact]
    public void TestJsonOptionsPropertyNaming()
    {
        var output = new TestLoggerOutput();
        var logger = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = "json-options-service";
                config.MinimumLogLevel = LogLevel.Debug;
                config.JsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = false
                };
                config.LogOutput = output;
            });
        }).CreatePowertoolsLogger();

        var handler = new Handlers(logger);
        var example = new ExampleClass
        {
            Name = "TestValue",
            Price = 29.99,
            ThisIsBig = "LargeValue"
        };

        logger.LogInformation("Testing JSON options with example: {@example}", example);

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        // Verify snake_case naming policy is applied
        Assert.Contains("\"this_is_big\":\"LargeValue\"", logOutput);
        Assert.Contains("\"name\":\"TestValue\"", logOutput);
    }

    [Fact]
    public void TestJsonOptionsDictionaryKeyPolicy()
    {
        var output = new TestLoggerOutput();
        var logger = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = "json-dictionary-service";
                config.JsonOptions = new JsonSerializerOptions
                {
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                config.LogOutput = output;
            });
        }).CreatePowertoolsLogger();

        var dictionary = new Dictionary<string, object>
        {
            { "UserID", 12345 },
            { "OrderDetails", new { ItemCount = 3, Total = 150.75 } },
            { "ShippingAddress", "123 Main St" }
        };

        logger.LogInformation("Dictionary with custom key policy: {@dictionary}", dictionary);

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        // Fix assertion to match actual camelCase behavior with acronyms
        Assert.Contains("\"userID\":12345", logOutput);  // ID remains uppercase
        Assert.Contains("\"orderDetails\":", logOutput);
        Assert.Contains("\"shippingAddress\":", logOutput);
    }

    [Fact]
    public void TestJsonOptionsWriteIndented()
    {
        var output = new TestLoggerOutput();
        var logger = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = "json-indented-service";
                config.JsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                config.LogOutput = output;
            });
        }).CreatePowertoolsLogger();

        var example = new ExampleClass
        {
            Name = "IndentedTest",
            Price = 59.99,
            ThisIsBig = "IndentedValue"
        };

        logger.LogInformation("Testing indented JSON: {@example}", example);

        var logOutput = output.ToString();
        _output.WriteLine(logOutput);

        // Check if the output contains newlines and spacing (indentation)
        Assert.Contains("\n", logOutput);
        Assert.Contains("  ", logOutput);
    }
}

#endif

public class ExampleClass
{
    public string Name { get; set; }

    public double Price { get; set; }

    public string ThisIsBig { get; set; }

    [JsonIgnore] public string ThisIsHidden { get; set; }
}