using AWS.Lambda.Powertools.Logging.Serializers;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing.Serializers;
using AWS.Lambda.Powertools.Tracing;
// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing
{
// The JSON source generator needs context type names to be unique per assembly.
[JsonSerializable(typeof(string))]
internal partial class TracingLambdaFunctionJsonSerializerContext : JsonSerializerContext;
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing.AotWithoutLogging
{
using Tracing = global::AWS.Lambda.Powertools.Tracing.Tracing;
using LambdaFunctionJsonSerializerContext = AWS.Lambda.Powertools.Docs.Snippets.Tracing.TracingLambdaFunctionJsonSerializerContext;
public class Function
{
    private static string FunctionHandler(string input, ILambdaContext context) => input;

// --8<-- [start:without_powertools_logging]

private static async Task Main()
{
    Func<string, ILambdaContext, string> handler = FunctionHandler;
    await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>()
    .WithTracing())
        .Build()
        .RunAsync();
}
// --8<-- [end:without_powertools_logging]
}
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing.AotWithLogging
{
using Tracing = global::AWS.Lambda.Powertools.Tracing.Tracing;
using LambdaFunctionJsonSerializerContext = AWS.Lambda.Powertools.Docs.Snippets.Tracing.TracingLambdaFunctionJsonSerializerContext;
public class Function
{
    private static string FunctionHandler(string input, ILambdaContext context) => input;

// --8<-- [start:with_powertools_logging]

private static async Task Main()
{
    Func<string, ILambdaContext, string> handler = FunctionHandler;
    await LambdaBootstrapBuilder.Create(handler,
        new PowertoolsSourceGeneratorSerializer<LambdaFunctionJsonSerializerContext>()
        .WithTracing())
            .Build()
            .RunAsync();
}
// --8<-- [end:with_powertools_logging]
}
}
