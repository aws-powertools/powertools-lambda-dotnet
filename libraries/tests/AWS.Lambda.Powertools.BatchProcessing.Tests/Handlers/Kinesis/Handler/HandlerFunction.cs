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
    [BatchProcessor(RecordHandler = typeof(CustomKinesisDataStreamRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttribute(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomFailKinesisDataStreamRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeAllFail(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomKinesisDataStreamRecordHandler), ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
    public BatchItemFailuresResponse HandlerUsingAttributeErrorPolicy(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomKinesisDataStreamRecordHandler))]
    public Task<BatchItemFailuresResponse> HandlerUsingAttributeAsync(KinesisEvent _)
    {
        return Task.FromResult(KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse);
    }
    
    [BatchProcessor]
    public BatchItemFailuresResponse HandlerUsingAttributeWithoutHandler(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor]
    public BatchItemFailuresResponse HandlerUsingAttributeWithoutEvent(string _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(BadCustomKinesisRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandler(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessor = typeof(BadCustomKinesisDataStreamRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessor(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(BatchProcessorProvider = typeof(BadCustomKinesisDataStreamRecordProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadProcessorProvider(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandlerProvider = typeof(BadCustomKinesisRecordHandler))]
    public BatchItemFailuresResponse HandlerUsingAttributeBadHandlerProvider(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomKinesisDataStreamRecordHandler), BatchProcessor = typeof(CustomKinesisDataStreamBatchProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessor(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcessor(RecordHandler = typeof(CustomKinesisDataStreamRecordHandler), BatchProcessorProvider = typeof(CustomKinesisDataStreamBatchProcessorProvider))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessorProvider(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtility(KinesisEvent kinesisEvent)
    {
        var result = await KinesisDataStreamBatchProcessor.Instance.ProcessAsync(kinesisEvent, RecordHandler<KinesisEvent.KinesisEventRecord>.From(kinesisRecord =>
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
        var batchProcessor = Services.Provider.GetRequiredService<CustomKinesisDataStreamBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<CustomKinesisDataStreamRecordHandler>();
        var result = await batchProcessor.ProcessAsync(kinesisEvent, recordHandler);
        return result.BatchItemFailuresResponse;
    }
}