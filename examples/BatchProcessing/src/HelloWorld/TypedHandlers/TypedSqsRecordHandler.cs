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
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;
using HelloWorld.Data;

namespace HelloWorld.TypedHandlers;

/// <summary>
/// Example of a typed record handler that automatically deserializes SQS message body to Product type
/// </summary>
public class TypedSqsRecordHandler : ITypedRecordHandler<Product>
{
    public async Task<RecordHandlerResult> HandleAsync(Product product, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Processing product with ID: {product.Id}, Name: {product.Name}, Price: {product.Price}");

        // Simulate business logic
        if (product.Id == 999)
        {
            throw new ArgumentException("Product ID 999 is not allowed");
        }

        // Simulate async processing
        await Task.Delay(100, cancellationToken);
        
        Logger.LogInformation($"Successfully processed product {product.Id}");
        return RecordHandlerResult.None;
    }
}