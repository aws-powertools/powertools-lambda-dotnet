using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using ILambdaContext = Amazon.Lambda.Core.ILambdaContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Logging.Serializers;
using AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.DemoClass;
using AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.JsonSerializerContextExample;
using AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.JsonSerializerOptionsAot;
using static AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.HandlerSupport;
using CustomLogFormatter = AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.CustomLogFormatterAotClass.CustomLogFormatter;

// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).


namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.JsonSerializerOptionsAot
{
public class Example
{
    public void Run()
    {
// --8<-- [start:json_serializer_options_aot]
Logger.Configure(logger =>
{
    logger.JsonOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = MyCustomJsonSerializerContext.Default
    };
});
// --8<-- [end:json_serializer_options_aot]
    }
}
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.BeforeAot
{
public class Example
{
    public async Task RunAsync()
    {
// --8<-- [start:before_aot]
 Func<APIGatewayHttpApiV2ProxyRequest, ILambdaContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler = FunctionHandler;
 await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<MyCustomJsonSerializerContext>())
     .Build()
     .RunAsync();
// --8<-- [end:before_aot]
    }
}
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.AfterAot
{
public class Example
{
    public async Task RunAsync()
    {
// --8<-- [start:after_aot]
Func<APIGatewayHttpApiV2ProxyRequest, ILambdaContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler = FunctionHandler;
await LambdaBootstrapBuilder.Create(handler, new PowertoolsSourceGeneratorSerializer<MyCustomJsonSerializerContext>())
    .Build()
    .RunAsync();
// --8<-- [end:after_aot]
    }
}
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.DemoClass
{
// --8<-- [start:demo_class]
public class Demo
{
    public string Name { get; set; }
    public Headers Headers { get; set; }
}
// --8<-- [end:demo_class]

public class Headers : Dictionary<string, string> { }
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.JsonSerializerContextExample
{
// --8<-- [start:json_serializer_context]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(Demo))]
public partial class MyCustomJsonSerializerContext : JsonSerializerContext
{
}
// --8<-- [end:json_serializer_context]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.CustomLogFormatterAotFunction
{
public class Example
{
    public async Task RunAsync()
    {
// --8<-- [start:custom_log_formatter_aot_function]

Func<APIGatewayHttpApiV2ProxyRequest, ILambdaContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler = FunctionHandler;
await LambdaBootstrapBuilder.Create(handler,
    new PowertoolsSourceGeneratorSerializer<MyCustomJsonSerializerContext>
    (
        new CustomLogFormatter()
    )
)
.Build()
.RunAsync();

// --8<-- [end:custom_log_formatter_aot_function]
    }
}
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.CustomLogFormatterAotClass
{
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
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples
{
internal static class HandlerSupport
{
    public static Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context) =>
        Task.FromResult(new APIGatewayHttpApiV2ProxyResponse());
}
}
