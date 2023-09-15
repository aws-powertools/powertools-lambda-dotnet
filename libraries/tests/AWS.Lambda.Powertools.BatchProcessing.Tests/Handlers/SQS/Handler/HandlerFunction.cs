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
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Custom;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Handler;

public class HandlerFunction
{
    private readonly ISqsBatchProcessor _batchProcessor;
    private readonly ISqsRecordHandler _recordHandler;

    public HandlerFunction()
    {
      
    }

    public HandlerFunction(ISqsBatchProcessor batchProcessor, ISqsRecordHandler recordHandler)
    {
        _batchProcessor = batchProcessor;
        _recordHandler = recordHandler;
    }
    
    
    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttribute(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomFailSqsRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeAllFail(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler), ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
    public BatchItemFailuresResponse HandlerUsingAttributeErrorPolicy(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler))]
    public Task<BatchItemFailuresResponse> HandlerUsingAttributeAsync(SQSEvent _)
    {
        return Task.FromResult(SqsBatchProcessor.Result.BatchItemFailuresResponse);
    }
    
    [BatchProcessor]
    public BatchItemFailuresResponse HandlerUsingAttributeWithoutHandler(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor]
    public BatchItemFailuresResponse HandlerUsingAttributeWithoutEvent(string _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(BadCustomSqsRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandler(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessor = typeof(BadCustomSqsRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessor(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessorProvider = typeof(BadCustomSqsRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessorProvider(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandlerProvider = typeof(BadCustomSqsRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandlerProvider(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler), BatchProcessor = typeof(CustomSqsBatchProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessor(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler), BatchProcessorProvider = typeof(CustomSqsBatchProcessorProvider))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessorProvider(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtility(SQSEvent sqsEvent)
    {
        var result = await SqsBatchProcessor.Instance.ProcessAsync(sqsEvent, RecordHandler<SQSEvent.SQSMessage>.From(sqsMessage =>
        {
            var product = JsonSerializer.Deserialize<JsonElement>(sqsMessage.Body);
        
            if (product.GetProperty("Id").GetInt16() == 4)
            {
                throw new ArgumentException("Error on 4");
            }
        }));
        return result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtilityFromIoc(SQSEvent sqsEvent)
    {
        var batchProcessor = Services.Provider.GetRequiredService<ISqsBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<ISqsRecordHandler>();
        var result = await batchProcessor.ProcessAsync(sqsEvent, recordHandler);
        return result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtilityFromIocConstructor(SQSEvent sqsEvent)
    {
        var result = await _batchProcessor.ProcessAsync(sqsEvent, _recordHandler);
        return result.BatchItemFailuresResponse;
    }
}