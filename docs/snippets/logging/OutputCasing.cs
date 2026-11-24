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


namespace Docs.LoggingSnippets.OutputCasing.OutputCasingAttribute
{
// --8<-- [start:output_casing_attribute]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging(LoggerOutputCase = LoggerOutputCase.CamelCase)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:output_casing_attribute]
}
