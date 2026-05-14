// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

// --8<-- [start:using_decorator]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging(Service = "payment", LogLevel = LogLevel.Debug)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Logger.LogInformation("Collecting payment");
        ...
    }
}
// --8<-- [end:using_decorator]

// --8<-- [start:logger_factory]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    private readonly ILogger _logger;

    public Function(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.Create(builder =>
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
// --8<-- [end:logger_factory]

// --8<-- [start:with_builder]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    private readonly ILogger _logger;

    public Function(ILogger logger)
    {
        _logger = logger ?? new PowertoolsLoggerBuilder()
            .WithService("TestService")
            .WithOutputCase(LoggerOutputCase.PascalCase)
            .Build();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        _logger.LogInformation("Collecting payment");
        ...
    }
}
// --8<-- [end:with_builder]
