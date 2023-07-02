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
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;

namespace HelloWorld.Sqs;
public class CustomSqsRecordHandler : IRecordHandler<SQSEvent.SQSMessage>
{
    public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        /*
         * Your business logic.
         * If an exception is thrown, the item will be marked as a partial batch item failure.
         */
        Logger.LogInformation($"Handling SQS record with message id: '{record.MessageId}'.");
        Logger.LogInformation("Doing async operation...");
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        Logger.LogInformation("Async operation complete.");
        return await Task.FromResult(RecordHandlerResult.None);
    }
}
