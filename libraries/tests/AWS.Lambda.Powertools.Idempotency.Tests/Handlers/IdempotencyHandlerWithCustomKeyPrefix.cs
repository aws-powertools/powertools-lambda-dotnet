using System;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Handlers;

/// <summary>
/// Simple Lambda function with Idempotent on handler with a custom prefix key
/// </summary>
public class IdempotencyHandlerWithCustomKeyPrefix
{
    [Idempotent(KeyPrefix = "MyHandler")]
    public string HandleRequest(string input, ILambdaContext context)
    {
        return Guid.NewGuid().ToString();
    }
}