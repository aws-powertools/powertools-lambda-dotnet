/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Handlers;

/// <summary>
/// Lambda function with multiple idempotent methods to test cross-method key isolation.
/// This tests the scenario where two different methods are decorated with [Idempotent]
/// and called with the same IdempotencyKey value - they should create separate entries
/// in the persistence store.
/// </summary>
public class IdempotencyMultipleMethodsFunction
{
    public bool Method1Called { get; private set; }
    public bool Method2Called { get; private set; }

    public (Basket, Basket) HandleRequest([IdempotencyKey] string key, ILambdaContext context)
    {
        Idempotency.RegisterLambdaContext(context);

        // Call both methods with the same key - they should each create their own idempotency record
        var result1 = Method1(key);
        var result2 = Method2(key);

        return (result1, result2);
    }

    [Idempotent]
    private Basket Method1([IdempotencyKey] string key)
    {
        Method1Called = true;
        return new Basket(new Product(1, "Product from Method1", 10.0));
    }

    [Idempotent]
    private Basket Method2([IdempotencyKey] string key)
    {
        Method2Called = true;
        return new Basket(new Product(2, "Product from Method2", 20.0));
    }
}
