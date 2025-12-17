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

namespace HelloWorld.Kinesis;

/// <summary>
/// Typed record handler for Kinesis records that automatically deserializes to Product.
/// No manual JSON deserialization needed - the batch processor handles it.
/// </summary>
public class TypedKinesisProductHandler : ITypedRecordHandler<Product>
{
    public Task<RecordHandlerResult> HandleAsync(Product product, CancellationToken cancellationToken)
    {
        /*
         * Your business logic with strongly-typed data.
         * The batch processor automatically deserializes the Kinesis record data to Product.
         * If an exception is thrown, the item will be marked as a partial batch item failure.
         */
        Logger.LogInformation($"[Typed] Handling product with id: {product.Id}, name: {product.Name}");

        if (product.Id == 4)
        {
            throw new ArgumentException("Error on id 4");
        }
        
        return Task.FromResult(RecordHandlerResult.None);
    }
}
