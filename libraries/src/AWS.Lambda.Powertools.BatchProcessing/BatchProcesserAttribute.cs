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
using System.Collections.Generic;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Internal;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.BatchProcessing;

[AttributeUsage(AttributeTargets.Method)]
public class BatchProcesserAttribute : MethodAspectAttribute
{
    /// <summary>
    /// SQS batch processor.
    /// </summary>
    public Type BatchProcessor;

    /// <summary>
    /// SQS batch processor provider.
    /// </summary>
    public Type BatchProcessorProvider;

    /// <summary>
    /// SQS record handler.
    /// </summary>
    public Type RecordHandler;

    /// <summary>
    /// Event source type.
    /// </summary>
    public EventType EventType;

    private static readonly Dictionary<EventType, Type> BatchProcessorTypes = new()
    {
        {EventType.DynamoDbStreams,    typeof(IBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>)},
        {EventType.KinesisDataStreams, typeof(IBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord>)},
        {EventType.Sqs,                typeof(IBatchProcessor<SQSEvent, SQSEvent.SQSMessage>)}
    };
    private static readonly Dictionary<EventType, Type> BatchProcessorProviderTypes = new()
    {
        {EventType.DynamoDbStreams,    typeof(IBatchProcessorProvider<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>)},
        {EventType.KinesisDataStreams, typeof(IBatchProcessorProvider<KinesisEvent, KinesisEvent.KinesisEventRecord>)},
        {EventType.Sqs,                typeof(IBatchProcessorProvider<SQSEvent, SQSEvent.SQSMessage>)}
    };
    private static readonly Dictionary<EventType, Type> RecordHandlerTypes = new()
    {
        {EventType.DynamoDbStreams,    typeof(IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>)},
        {EventType.KinesisDataStreams, typeof(IRecordHandler<KinesisEvent.KinesisEventRecord>)},
        {EventType.Sqs,                typeof(IRecordHandler<SQSEvent.SQSMessage>)}
    };

    /// <inheritdoc />
    protected override IMethodAspectHandler CreateHandler()
    {
        // Check type of batch processor (optional)
        if (BatchProcessor != null && !BatchProcessor.IsAssignableTo(BatchProcessorTypes[EventType]))
        {
            throw new ArgumentException($"The SQS batch processor must implement {BatchProcessorTypes[EventType]}.", nameof(BatchProcessor));
        }

        // Check type of batch processor provider (optional)
        if (BatchProcessorProvider != null && !BatchProcessorProvider.IsAssignableTo(BatchProcessorProviderTypes[EventType]))
        {
            throw new ArgumentException($"The SQS batch processor provider must implement {BatchProcessorProviderTypes[EventType]}.", nameof(BatchProcessorProvider));
        }

        // Check type of record handler (required)
        if (!RecordHandler.IsAssignableTo(RecordHandlerTypes[EventType]))
        {
            throw new ArgumentException($"The SQS record handler is required and must implement {RecordHandlerTypes[EventType]}.", nameof(RecordHandler));
        }

        // Create aspect handler
        return EventType switch
        {
            EventType.DynamoDbStreams => CreateHandlerInternal(() => DynamoDbStreamBatchProcessor.Instance),
            EventType.KinesisDataStreams => CreateHandlerInternal(() => KinesisDataStreamBatchProcessor.Instance),
            EventType.Sqs => CreateHandlerInternal(() => SqsBatchProcessor.Instance),
            _ => throw new ArgumentOutOfRangeException(nameof(EventType))
        };
    }

    private IMethodAspectHandler CreateHandlerInternal<TEvent, TRecord>(Func<IBatchProcessor<TEvent, TRecord>> defaultBatchProcessorProvider)
    {
        // Create batch processor
        IBatchProcessor<TEvent, TRecord> batchProcessor;
        if (BatchProcessor != null)
        {
            try
            {
                batchProcessor = (IBatchProcessor<TEvent, TRecord>)Activator.CreateInstance(BatchProcessor)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not create instance of {BatchProcessor.Name}", ex);
            }
        }
        else if (BatchProcessorProvider != null)
        {
            try
            {
                var batchProcessorProvider = (IBatchProcessorProvider<TEvent, TRecord>)Activator.CreateInstance(BatchProcessorProvider)!;
                batchProcessor = batchProcessorProvider.Create();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not create instance of {BatchProcessorProvider.Name}", ex);
            }
        }
        else
        {
            batchProcessor = defaultBatchProcessorProvider.Invoke();
        }

        // Create record handler
        IRecordHandler<TRecord> recordHandler;
        try
        {
            recordHandler = (IRecordHandler<TRecord>)Activator.CreateInstance(RecordHandler)!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not create instance of {RecordHandler.Name}", ex);
        }

        return new BatchProcessingAspectHandler<TEvent, TRecord>(batchProcessor, recordHandler);
    }
}
