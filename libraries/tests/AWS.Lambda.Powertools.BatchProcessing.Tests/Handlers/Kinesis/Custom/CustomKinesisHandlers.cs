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
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis.Custom;

internal class CustomKinesisEventRecordHandler : IKinesisEventRecordHandler
{
    public async Task<RecordHandlerResult> HandleAsync(KinesisEvent.KinesisEventRecord record,
        CancellationToken cancellationToken)
    {
        var product = JsonSerializer.Deserialize<JsonElement>(record.Kinesis.Data);

        if (product.GetProperty("Id").GetInt16() == 4)
        {
            throw new ArgumentException("Error on id 4");
        }

        return await Task.FromResult(RecordHandlerResult.None);
    }
}

internal class CustomFailKinesisEventRecordHandler : IKinesisEventRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(KinesisEvent.KinesisEventRecord record,
        CancellationToken cancellationToken)
    {
        throw new ArgumentException("Raise exception on all!");
    }
}

public class BadCustomKinesisEventRecordHandler
{
}