// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

// --8<-- [start:json_serializer_options_aot]
Logger.Configure(logger =>
{
    logger.JsonOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = YourJsonSerializerContext.Default
    };
});
// --8<-- [end:json_serializer_options_aot]

// --8<-- [start:before_aot]
 Func<APIGatewayHttpApiV2ProxyRequest, ILambdaContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler = FunctionHandler;
 await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<MyCustomJsonSerializerContext>())
     .Build()
     .RunAsync();
// --8<-- [end:before_aot]

// --8<-- [start:after_aot]
Func<APIGatewayHttpApiV2ProxyRequest, ILambdaContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler = FunctionHandler;
await LambdaBootstrapBuilder.Create(handler, new PowertoolsSourceGeneratorSerializer<MyCustomJsonSerializerContext>())
    .Build()
    .RunAsync();
// --8<-- [end:after_aot]

// --8<-- [start:demo_class]
public class Demo
{
    public string Name { get; set; }
    public Headers Headers { get; set; }
}
// --8<-- [end:demo_class]

// --8<-- [start:json_serializer_context]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(Demo))]
public partial class MyCustomJsonSerializerContext : JsonSerializerContext
{
}
// --8<-- [end:json_serializer_context]

// --8<-- [start:custom_log_formatter_aot_function]

Func<APIGatewayHttpApiV2ProxyRequest, ILambdaContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler = FunctionHandler;
await LambdaBootstrapBuilder.Create(handler,
    new PowertoolsSourceGeneratorSerializer<LambdaFunctionJsonSerializerContext>
    (
        new CustomLogFormatter()
    )
)
.Build()
.RunAsync();

// --8<-- [end:custom_log_formatter_aot_function]

// --8<-- [start:custom_log_formatter_aot_class]
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
// --8<-- [end:custom_log_formatter_aot_class]
