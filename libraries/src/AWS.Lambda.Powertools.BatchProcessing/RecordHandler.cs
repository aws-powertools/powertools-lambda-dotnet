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
public class RecordHandler<TRecord> : IRecordHandler<TRecord>
{
    private readonly Func<TRecord, Task> _handlerFunc;

    private RecordHandler(Func<TRecord, Task> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    public async Task HandleAsync(TRecord record, CancellationToken cancellationToken)
    {
        await _handlerFunc.Invoke(record);
    }

    public static IRecordHandler<TRecord> From(Action<TRecord> handlerAction)
    {
        return new RecordHandler<TRecord>(async x =>
        {
            handlerAction(x);
            await Task.CompletedTask;
        });
    }

    public static IRecordHandler<TRecord> From(Func<TRecord, Task> handlerFunc)
    {
        return new RecordHandler<TRecord>(handlerFunc);
    }
}
