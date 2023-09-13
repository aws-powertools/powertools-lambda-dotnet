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
using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB.Custom;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB.Handler;

public class HandlerFunction
{
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttribute(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbRecordHandler), ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
    public BatchItemFailuresResponse HandlerUsingAttributeErrorPolicy(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbRecordHandler))]
    public async Task<BatchItemFailuresResponse> HandlerUsingAttributeAsync(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor]
    public BatchItemFailuresResponse HandlerUsingAttributeWithoutHandler(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor]
    public BatchItemFailuresResponse HandlerUsingAttributeWithoutEvent(string _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(BadCustomDynamoDbRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandler(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessor = typeof(BadCustomDynamoDbRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessor(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessorProvider = typeof(BadCustomDynamoDbRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessorProvider(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandlerProvider = typeof(BadCustomDynamoDbRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandlerProvider(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbRecordHandler), BatchProcessor = typeof(CustomDynamoDbBatchProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessor(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbRecordHandler), BatchProcessorProvider = typeof(CustomDynamoDbBatchProcessorProvider))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessorProvider(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtility(DynamoDBEvent dynamoDbEvent)
    {
        var result = await DynamoDbStreamBatchProcessor.Instance.ProcessAsync(dynamoDbEvent, RecordHandler<DynamoDBEvent.DynamodbStreamRecord>.From(record =>
        {
            var product = JsonSerializer.Deserialize<JsonElement>(record.Dynamodb.NewImage["Product"].S);
        
            if (product.GetProperty("Id").GetInt16() == 4)
            {
                throw new ArgumentException("Error on 4");
            }
        }));
        return result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtilityFromIoc(DynamoDBEvent dynamoDbEvent)
    {
        var batchProcessor = Services.Provider.GetRequiredService<CustomDynamoDbBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<CustomDynamoDbRecordHandler>();
        var result = await batchProcessor.ProcessAsync(dynamoDbEvent, recordHandler);
        return result.BatchItemFailuresResponse;
    }

    [BatchProcessor(RecordHandler = typeof(CustomFailDynamoDbRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeAllFail(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
}