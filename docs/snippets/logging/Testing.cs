extern alias LoggingTypes;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using ILambdaContext = Amazon.Lambda.Core.ILambdaContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using IConsoleWrapper = LoggingTypes::AWS.Lambda.Powertools.Common.IConsoleWrapper;

// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).


namespace Docs.LoggingSnippets.Testing.TestingSetup
{
public class Example
{
    public void Run()
    {
// --8<-- [start:testing_setup]
Logger.Configure(options =>
{
    // Using TestLoggerOutput
    options.LogOutput = new TestLoggerOutput();
    // Custom console output for testing
    options.LogOutput = new TestConsoleWrapper();
});
} } // --8<-- [start:hidden]

// Example implementation for testing:
public class TestConsoleWrapper : IConsoleWrapper
{
    public List<string> CapturedOutput { get; } = new();

    public void WriteLine(string message)
    {
        CapturedOutput.Add(message);
    }

    public void Debug(string message) => CapturedOutput.Add(message);
    public void Error(string message) => CapturedOutput.Add(message);
}
// --8<-- [end:testing_setup]
}

namespace Docs.LoggingSnippets.Testing.TestExample
{
public class Example
{
    private readonly TestHandlers _testHandlers = new();
// --8<-- [start:test_example]
// Test example
[Fact]
public void When_Setting_Service_Should_Update_Key()
{
    // Arrange
    var consoleOut = new TestLoggerOutput();
    Logger.Configure(options =>
    {
        options.LogOutput = consoleOut;
    });

    // Act
    _testHandlers.HandlerService();

    // Assert

    var st = consoleOut.ToString();

    Assert.Contains("\"level\":\"Information\"", st);
    Assert.Contains("\"service\":\"test\"", st);
    Assert.Contains("\"name\":\"AWS.Lambda.Powertools.Logging.Logger\"", st);
    Assert.Contains("\"message\":\"test\"", st);
}
// --8<-- [end:test_example]
}
}

namespace Docs.LoggingSnippets.Testing.IloggerTesting
{
// --8<-- [start:ilogger_testing]
public class Function
{
    private readonly ILogger _logger;

    public Function()
    {
        _logger = LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = "TestService";
                config.LoggerOutputCase = LoggerOutputCase.PascalCase;
            });
        }).CreatePowertoolsLogger();
    }

    // constructor used for tests - pass the mock ILogger
    public Function(ILogger logger)
    {
        _logger = logger ?? LoggerFactory.Create(builder =>
        {
            builder.AddPowertoolsLogger(config =>
            {
                config.Service = "TestService";
                config.LoggerOutputCase = LoggerOutputCase.PascalCase;
            });
        }).CreatePowertoolsLogger();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        _logger.LogInformation("Collecting payment");
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:ilogger_testing]
}

internal class TestLoggerOutput : IConsoleWrapper
{
    private readonly List<string> _lines = new();
    public void WriteLine(string message) => _lines.Add(message);
    public void Debug(string message) => _lines.Add(message);
    public void Error(string message) => _lines.Add(message);
    public override string ToString() => string.Join(Environment.NewLine, _lines);
}

internal class TestHandlers
{
    public void HandlerService() => Logger.LogInformation("test");
}
