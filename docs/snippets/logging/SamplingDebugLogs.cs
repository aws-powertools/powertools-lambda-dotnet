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


namespace Docs.LoggingSnippets.SamplingDebugLogs.SamplingAttribute
{
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
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:sampling_attribute]
}

namespace Docs.LoggingSnippets.SamplingDebugLogs.SamplingConfigure
{
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
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:sampling_configure]
}
