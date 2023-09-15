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
    private readonly IDynamoDbStreamBatchProcessor _batchProcessor;
    private readonly IDynamoDbStreamRecordHandler _recordHandler;

    public HandlerFunction()
    {
      
    }

    public HandlerFunction(IDynamoDbStreamBatchProcessor batchProcessor, IDynamoDbStreamRecordHandler recordHandler)
    {
        _batchProcessor = batchProcessor;
        _recordHandler = recordHandler;
    }

    
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttribute(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler), ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
    public BatchItemFailuresResponse HandlerUsingAttributeErrorPolicy(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler))]
    public Task<BatchItemFailuresResponse> HandlerUsingAttributeAsync(DynamoDBEvent _)
    {
        return Task.FromResult(DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse);
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
    
    [BatchProcessor(RecordHandler = typeof(BadCustomDynamoDbStreamRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandler(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessor = typeof(BadCustomDynamoDbStreamRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessor(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessorProvider = typeof(BadCustomDynamoDbStreamRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessorProvider(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandlerProvider = typeof(BadCustomDynamoDbStreamRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandlerProvider(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler), BatchProcessor = typeof(CustomDynamoDbStreamBatchProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessor(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler), BatchProcessorProvider = typeof(CustomDynamoDbStreamBatchProcessorProvider))]
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
        var batchProcessor = Services.Provider.GetRequiredService<IDynamoDbStreamBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<IDynamoDbStreamRecordHandler>();
        var result = await batchProcessor.ProcessAsync(dynamoDbEvent, recordHandler);
        return result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtilityFromIocConstructor(DynamoDBEvent dynamoDbEvent)
    {
        var result = await _batchProcessor.ProcessAsync(dynamoDbEvent, _recordHandler);
        return result.BatchItemFailuresResponse;
    }

    [BatchProcessor(RecordHandler = typeof(CustomFailDynamoDbStreamRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeAllFail(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
}