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

    [Logging(LogEvent = true)]
    [BatchProcessor(RecordHandler = typeof(CustomDynamoDbStreamRecordHandler))]
    public BatchItemFailuresResponse DynamoDbStreamHandlerUsingAttribute(DynamoDBEvent _)
    {
        return DynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }

    [Logging(LogEvent = true)]
    [BatchProcessor(RecordHandler = typeof(CustomKinesisEventRecordHandler))]
    public BatchItemFailuresResponse KinesisEventHandlerUsingAttribute(KinesisEvent _)
    {
        return KinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }

    [Logging(LogEvent = true)]
    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler))]
    public BatchItemFailuresResponse SqsHandlerUsingAttribute(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }

    [Logging(LogEvent = true)]
    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler), ErrorHandlingPolicy = BatchProcessorErrorHandlingPolicy.StopOnFirstBatchItemFailure)]
    public BatchItemFailuresResponse SqsHandlerUsingAttributeWithErrorPolicy(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }

    #region More example handlers...

    [Logging(LogEvent = true)]
    [BatchProcessor(RecordHandlerProvider = typeof(CustomSqsRecordHandlerProvider), BatchProcessor = typeof(CustomSqsBatchProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomRecordHandlerProvider(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }

    [Logging(LogEvent = true)]
    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler), BatchProcessor = typeof(CustomSqsBatchProcessor))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessor(SQSEvent _)
    {
        return SqsBatchProcessor.Result.BatchItemFailuresResponse;
    }

    [Logging(LogEvent = true)]
    [BatchProcessor(RecordHandler = typeof(CustomSqsRecordHandler), BatchProcessorProvider = typeof(CustomSqsBatchProcessorProvider))]
    public BatchItemFailuresResponse HandlerUsingAttributeAndCustomBatchProcessorProvider(SQSEvent _)
    {
        var batchProcessor = Services.Provider.GetRequiredService<ISqsBatchProcessor>();
        return batchProcessor.ProcessingResult.BatchItemFailuresResponse;
    }

    [Logging(LogEvent = true)]
    public async Task<BatchItemFailuresResponse> HandlerUsingUtility(SQSEvent sqsEvent)
    {
        var result = await SqsBatchProcessor.Instance.ProcessAsync(sqsEvent, RecordHandler<SQSEvent.SQSMessage>.From(x =>
        {
            Logger.LogInformation($"Inline handling of SQS message with body: '{x.Body}'.");
        }));
        return result.BatchItemFailuresResponse;
    }

    [Logging(LogEvent = true)]
    public async Task<BatchItemFailuresResponse> HandlerUsingUtilityFromIoc(SQSEvent sqsEvent)
    {
        var batchProcessor = Services.Provider.GetRequiredService<ISqsBatchProcessor>();
        var recordHandler = Services.Provider.GetRequiredService<ISqsRecordHandler>();
        var result = await batchProcessor.ProcessAsync(sqsEvent, recordHandler);
        return result.BatchItemFailuresResponse;
    }

    #endregion
}
