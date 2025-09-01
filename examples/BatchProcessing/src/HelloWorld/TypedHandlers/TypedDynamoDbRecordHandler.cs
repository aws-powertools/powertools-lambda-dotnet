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
/// Example of a typed record handler for DynamoDB stream events
/// </summary>
public class TypedDynamoDbRecordHandler : ITypedRecordHandler<Customer>
{
    public static async Task<RecordHandlerResult> HandleAsync(Customer customer, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Processing customer change: {customer.CustomerId} - {customer.Name}");
        Logger.LogInformation($"Customer email: {customer.Email}, Created: {customer.CreatedAt}");

        // Simulate customer validation
        if (string.IsNullOrEmpty(customer.CustomerId))
        {
            throw new ArgumentException("Customer ID cannot be empty");
        }

        if (string.IsNullOrEmpty(customer.Email) || !customer.Email.Contains("@"))
        {
            throw new ArgumentException("Valid email address is required");
        }

        // Simulate async processing (e.g., updating search index, sending welcome email)
        await Task.Delay(150, cancellationToken);
        
        Logger.LogInformation($"Successfully processed customer {customer.CustomerId}");
        return RecordHandlerResult.None;
    }
}