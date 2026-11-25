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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Common.Utils;

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

        // Call the typed batch processor with the data type of the typed handler
        await CallTypedProcessAsync(@event, context);
    }

    private Task CallTypedProcessAsync(TEvent @event, ILambdaContext context)
    {
        var dataType = GetHandlerDataType(_typedHandler);

        // Under Native AOT, MakeGenericMethod only works for reference types, which share canonical code.
        if (!RuntimeFeatureWrapper.IsDynamicCodeSupported && dataType.IsValueType)
        {
            throw new NotSupportedException(
                $"Typed record handler '{_typedHandler.GetType().Name}' handles '{dataType.Name}', a value type, " +
                "which isn't supported when running as Native AOT. Use a class or record for the record data instead.");
        }

        return InvokeProcessAsync(dataType, @event, context);
    }

    /// <summary>
    /// Calls <see cref="ProcessAsync{T}"/> for the data type of the typed record handler.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "When dynamic code isn't supported, ProcessAsync is only instantiated over reference types, which use " +
                        "shared canonical code. Value types are rejected before this is called.")]
    private Task InvokeProcessAsync(Type dataType, TEvent @event, ILambdaContext context)
    {
        // Resolved here, rather than cached in a static field, so the trimmer can see which method
        // MakeGenericMethod is called on (avoids IL2060).
        var processMethod = typeof(TypedBatchProcessingAspectHandler<TEvent, TRecord>)
            .GetMethod(nameof(ProcessAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(dataType);

        return (Task)processMethod.Invoke(this, new object[] { @event, context })!;
    }

    private async Task ProcessAsync<T>(TEvent @event, ILambdaContext context)
    {
        if (_hasContext && context != null && _typedHandler is ITypedRecordHandlerWithContext<T> handlerWithContext)
        {
            await _typedBatchProcessor.ProcessAsync(@event, handlerWithContext, context, _deserializationOptions,
                _processingOptions);
            return;
        }

        if (_typedHandler is not ITypedRecordHandler<T> handler)
        {
            throw new InvalidOperationException(
                $"Typed record handler '{_typedHandler.GetType().Name}' implements ITypedRecordHandlerWithContext<T>. " +
                "Configure it with TypedRecordHandlerWithContext or TypedRecordHandlerWithContextProvider, and add an " +
                "ILambdaContext as the second function handler parameter.");
        }

        await _typedBatchProcessor.ProcessAsync(@event, handler, _deserializationOptions, _processingOptions);
    }

    /// <summary>
    /// Gets T from the <see cref="ITypedRecordHandler{T}"/> or <see cref="ITypedRecordHandlerWithContext{T}"/>
    /// the handler implements.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.",
        Justification = "Only ITypedRecordHandler<T> and ITypedRecordHandlerWithContext<T> are looked for. ProcessAsync<T>, " +
                        "which is always compiled because MakeGenericMethod is called on it, casts the handler to these " +
                        "interfaces, so the trimmer and ILC keep them on the handler types the application creates.")]
    private Type GetHandlerDataType(object typedHandler)
    {
        var handlerType = typedHandler.GetType();
        var preferred = _hasContext ? typeof(ITypedRecordHandlerWithContext<>) : typeof(ITypedRecordHandler<>);
        var other = _hasContext ? typeof(ITypedRecordHandler<>) : typeof(ITypedRecordHandlerWithContext<>);
        var interfaces = handlerType.GetInterfaces().Where(i => i.IsGenericType).ToArray();
        var handlerInterface = interfaces.FirstOrDefault(i => i.GetGenericTypeDefinition() == preferred)
                               ?? interfaces.FirstOrDefault(i => i.GetGenericTypeDefinition() == other);

        if (handlerInterface == null)
        {
            throw new InvalidOperationException($"Handler type '{handlerType.Name}' does not implement ITypedRecordHandler<T> or ITypedRecordHandlerWithContext<T>.");
        }

        return handlerInterface.GetGenericArguments()[0];
    }
}
