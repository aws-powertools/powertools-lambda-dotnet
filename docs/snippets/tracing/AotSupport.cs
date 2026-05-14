// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing;

// --8<-- [start:without_powertools_logging]
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Tracing.Serializers;

private static async Task Main()
{
    Func<string, ILambdaContext, string> handler = FunctionHandler;
    await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>()
    .WithTracing())
        .Build()
        .RunAsync();
}
// --8<-- [end:without_powertools_logging]

// --8<-- [start:with_powertools_logging]
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Logging.Serializers;
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Tracing.Serializers;

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
