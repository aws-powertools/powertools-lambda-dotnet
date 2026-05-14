// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

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
        ...
    }
}
// --8<-- [end:configure_static_logger]

// --8<-- [start:configure_ilogger]
public class Function
{
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
        Logger.LogInformation("Collecting payment");
        ...
    }
}
// --8<-- [end:configure_ilogger]
