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
using System.Reflection;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

internal class TypedBatchProcessingAspectHandler<TEvent, TRecord> : IBatchProcessingAspectHandler
{
    private readonly ITypedBatchProcessor<TEvent, TRecord> _typedBatchProcessor;
    private readonly object _typedHandler;
    private readonly bool _hasContext;
    private readonly DeserializationOptions _deserializationOptions;
    private readonly ProcessingOptions _processingOptions;

    public TypedBatchProcessingAspectHandler(
        ITypedBatchProcessor<TEvent, TRecord> typedBatchProcessor, 
        object typedHandler, 
        bool hasContext,
        DeserializationOptions deserializationOptions,
        ProcessingOptions processingOptions)
    {
        _typedBatchProcessor = typedBatchProcessor;
        _typedHandler = typedHandler;
        _hasContext = hasContext;
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

        // Get Lambda context if available and needed
        ILambdaContext context = null;
        if (_hasContext && args.Length > 1 && args[1] is ILambdaContext lambdaContext)
        {
            context = lambdaContext;
        }

        // Use reflection to call the appropriate ProcessAsync method on the typed batch processor
        await CallTypedProcessAsync(@event, context);
    }

    private async Task CallTypedProcessAsync(TEvent @event, ILambdaContext context)
    {
        // Get the generic type argument from the handler
        var handlerType = _typedHandler.GetType();
        var handlerInterface = handlerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                                (i.GetGenericTypeDefinition() == typeof(ITypedRecordHandler<>) ||
                                 i.GetGenericTypeDefinition() == typeof(ITypedRecordHandlerWithContext<>)));

        if (handlerInterface == null)
        {
            throw new InvalidOperationException($"Handler type '{handlerType.Name}' does not implement ITypedRecordHandler<T> or ITypedRecordHandlerWithContext<T>.");
        }

        var dataType = handlerInterface.GetGenericArguments()[0];

        // Find the appropriate ProcessAsync method on the typed batch processor
        MethodInfo processMethod;
        if (_hasContext && context != null)
        {
            // Look for ProcessAsync<T>(TEvent, ITypedRecordHandlerWithContext<T>, ILambdaContext, DeserializationOptions, ProcessingOptions)
            processMethod = _typedBatchProcessor.GetType().GetMethods()
                .FirstOrDefault(m => m.Name == "ProcessAsync" &&
                                    m.IsGenericMethodDefinition &&
                                    m.GetParameters().Length == 5 &&
                                    m.GetParameters()[1].ParameterType.IsGenericType &&
                                    m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(ITypedRecordHandlerWithContext<>) &&
                                    m.GetParameters()[2].ParameterType == typeof(ILambdaContext) &&
                                    m.GetParameters()[3].ParameterType == typeof(DeserializationOptions) &&
                                    m.GetParameters()[4].ParameterType == typeof(ProcessingOptions));
        }
        else
        {
            // Look for ProcessAsync<T>(TEvent, ITypedRecordHandler<T>, DeserializationOptions, ProcessingOptions)
            processMethod = _typedBatchProcessor.GetType().GetMethods()
                .FirstOrDefault(m => m.Name == "ProcessAsync" &&
                                    m.IsGenericMethodDefinition &&
                                    m.GetParameters().Length == 4 &&
                                    m.GetParameters()[1].ParameterType.IsGenericType &&
                                    m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(ITypedRecordHandler<>) &&
                                    m.GetParameters()[2].ParameterType == typeof(DeserializationOptions) &&
                                    m.GetParameters()[3].ParameterType == typeof(ProcessingOptions));
        }

        if (processMethod == null)
        {
            throw new InvalidOperationException($"Could not find appropriate ProcessAsync method on typed batch processor for handler type '{handlerType.Name}'.");
        }

        // Make the method generic with the data type
        var genericProcessMethod = processMethod.MakeGenericMethod(dataType);

        // Call the method
        Task processTask;
        if (_hasContext && context != null)
        {
            processTask = (Task)genericProcessMethod.Invoke(_typedBatchProcessor, new object[] { @event, _typedHandler, context, _deserializationOptions, _processingOptions });
        }
        else
        {
            processTask = (Task)genericProcessMethod.Invoke(_typedBatchProcessor, new object[] { @event, _typedHandler, _deserializationOptions, _processingOptions });
        }

        await processTask;
    }
}