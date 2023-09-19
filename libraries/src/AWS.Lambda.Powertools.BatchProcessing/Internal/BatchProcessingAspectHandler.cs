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
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

internal class BatchProcessingAspectHandler<TEvent, TRecord> : IBatchProcessingAspectHandler
{
    private readonly IBatchProcessor<TEvent, TRecord> _batchProcessor;
    private readonly IRecordHandler<TRecord> _recordHandler;
    private readonly ProcessingOptions _processingOptions;

    public BatchProcessingAspectHandler(IBatchProcessor<TEvent, TRecord> batchProcessor, IRecordHandler<TRecord> recordHandler, ProcessingOptions processingOptions)
    {
        _batchProcessor = batchProcessor;
        _recordHandler = recordHandler;
        _processingOptions = processingOptions;
    }

    public async Task HandleAsync(object[] args)
    {
        // Try get event from args
        if (args?.FirstOrDefault() is not TEvent @event)
        {
            throw new InvalidOperationException($"The first function handler parameter must be of type: '{typeof(TEvent).Namespace}'.");
        }

        // Run batch processor
        await _batchProcessor.ProcessAsync(@event, _recordHandler, _processingOptions);
    }
}