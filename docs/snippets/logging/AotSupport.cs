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
using Docs.LoggingSnippets.AotSupportExamples.DemoClass;
using Docs.LoggingSnippets.AotSupportExamples.JsonSerializerContextExample;
using Docs.LoggingSnippets.AotSupportExamples.JsonSerializerOptionsAot;
using static Docs.LoggingSnippets.AotSupportExamples.HandlerSupport;
using CustomLogFormatter = Docs.LoggingSnippets.AotSupportExamples.CustomLogFormatterAotClass.CustomLogFormatter;

// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).


namespace Docs.LoggingSnippets.AotSupportExamples.JsonSerializerOptionsAot
{
using YourJsonSerializerContext = Docs.LoggingSnippets.AotSupportExamples.JsonSerializerContextExample.MyCustomJsonSerializerContext;

public class Example
{
    public void Run()
    {
// --8<-- [start:json_serializer_options_aot]
Logger.Configure(logger =>
{
    logger.JsonOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = YourJsonSerializerContext.Default
    };
});
// --8<-- [end:json_serializer_options_aot]
    }
}
}

namespace Docs.LoggingSnippets.AotSupportExamples.BeforeAot
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

namespace Docs.LoggingSnippets.AotSupportExamples.AfterAot
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

namespace Docs.LoggingSnippets.AotSupportExamples.DemoClass
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

namespace Docs.LoggingSnippets.AotSupportExamples.JsonSerializerContextExample
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

namespace Docs.LoggingSnippets.AotSupportExamples.CustomLogFormatterAotFunction
{
using CustomLogFormatterJsonSerializerContext = Docs.LoggingSnippets.AotSupportExamples.CustomLogFormatterAotClass.CustomLogFormatterJsonSerializerContext;

public class Example
{
    public async Task RunAsync()
    {
// --8<-- [start:custom_log_formatter_aot_function]

Func<APIGatewayHttpApiV2ProxyRequest, ILambdaContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler = FunctionHandler;
await LambdaBootstrapBuilder.Create(handler,
    new PowertoolsSourceGeneratorSerializer<CustomLogFormatterJsonSerializerContext>
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

namespace Docs.LoggingSnippets.AotSupportExamples.CustomLogFormatterAotClass
{
// --8<-- [start:custom_log_formatter_aot_class]
// Under Native AOT, return a type declared in a namespace and registered in your
// JsonSerializerContext. Anonymous types can't be read under Native AOT.
public class CustomLogFormatter : ILogFormatter
{
    public object FormatLogEntry(LogEntry logEntry)
    {
        return new CustomLogEntry(
            Message: logEntry.Message,
            Service: logEntry.Service,
            CorrelationIds: new CorrelationIds(
                AwsRequestId: logEntry.LambdaContext?.AwsRequestId,
                XRayTraceId: logEntry.XRayTraceId,
                CorrelationId: logEntry.CorrelationId),
            LambdaFunction: new LambdaFunctionInfo(
                Name: logEntry.LambdaContext?.FunctionName,
                Arn: logEntry.LambdaContext?.InvokedFunctionArn,
                MemoryLimitInMB: logEntry.LambdaContext?.MemoryLimitInMB,
                Version: logEntry.LambdaContext?.FunctionVersion,
                ColdStart: logEntry.ColdStart),
            Level: logEntry.Level.ToString(),
            Timestamp: logEntry.Timestamp.ToString("o"),
            Logger: new LoggerInfo(
                Name: logEntry.Name,
                SampleRate: logEntry.SamplingRate));
    }
}

public record CustomLogEntry(
    object Message,
    string Service,
    CorrelationIds CorrelationIds,
    LambdaFunctionInfo LambdaFunction,
    string Level,
    string Timestamp,
    LoggerInfo Logger);

public record CorrelationIds(string AwsRequestId, string XRayTraceId, string CorrelationId);

public record LambdaFunctionInfo(string Name, string Arn, int? MemoryLimitInMB, string Version, bool ColdStart);

public record LoggerInfo(string Name, double? SampleRate);

// Register the formatter's output type next to your function's request and response types
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(CustomLogEntry))]
public partial class CustomLogFormatterJsonSerializerContext : JsonSerializerContext
{
}
// --8<-- [end:custom_log_formatter_aot_class]
}

namespace Docs.LoggingSnippets.AotSupportExamples
{
internal static class HandlerSupport
{
    public static Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context) =>
        Task.FromResult(new APIGatewayHttpApiV2ProxyResponse());
}
}
