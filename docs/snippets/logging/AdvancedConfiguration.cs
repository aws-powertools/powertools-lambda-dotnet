// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

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

// --8<-- [start:clear_providers]
builder.Logging.AddPowertoolsLogger(config =>
    {
    config.Service = "TestService";
    config.LoggerOutputCase = LoggerOutputCase.PascalCase;
    }, clearExistingProviders: true);
// --8<-- [end:clear_providers]
