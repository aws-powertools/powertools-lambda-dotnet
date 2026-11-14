using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using ILambdaContext = Amazon.Lambda.Core.ILambdaContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using CustomLogFormatter = AWS.Lambda.Powertools.Docs.Snippets.Logging.CustomLogFormatterExamples.CustomLogFormatterClass.CustomLogFormatter;

// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).


namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.CustomLogFormatterExamples.CustomLogFormatterFunction
{
// --8<-- [start:custom_log_formatter_function]
/**
 * Handler for requests to Lambda function.
 */
public class Function
{
    /// <summary>
    /// Function constructor
    /// </summary>
    public Function()
    {
        Logger.Configure(options =>
        {
            options.LogFormatter = new CustomLogFormatter();
        });
    }

    [Logging(CorrelationIdPath = "/headers/my_request_id_header", SamplingRate = 0.7)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // ...
        return new APIGatewayProxyResponse();
    }
}
// --8<-- [end:custom_log_formatter_function]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.CustomLogFormatterExamples.CustomLogFormatterClass
{
// --8<-- [start:custom_log_formatter_class]
public class CustomLogFormatter : ILogFormatter
{
    public object FormatLogEntry(LogEntry logEntry)
    {
        return new
        {
            Message = logEntry.Message,
            Service = logEntry.Service,
            CorrelationIds = new
            {
                AwsRequestId = logEntry.LambdaContext?.AwsRequestId,
                XRayTraceId = logEntry.XRayTraceId,
                CorrelationId = logEntry.CorrelationId
            },
            LambdaFunction = new
            {
                Name = logEntry.LambdaContext?.FunctionName,
                Arn = logEntry.LambdaContext?.InvokedFunctionArn,
                MemoryLimitInMB = logEntry.LambdaContext?.MemoryLimitInMB,
                Version = logEntry.LambdaContext?.FunctionVersion,
                ColdStart = logEntry.ColdStart,
            },
            Level = logEntry.Level.ToString(),
            Timestamp = logEntry.Timestamp.ToString("o"),
            Logger = new
            {
                Name = logEntry.Name,
                SampleRate = logEntry.SamplingRate
            },
        };
    }
}
// --8<-- [end:custom_log_formatter_class]
}
