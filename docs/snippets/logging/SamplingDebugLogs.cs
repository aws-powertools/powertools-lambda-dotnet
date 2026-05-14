// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

// --8<-- [start:sampling_attribute]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging(SamplingRate = 0.5)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        ...
    }
}
// --8<-- [end:sampling_attribute]

// --8<-- [start:sampling_configure]
public class Function
{
    public Function()
    {
        Logger.Configure(options =>
        {
            options.MinimumLogLevel = LogLevel.Information;
            options.LoggerOutputCase = LoggerOutputCase.CamelCase;
            options.SamplingRate = 0.1; // 10% sampling
        });
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        Logger.RefreshSampleRateCalculation();
        ...
    }
}
// --8<-- [end:sampling_configure]
