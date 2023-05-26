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

using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Handlers;

public interface IIdempotencyWithErrorFunction
{
    Task<Basket> HandleTest(Product input, ILambdaContext context);
}

public class IdempotencyWithErrorFunction : IIdempotencyWithErrorFunction
{
    [Idempotent]
    public Task<Basket> Handle(Product input, ILambdaContext context) 
        => throw new IndexOutOfRangeException("Fake exception");

    public Task<Basket> HandleTest(Product input, ILambdaContext context)
    {
        return Handle(input, context);
    }
}

public class IdempotencyWithErrorSyncFunction : IIdempotencyWithErrorFunction
{
    [Idempotent]
    public Basket Handle(Product input, ILambdaContext context) 
        => throw new IndexOutOfRangeException("Fake exception");

    public Task<Basket> HandleTest(Product input, ILambdaContext context)
    {
        return Task.FromResult(Handle(input, context));
    }
}