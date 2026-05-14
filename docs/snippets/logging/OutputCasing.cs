// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

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
        ...
    }
}
// --8<-- [end:output_casing_attribute]
