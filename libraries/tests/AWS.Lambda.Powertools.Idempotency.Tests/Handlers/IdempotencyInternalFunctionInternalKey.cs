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
/// Simple Lambda function with Idempotent attribute on a sub method (not the Lambda handler one)
/// </summary>
public class IdempotencyInternalFunctionInternalKey
{
    public Basket HandleRequest(Product input, ILambdaContext context) 
    {
        return CreateBasket(input);
    }

    [Idempotent]
    private Basket CreateBasket(Product p)
    {
        return new Basket(p);
    }
}