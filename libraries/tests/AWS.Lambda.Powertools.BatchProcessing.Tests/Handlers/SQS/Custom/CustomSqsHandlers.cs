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
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Custom;

public class CustomSqsRecordHandler : ISqsRecordHandler
{
    public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        var product = JsonSerializer.Deserialize<JsonElement>(record.Body);
        
        if (product.GetProperty("Id").GetInt16() == 4)
        {
            throw new ArgumentException("Error on 4");
        }
        
        return await Task.FromResult(RecordHandlerResult.None);
    }
}

public class CustomFailSqsRecordHandler : ISqsRecordHandler
{
    public Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        throw new ArgumentException("Raise exception on all!");
    }
}

public class BadCustomSqsRecordHandler
{
}