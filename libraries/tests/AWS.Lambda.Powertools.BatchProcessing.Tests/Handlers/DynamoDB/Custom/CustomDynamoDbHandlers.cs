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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.Logging;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB.Custom;

internal class CustomDynamoDbRecordHandler : DynamoDBCustomRecordHandler
{
    public override async Task<RecordHandlerResult> HandleAsync(DynamoDBEvent.DynamodbStreamRecord record,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Handling DynamoDb record with sequence number: '{record.Dynamodb.SequenceNumber}'.");

        var product = JsonSerializer.Deserialize<JsonElement>(record.Dynamodb.NewImage["Product"].S);

        Logger.LogInformation($"Retried product {product}");

        if (product.GetProperty("Id").GetInt16() == 4)
        {
            throw new ArgumentException("Error on id 4");
        }

        // Return some data (not needed but useful for test coverage)
        return await Task.FromResult(RecordHandlerResult.FromData(product));
    }
}

public class BadCustomDynamoDbRecordHandler
{
}