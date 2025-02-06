using System;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Handlers;

/// <summary>
/// Simple Lambda function with Idempotent attribute on a sub method with a custom prefix key
/// </summary>
public class IdempotencyAttributeWithCustomKeyPrefix
{
    public string HandleRequest(string input, ILambdaContext context) 
    {
        return ReturnGuid(input);
    }

    [Idempotent(KeyPrefix = "MyMethod")]
    private string ReturnGuid(string p)
    {
        return Guid.NewGuid().ToString();
    }
}