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

using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using HelloWorld.DynamoDb;
using HelloWorld.Kinesis;
using HelloWorld.Sqs;
using Microsoft.Extensions.DependencyInjection;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace HelloWorld;

public class Function
{
    static Function()
    {
        Logger.LogInformation("Initializing IoC container using the static constructor...");
        Services.Init();
    }

    [BatchProcesser(EventType = EventType.DynamoDbStream, RecordHandler = typeof(CustomDynamoDbStreamRecordHandler))]
    [Logging(LogEvent = true)]
    public BatchResponse DynamoDbStreamHandlerUsingAttribute(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Instance.BatchResponse;
    }

    [BatchProcesser(EventType = EventType.KinesisDataStream, RecordHandler = typeof(CustomKinesisDataStreamRecordHandler))]
    [Logging(LogEvent = true)]
    public BatchResponse KinesisDataStreamHandlerUsingAttribute(KinesisEvent _)
    {
        return KinesisDataStreamBatchProcessor.Instance.BatchResponse;
    }

    [BatchProcesser(EventType = EventType.Sqs, RecordHandler = typeof(CustomSqsRecordHandler))]
    [Logging(LogEvent = true)]
    public BatchResponse SqsHandlerUsingAttribute(SQSEvent _)
    {
        return SqsBatchProcessor.Instance.BatchResponse;
    }

    #region More example handlers...

    [BatchProcesser(EventType = EventType.Sqs, RecordHandlerProvider = typeof(CustomSqsRecordHandlerProvider), BatchProcessor = typeof(CustomSqsBatchProcessor))]
    [Logging(LogEvent = true)]
    public BatchResponse HandlerUsingAttributeAndCustomRecordHandlerProvider(SQSEvent _)
    {
        return SqsBatchProcessor.Instance.BatchResponse;
    }

    [BatchProcesser(EventType = EventType.Sqs, RecordHandler = typeof(CustomSqsRecordHandler), BatchProcessor = typeof(CustomSqsBatchProcessor))]
    [Logging(LogEvent = true)]
    public BatchResponse HandlerUsingAttributeAndCustomBatchProcessor(SQSEvent _)
    {
        return SqsBatchProcessor.Instance.BatchResponse;
    }

    [BatchProcesser(EventType = EventType.Sqs, RecordHandler = typeof(CustomSqsRecordHandler), BatchProcessorProvider = typeof(CustomSqsBatchProcessorProvider))]
    [Logging(LogEvent = true)]
    public BatchResponse HandlerUsingAttributeAndCustomBatchProcessorProvider(SQSEvent _)
    {
        var batchProcessor = Services.Provider.GetRequiredService<CustomSqsBatchProcessor>();
        return batchProcessor.BatchResponse;
    }

    [Logging(LogEvent = true)]
    public async Task<BatchResponse> HandlerUsingUtility(SQSEvent sqsEvent)
    {
        return await SqsBatchProcessor.Instance.ProcessAsync(sqsEvent, RecordHandler<SQSEvent.SQSMessage>.From(x =>
        {
            Logger.LogInformation($"Inline handling of SQS message with body: '{x.Body}'.");
        }));
    }

    [Logging(LogEvent = true)]
    public async Task<BatchResponse> HandlerUsingUtilityFromIoc(SQSEvent sqsEvent)
    {
        var batchProcessor = Services.Provider.GetRequiredService<CustomSqsBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<CustomSqsRecordHandler>();
        return await batchProcessor.ProcessAsync(sqsEvent, recordHandler);
    }

    #endregion
}
