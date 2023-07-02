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
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;

namespace HelloWorld.Kinesis;
internal class CustomKinesisDataStreamRecordHandler : IRecordHandler<KinesisEvent.KinesisEventRecord>
{
    public async Task HandleAsync(KinesisEvent.KinesisEventRecord record, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Handling Kinesis record with sequence number: '{record.Kinesis.SequenceNumber}'.");
        await Task.CompletedTask;
    }
}
