// This file is referenced by docs/utilities/idempotency.md
// via pymdownx.snippets (mkdocs).

using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using Docs.IdempotencySnippets;

namespace Docs.IdempotencySnippets.PayloadValidation.PayloadValidationJmespath
{
internal static class Example
{
    public static void Run()
    {
// --8<-- [start:payload_validation_jmespath]
    Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder
                        .WithEventKeyJmesPath("[userDetail, productId]")
                        .WithPayloadValidationJmesPath("amount"))
                .UseDynamoDb("TABLE_NAME"));
// --8<-- [end:payload_validation_jmespath]
    }
}
}
