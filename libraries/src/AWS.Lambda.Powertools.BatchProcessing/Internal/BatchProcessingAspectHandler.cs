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
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

/// <summary>
///     Class BatchProcessingAspectHandler.
///     Implements the <see cref="IMethodAspectHandler" />
/// </summary>
/// <seealso cref="IMethodAspectHandler" />
internal class BatchProcessingAspectHandler<TEvent, TRecord> : IMethodAspectHandler
{
    private readonly IBatchProcessor<TEvent, TRecord> _batchProcessor;
    private readonly IRecordHandler<TRecord> _recordHandler;

    public BatchProcessingAspectHandler(
        IBatchProcessor<TEvent, TRecord> batchProcessor,
        IRecordHandler<TRecord> recordHandler)
    {
        _batchProcessor = batchProcessor;
        _recordHandler = recordHandler;
    }

    /// <summary>
    ///     Handles the <see cref="E:Entry" /> event.
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    public void OnEntry(AspectEventArgs eventArgs)
    {
        Console.WriteLine("Aspect: OnEntry");

        // Try get event from args
        var @event = eventArgs.Args.OfType<TEvent>().SingleOrDefault();
        if (@event == null)
        {
            throw new InvalidOperationException($"Function handler must accept a single '{typeof(TEvent).Name}' argument.");
        }

        // Run processor
        // TODO: Consider framework support for async aspect events
        _batchProcessor.ProcessAsync(@event, _recordHandler).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Called when [success].
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    /// <param name="result">The result.</param>
    public void OnSuccess(AspectEventArgs eventArgs, object result)
    {
        Console.WriteLine("Aspect: OnSuccess");
    }

    /// <summary>
    ///     Called when [exception].
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    /// <param name="exception">The exception.</param>
    /// <returns>T.</returns>
    public T OnException<T>(AspectEventArgs eventArgs, Exception exception)
    {
        Console.WriteLine("Aspect: OnException");
        throw exception;
    }

    /// <summary>
    ///     Handles the <see cref="E:Exit" /> event.
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    public void OnExit(AspectEventArgs eventArgs)
    {
        Console.WriteLine("Aspect: OnExit");
    }

    /// <summary>
    ///     Resets for test.
    /// </summary>
    internal static void ResetForTest()
    {
    }
}