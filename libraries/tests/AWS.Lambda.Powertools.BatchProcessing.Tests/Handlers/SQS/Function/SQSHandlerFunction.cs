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
using AWS.Lambda.Powertools.BatchProcessing.Tests.Helpers;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Function;

public class SQSHandlerFunction
{
    [BatchProcesser(RecordHandler = typeof(CustomSqsRecordHandler))]
    public BatchItemFailuresResponse SqsHandlerUsingAttribute(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcesser(RecordHandler = typeof(CustomSqsRecordHandler))]
    public async Task<BatchItemFailuresResponse> SqsHandlerUsingAttributeAsync(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcesser]
    public BatchItemFailuresResponse SqsHandlerUsingAttributeWithoutHandler(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcesser]
    public BatchItemFailuresResponse SqsHandlerUsingAttributeWithoutEvent(string _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcesser(RecordHandler = typeof(BadCustomSqsRecordHandler))]
    public BatchItemFailuresResponse SqsHandlerUsingAttributeBadHandler(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcesser(BatchProcessor = typeof(BadCustomSqsRecordProcessor))]
    public BatchItemFailuresResponse SqsHandlerUsingAttributeBadProcessor(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcesser(BatchProcessorProvider = typeof(BadCustomSqsRecordProcessor))]
    public BatchItemFailuresResponse SqsHandlerUsingAttributeBadProcessorProvider(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcesser(RecordHandlerProvider = typeof(BadCustomSqsRecordHandler))]
    public BatchItemFailuresResponse SqsHandlerUsingAttributeBadHandlerProvider(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcesser(RecordHandler = typeof(CustomSqsRecordHandler), BatchProcessor = typeof(CustomSqsBatchProcessor))]
    public BatchItemFailuresResponse SqsHandlerUsingAttributeAndCustomBatchProcessor(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    [BatchProcesser(RecordHandler = typeof(CustomSqsRecordHandler), BatchProcessorProvider = typeof(CustomSqsBatchProcessorProvider))]
    public BatchItemFailuresResponse SqsHandlerUsingAttributeAndCustomBatchProcessorProvider(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtility(SQSEvent sqsEvent)
    {
        var result = await SqsBatchProcessor.Instance.ProcessAsync(sqsEvent, RecordHandler<SQSEvent.SQSMessage>.From(sqsMessage =>
        {
            Logger.LogInformation($"Inline handling of SQS message with body: '{sqsMessage.Body}'.");
            var product = JsonSerializer.Deserialize<JsonElement>(sqsMessage.Body);
        
            Logger.LogInformation($"Retried product {product}");
        
            if (product.GetProperty("Id").GetInt16() == 4)
            {
                Logger.LogInformation($"Error on product 4");
                throw new ArgumentException("Error on 4");
            }
        }));
        return result.BatchItemFailuresResponse;
    }
    
    public async Task<BatchItemFailuresResponse> HandlerUsingUtilityFromIoc(SQSEvent sqsEvent)
    {
        var batchProcessor = Services.Provider.GetRequiredService<CustomSqsBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<CustomSqsRecordHandler>();
        var result = await batchProcessor.ProcessAsync(sqsEvent, recordHandler);
        return result.BatchItemFailuresResponse;
    }
}