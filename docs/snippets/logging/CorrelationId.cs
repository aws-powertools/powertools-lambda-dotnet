// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

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
        ...
    }
}
// --8<-- [end:correlation_id_custom]

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
        ...
    }
}
// --8<-- [end:correlation_id_builtin]
