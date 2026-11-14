using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using ILambdaContext = Amazon.Lambda.Core.ILambdaContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).


namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.GettingStartedExamples.UsingDecorator
{
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
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:using_decorator]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.GettingStartedExamples.LoggerFactoryExample
{
// --8<-- [start:logger_factory]
/**
 * Handler for requests to Lambda function.
 */
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

    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        _logger.LogInformation("Collecting payment");
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:logger_factory]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.GettingStartedExamples.WithBuilder
{
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
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:with_builder]
}
