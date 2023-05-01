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
    /// Type of batch processor.
    /// </summary>
    public Type BatchProcessor;

    /// <summary>
    /// Type of batch processor provider.
    /// </summary>
    public Type BatchProcessorProvider;

    /// <summary>
    /// Type of record handler.
    /// </summary>
    public Type RecordHandler;

    /// <summary>
    /// Type of record handler provider.
    /// </summary>
    public Type RecordHandlerProvider;

    /// <summary>
    /// Event source (i.e. SQS Queue, DynamoDB Stream or Kinesis Data Stream).
    /// </summary>
    public EventType EventType;

    private static readonly Dictionary<EventType, Type> BatchProcessorTypes = new()
    {
        {EventType.DynamoDbStream,    typeof(IBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>)},
        {EventType.KinesisDataStream, typeof(IBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord>)},
        {EventType.Sqs,               typeof(IBatchProcessor<SQSEvent, SQSEvent.SQSMessage>)}
    };
    private static readonly Dictionary<EventType, Type> BatchProcessorProviderTypes = new()
    {
        {EventType.DynamoDbStream,    typeof(IBatchProcessorProvider<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>)},
        {EventType.KinesisDataStream, typeof(IBatchProcessorProvider<KinesisEvent, KinesisEvent.KinesisEventRecord>)},
        {EventType.Sqs,               typeof(IBatchProcessorProvider<SQSEvent, SQSEvent.SQSMessage>)}
    };
    private static readonly Dictionary<EventType, Type> RecordHandlerTypes = new()
    {
        {EventType.DynamoDbStream,    typeof(IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>)},
        {EventType.KinesisDataStream, typeof(IRecordHandler<KinesisEvent.KinesisEventRecord>)},
        {EventType.Sqs,               typeof(IRecordHandler<SQSEvent.SQSMessage>)}
    };
    private static readonly Dictionary<EventType, Type> RecordHandlerProviderTypes = new()
    {
        {EventType.DynamoDbStream,    typeof(IRecordHandlerProvider<DynamoDBEvent.DynamodbStreamRecord>)},
        {EventType.KinesisDataStream, typeof(IRecordHandlerProvider<KinesisEvent.KinesisEventRecord>)},
        {EventType.Sqs,               typeof(IRecordHandlerProvider<SQSEvent.SQSMessage>)}
    };

    /// <inheritdoc />
    protected override IMethodAspectHandler CreateHandler()
    {
        // Check type of batch processor (optional)
        if (BatchProcessor != null && !BatchProcessor.IsAssignableTo(BatchProcessorTypes[EventType]))
        {
            throw new ArgumentException($"The provided batch processor must implement: '{BatchProcessorTypes[EventType]}'.", nameof(BatchProcessor));
        }

        // Check type of batch processor provider (optional)
        if (BatchProcessorProvider != null && !BatchProcessorProvider.IsAssignableTo(BatchProcessorProviderTypes[EventType]))
        {
            throw new ArgumentException($"The provided batch processor provider must implement: '{BatchProcessorProviderTypes[EventType]}'.", nameof(BatchProcessorProvider));
        }

        // Check type of record handler (conditionally required)
        if (RecordHandler != null && !RecordHandler.IsAssignableTo(RecordHandlerTypes[EventType]))
        {
            throw new ArgumentException($"The provided record handler must implement: '{RecordHandlerTypes[EventType]}'.", nameof(RecordHandler));
        }

        // Check type of record handler provider (conditionally required)
        if (RecordHandlerProvider != null && !RecordHandlerProvider.IsAssignableTo(RecordHandlerProviderTypes[EventType]))
        {
            throw new ArgumentException($"The provided record handler provider must implement: '{RecordHandlerProviderTypes[EventType]}'.", nameof(RecordHandlerProvider));
        }

        // Create aspect handler
        return EventType switch
        {
            EventType.DynamoDbStream => CreateHandlerInternal(() => DynamoDbStreamBatchProcessor.Instance),
            EventType.KinesisDataStream => CreateHandlerInternal(() => KinesisDataStreamBatchProcessor.Instance),
            EventType.Sqs => CreateHandlerInternal(() => SqsBatchProcessor.Instance),
            _ => throw new ArgumentOutOfRangeException(nameof(EventType), EventType, "Unsupported event type.")
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
                throw new InvalidOperationException($"Error during creation of: '{BatchProcessor.Name}'.", ex);
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
                throw new InvalidOperationException($"Error during creation of batch processor using provider: '{BatchProcessorProvider.Name}'.", ex);
            }
        }
        else
        {
            batchProcessor = defaultBatchProcessorProvider.Invoke();
        }

        // Create record handler
        IRecordHandler<TRecord> recordHandler;
        if (RecordHandler != null)
        {
            try
            {
                recordHandler = (IRecordHandler<TRecord>)Activator.CreateInstance(RecordHandler)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error during creation of: '{RecordHandler.Name}'.", ex);
            }
        }
        else if (RecordHandlerProvider != null)
        {
            try
            {
                var recordHandlerProvider = (IRecordHandlerProvider<TRecord>)Activator.CreateInstance(RecordHandlerProvider)!;
                recordHandler = recordHandlerProvider.Create();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error during creation of record handler using provider: '{RecordHandlerProvider.Name}'.", ex);
            }
        }
        else
        {
            throw new InvalidOperationException("A record handler or record handler provider is required.");
        }

        return new BatchProcessingAspectHandler<TEvent, TRecord>(batchProcessor, recordHandler);
    }
}
