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


namespace Docs.LoggingSnippets.CorrelationId.CorrelationIdCustom
{
// --8<-- [start:correlation_id_custom]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging(CorrelationIdPath = "/headers/my_request_id_header")]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:correlation_id_custom]
}

namespace Docs.LoggingSnippets.CorrelationId.CorrelationIdBuiltin
{
// --8<-- [start:correlation_id_builtin]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    [Logging(CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:correlation_id_builtin]
}
