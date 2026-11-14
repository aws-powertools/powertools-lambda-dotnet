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
using AWS.Lambda.Powertools.Docs.Snippets.Idempotency;

namespace AWS.Lambda.Powertools.Docs.Snippets.Idempotency.ExpirationSettings.CustomizingExpirationTime
{
using Idempotency = global::AWS.Lambda.Powertools.Idempotency.Idempotency;
internal static class Example
{
    public static void Run()
    {
// --8<-- [start:customizing_expiration_time]
new IdempotencyOptionsBuilder()
    .WithExpiration(TimeSpan.FromMinutes(5))
    .Build();
// --8<-- [end:customizing_expiration_time]
    }
}
}
