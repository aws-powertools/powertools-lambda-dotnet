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
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Helper class to create inline record handlers.
/// </summary>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public class RecordHandler<TRecord> : IRecordHandler<TRecord>
{
    private readonly Func<TRecord, Task<RecordHandlerResult>> _handlerFunc;

    private RecordHandler(Func<TRecord, Task<RecordHandlerResult>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    /// <inheritdoc />
    public async Task<RecordHandlerResult> HandleAsync(TRecord record, CancellationToken cancellationToken)
    {
        return await _handlerFunc.Invoke(record);
    }

    /// <summary>
    /// Creates a record handler that uses the provided delegate for record processing.
    /// </summary>
    /// <param name="handlerAction">The delegate to use for record processing.</param>
    /// <returns>The created record handler.</returns>
    public static IRecordHandler<TRecord> From(Action<TRecord> handlerAction)
    {
        return new RecordHandler<TRecord>(async x =>
        {
            handlerAction(x);
            return await Task.FromResult(RecordHandlerResult.None);
        });
    }

    /// <summary>
    /// Creates a record handler that uses the provided async delegate for record processing.
    /// </summary>
    /// <param name="handlerFunc">The async delegate to use for record processing.</param>
    /// <returns>The created record handler.</returns>
    public static IRecordHandler<TRecord> From(Func<TRecord, Task<RecordHandlerResult>> handlerFunc)
    {
        return new RecordHandler<TRecord>(handlerFunc);
    }
}
