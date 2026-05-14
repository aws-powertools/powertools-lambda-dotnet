// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

// --8<-- [start:testing_setup]
Logger.Configure(options =>
{
    // Using TestLoggerOutput
    options.LogOutput = new TestLoggerOutput();
    // Custom console output for testing
    options.LogOutput = new TestConsoleWrapper();
});

// Example implementation for testing:
public class TestConsoleWrapper : IConsoleWrapper
{
    public List<string> CapturedOutput { get; } = new();

    public void WriteLine(string message)
    {
        CapturedOutput.Add(message);
    }
}
// --8<-- [end:testing_setup]

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

// --8<-- [start:ilogger_testing]
public class Function
{
    private readonly ILogger _logger;

    public Function()
    {
        _logger = oggerFactory.Create(builder =>
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
        _logger = logger ?? loggerFactory.Create(builder =>
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
        ...
    }
}
// --8<-- [end:ilogger_testing]
