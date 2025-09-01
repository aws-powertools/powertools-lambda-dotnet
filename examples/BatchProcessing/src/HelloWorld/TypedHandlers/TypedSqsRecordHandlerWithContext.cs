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
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;
using HelloWorld.Data;

namespace HelloWorld.TypedHandlers;

/// <summary>
/// Example of a typed record handler with Lambda context access
/// </summary>
public class TypedSqsRecordHandlerWithContext : ITypedRecordHandlerWithContext<Product>
{
    public static async Task<RecordHandlerResult> HandleAsync(Product product, ILambdaContext context, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Processing product {product.Id} in request {context.AwsRequestId}");
        Logger.LogInformation($"Remaining time: {context.RemainingTime.TotalMilliseconds}ms");

        // Example of using context for timeout handling
        if (context.RemainingTime.TotalSeconds < 5)
        {
            Logger.LogWarning("Low remaining time, processing quickly");
        }

        // Simulate business logic
        if (product.Price < 0)
        {
            throw new ArgumentException("Product price cannot be negative");
        }

        await Task.Delay(50, cancellationToken);
        
        Logger.LogInformation($"Successfully processed product {product.Id} with context");
        return RecordHandlerResult.None;
    }
}