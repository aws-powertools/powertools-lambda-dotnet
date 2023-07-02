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

using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// The <see cref="IBatchProcessor{TEvent,TRecord}"/> interface.
/// </summary>
/// <typeparam name="TEvent">Type of batch event.</typeparam>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public interface IBatchProcessor<in TEvent, TRecord>
{
    /// <summary>
    /// The <see cref="ProcessingResult{TRecord}"/> of the latest batch processing run. This includes a <see cref="BatchItemFailuresResponse"/> object with the identifiers of the batch items that failed processing.
    /// </summary>
    ProcessingResult<TRecord> ProcessingResult { get; }

    /// <inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/></param>
    Task<ProcessingResult<TRecord>> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler);

    /// <inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/>
    /// <param name="event"><inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/></param>
    /// <param name="recordHandler"><inheritdoc cref="ProcessAsync(TEvent,AWS.Lambda.Powertools.BatchProcessing.IRecordHandler{TRecord}, ProcessingOptions)"/></param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    Task<ProcessingResult<TRecord>> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a batch event.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="recordHandler">The record handler containing the per-record processing logic.</param>
    /// <param name="processingOptions">Processing options to control settings such as cancellation, error handling policy and parallelism.</param>
    /// <returns>A <see cref="ProcessingResult{TRecord}"/> of the latest batch processing run. This includes a <see cref="BatchItemFailuresResponse"/> object with the identifiers of the batch items that failed processing.</returns>
    Task<ProcessingResult<TRecord>> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, ProcessingOptions processingOptions);
}
