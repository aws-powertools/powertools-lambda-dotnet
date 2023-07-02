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
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;

namespace HelloWorld.Sqs;
internal class CustomSqsBatchProcessor : SqsBatchProcessor
{
    public override Task<BatchResponse> ProcessAsync(SQSEvent @event, IRecordHandler<SQSEvent.SQSMessage> recordHandler, ProcessingOptions processingOptions)
    {
        Logger.LogInformation($"Processing {@event.Records.Count} record(s) using: '{GetType().Name}'.");
        return base.ProcessAsync(@event, recordHandler, processingOptions);
    }

    protected override async Task HandleRecordFailureAsync(SQSEvent.SQSMessage record, Exception exception)
    {
        Logger.LogWarning(exception, $"Failed to process record: '{record.MessageId}'.");
        await base.HandleRecordFailureAsync(record, exception);
    }
}
