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


namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.Configuration.ConfigureStaticLogger
{
// --8<-- [start:configure_static_logger]
public class Function
{
    public Function()
    {
        Logger.Configure(options =>
        {
            options.MinimumLogLevel = LogLevel.Information;
            options.LoggerOutputCase = LoggerOutputCase.CamelCase;
        });
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Logger.LogInformation("Collecting payment");
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:configure_static_logger]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.Configuration.ConfigureIlogger
{
// --8<-- [start:configure_ilogger]
public class Function
{
    private readonly ILogger _logger;

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
// --8<-- [end:configure_ilogger]
}
