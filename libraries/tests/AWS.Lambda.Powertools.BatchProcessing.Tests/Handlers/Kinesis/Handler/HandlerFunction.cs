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
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis.Custom;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis.Handler;

public class HandlerFunction
{
    [BatchProcessor(RecordHandler = typeof(CustomKinesisEventRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttribute(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomFailKinesisEventRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeAllFail(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomKinesisEventRecordHandler), ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
    public BatchItemFailuresResponse HandlerUsingAttributeErrorPolicy(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomKinesisEventRecordHandler))]
    public Task<BatchItemFailuresResponse> HandlerUsingAttributeAsync(KinesisEvent _)
    {
        return Task.FromResult(KinesisEventBatchProcessor.Result.BatchItemFailuresResponse);
    }
    
    [BatchProcessor]
    public BatchItemFailuresResponse HandlerUsingAttributeWithoutHandler(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor]
    public BatchItemFailuresResponse HandlerUsingAttributeWithoutEvent(string _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(BadCustomKinesisEventRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandler(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessor = typeof(BadCustomKinesisEventRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessor(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessorProvider = typeof(BadCustomKinesisEventRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessorProvider(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandlerProvider = typeof(BadCustomKinesisEventRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandlerProvider(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomKinesisEventRecordHandler), BatchProcessor = typeof(CustomKinesisEventBatchProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessor(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomKinesisEventRecordHandler), BatchProcessorProvider = typeof(CustomKinesisEventBatchProcessorProvider))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessorProvider(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtility(KinesisEvent kinesisEvent)
    {
        var result = await KinesisEventBatchProcessor.Instance.ProcessAsync(kinesisEvent, RecordHandler<KinesisEvent.KinesisEventRecord>.From(kinesisRecord =>
        {
            var product = JsonSerializer.Deserialize<JsonElement>(kinesisRecord.Kinesis.Data);
        
            if (product.GetProperty("Id").GetInt16() == 4)
            {
                throw new ArgumentException("Error on 4");
            }
        }));
        return result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtilityFromIoc(KinesisEvent kinesisEvent)
    {
        var batchProcessor = Services.Provider.GetRequiredService<CustomKinesisEventBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<CustomKinesisEventRecordHandler>();
        var result = await batchProcessor.ProcessAsync(kinesisEvent, recordHandler);
        return result.BatchItemFailuresResponse;
    }
}