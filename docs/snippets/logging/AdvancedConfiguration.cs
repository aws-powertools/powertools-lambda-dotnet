using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using ILambdaContext = Amazon.Lambda.Core.ILambdaContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Logging;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Docs.Snippets.Logging.AotSupportExamples.JsonSerializerContextExample;

// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).


namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AdvancedConfigurationExamples.JsonSerializerOptionsExample
{
public class Example
{
    private readonly Microsoft.Extensions.Hosting.HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
    public void Run()
    {
// --8<-- [start:json_serializer_options]
builder.Logging.AddPowertoolsLogger(options =>
{
    options.JsonOptions = new JsonSerializerOptions
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase, // Override output casing
        TypeInfoResolver = MyCustomJsonSerializerContext.Default // Your custom JsonSerializerContext
    };
});
// --8<-- [end:json_serializer_options]
    }
}
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging.AdvancedConfigurationExamples.ClearProviders
{
public class Example
{
    private readonly Microsoft.Extensions.Hosting.HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
    public void Run()
    {
// --8<-- [start:clear_providers]
builder.Logging.AddPowertoolsLogger(config =>
    {
    config.Service = "TestService";
    config.LoggerOutputCase = LoggerOutputCase.PascalCase;
    }, clearExistingProviders: true);
// --8<-- [end:clear_providers]
    }
}
}
