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
/// Example of a typed record handler for Kinesis events
/// </summary>
public class TypedKinesisRecordHandler : ITypedRecordHandler<Order>
{
    public static async Task<RecordHandlerResult> HandleAsync(Order order, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Processing order {order.OrderId} for customer {order.CustomerId}");
        Logger.LogInformation($"Order contains {order.Items.Count} items with total amount {order.TotalAmount}");

        // Simulate order validation
        if (order.TotalAmount <= 0)
        {
            throw new ArgumentException("Order total amount must be positive");
        }

        if (string.IsNullOrEmpty(order.CustomerId))
        {
            throw new ArgumentException("Customer ID is required");
        }

        // Simulate async processing (e.g., updating inventory, sending notifications)
        await Task.Delay(200, cancellationToken);
        
        Logger.LogInformation($"Successfully processed order {order.OrderId}");
        return RecordHandlerResult.None;
    }
}