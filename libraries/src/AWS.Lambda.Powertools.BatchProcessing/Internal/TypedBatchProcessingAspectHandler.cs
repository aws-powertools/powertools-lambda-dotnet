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
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

internal class TypedBatchProcessingAspectHandler<TEvent, TRecord, T> : IBatchProcessingAspectHandler
{
    private readonly ITypedBatchProcessor<TEvent, TRecord> _batchProcessor;
    private readonly ITypedRecordHandler<T> _typedRecordHandler;
    private readonly ITypedRecordHandlerWithContext<T> _typedRecordHandlerWithContext;
    private readonly DeserializationOptions _deserializationOptions;
    private readonly ProcessingOptions _processingOptions;

    public TypedBatchProcessingAspectHandler(
        ITypedBatchProcessor<TEvent, TRecord> batchProcessor, 
        ITypedRecordHandler<T> typedRecordHandler,
        DeserializationOptions deserializationOptions,
        ProcessingOptions processingOptions)
    {
        _batchProcessor = batchProcessor;
        _typedRecordHandler = typedRecordHandler;
        _deserializationOptions = deserializationOptions;
        _processingOptions = processingOptions;
    }

    public TypedBatchProcessingAspectHandler(
        ITypedBatchProcessor<TEvent, TRecord> batchProcessor, 
        ITypedRecordHandlerWithContext<T> typedRecordHandlerWithContext,
        DeserializationOptions deserializationOptions,
        ProcessingOptions processingOptions)
    {
        _batchProcessor = batchProcessor;
        _typedRecordHandlerWithContext = typedRecordHandlerWithContext;
        _deserializationOptions = deserializationOptions;
        _processingOptions = processingOptions;
    }

    public async Task HandleAsync(object[] args)
    {
        // Try get event from args
        if (args?.FirstOrDefault() is not TEvent @event)
        {
            throw new InvalidOperationException($"The first function handler parameter must be of type: '{typeof(TEvent).Namespace}'.");
        }

        // Try get Lambda context from args (optional)
        var context = args.OfType<ILambdaContext>().FirstOrDefault();

        // Run batch processor with appropriate handler
        if (_typedRecordHandler != null)
        {
            // For handlers without context, we still pass context to the processor
            // The processor will handle context injection automatically
            if (context != null)
            {
                // Create a wrapper that can handle context injection
                var contextWrapper = new AWS.Lambda.Powertools.BatchProcessing.TypedRecordHandlerWrapper<T>(_typedRecordHandler);
                await _batchProcessor.ProcessAsync(@event, contextWrapper, context, _deserializationOptions, _processingOptions);
            }
            else
            {
                await _batchProcessor.ProcessAsync(@event, _typedRecordHandler, _deserializationOptions, _processingOptions);
            }
        }
        else if (_typedRecordHandlerWithContext != null)
        {
            await _batchProcessor.ProcessAsync(@event, _typedRecordHandlerWithContext, context, _deserializationOptions, _processingOptions);
        }
        else
        {
            throw new InvalidOperationException("No typed record handler was provided.");
        }
    }
}