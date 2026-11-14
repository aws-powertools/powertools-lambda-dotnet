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

namespace Docs.TracingSnippets
{
// The JSON source generator needs context type names to be unique per assembly.
[JsonSerializable(typeof(string))]
internal partial class TracingLambdaFunctionJsonSerializerContext : JsonSerializerContext;
}

namespace Docs.TracingSnippets.AotWithoutLogging
{
using LambdaFunctionJsonSerializerContext = Docs.TracingSnippets.TracingLambdaFunctionJsonSerializerContext;
// --8<-- [start:without_powertools_logging]
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Tracing.Serializers;

public class Function { private static string FunctionHandler(string input, ILambdaContext context) => input; // --8<-- [start:hidden]
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

namespace Docs.TracingSnippets.AotWithLogging
{
using LambdaFunctionJsonSerializerContext = Docs.TracingSnippets.TracingLambdaFunctionJsonSerializerContext;
// --8<-- [start:with_powertools_logging]
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Logging.Serializers;
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Tracing.Serializers;

public class Function { private static string FunctionHandler(string input, ILambdaContext context) => input; // --8<-- [start:hidden]
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
