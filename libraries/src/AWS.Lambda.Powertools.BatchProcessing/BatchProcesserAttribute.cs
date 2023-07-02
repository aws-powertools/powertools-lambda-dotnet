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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using AspectInjector.Broker;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Internal;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.BatchProcessing;

[AttributeUsage(AttributeTargets.Method)]
[Injection(typeof(UniversalWrapperAspect), Inherited = true)]
public class BatchProcesserAttribute : UniversalWrapperAttribute
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
    /// Error handling policy.
    /// </summary>
    public BatchProcessorErrorHandlingPolicy ErrorHandlingPolicy =
        Enum.TryParse(PowertoolsConfigurations.Instance.BatchProcessingErrorHandlingPolicy, true, out BatchProcessorErrorHandlingPolicy errorHandlingPolicy)
            ? errorHandlingPolicy
            : BatchProcessorErrorHandlingPolicy.DeriveFromEvent;

    /// <summary>
    /// The maximum degree of parallelism to apply during batch processing.
    /// </summary>
    public int MaxDegreeOfParallelism =
        PowertoolsConfigurations.Instance.BatchProcessingMaxDegreeOfParallelism;

    private static readonly Dictionary<Type, EventType> EventTypes = new()
    {
        {typeof(DynamoDBEvent), EventType.DynamoDbStream},
        {typeof(KinesisEvent),  EventType.KinesisDataStream},
        {typeof(SQSEvent),      EventType.Sqs}
    };
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
    protected internal override T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
    {
        return WrapAsync(x => Task.FromResult(target(x)), args, eventArgs).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    protected internal override async Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args, AspectEventArgs eventArgs)
    {
        // Create aspect handler
        var handler = CreateAspectHandler(args);

        // Run batch processing logic
        await handler.HandleAsync(args);

        // Run Lambda function logic
        return await target(args);
    }

    private IBatchProcessingAspectHandler CreateAspectHandler(IReadOnlyList<object> args)
    {
        // Try get event type
        if (args == null || args.Count == 0 || !EventTypes.TryGetValue(args[0].GetType(), out var eventType))
        {
            throw new ArgumentException($"The first function handler parameter must be of one of the following types: {string.Join(',', EventTypes.Keys.Select(x => $"'{x.Namespace}'"))}.");
        }

        // Check type of batch processor (optional)
        if (BatchProcessor != null && !BatchProcessor.IsAssignableTo(BatchProcessorTypes[eventType]))
        {
            throw new ArgumentException($"The provided batch processor must implement: '{BatchProcessorTypes[eventType]}'.", nameof(BatchProcessor));
        }

        // Check type of batch processor provider (optional)
        if (BatchProcessorProvider != null && !BatchProcessorProvider.IsAssignableTo(BatchProcessorProviderTypes[eventType]))
        {
            throw new ArgumentException($"The provided batch processor provider must implement: '{BatchProcessorProviderTypes[eventType]}'.", nameof(BatchProcessorProvider));
        }

        // Check type of record handler (conditionally required)
        if (RecordHandler != null && !RecordHandler.IsAssignableTo(RecordHandlerTypes[eventType]))
        {
            throw new ArgumentException($"The provided record handler must implement: '{RecordHandlerTypes[eventType]}'.", nameof(RecordHandler));
        }

        // Check type of record handler provider (conditionally required)
        if (RecordHandlerProvider != null && !RecordHandlerProvider.IsAssignableTo(RecordHandlerProviderTypes[eventType]))
        {
            throw new ArgumentException($"The provided record handler provider must implement: '{RecordHandlerProviderTypes[eventType]}'.", nameof(RecordHandlerProvider));
        }

        // Create aspect handler
        return eventType switch
        {
            EventType.DynamoDbStream => CreateBatchProcessingAspectHandler(() => DynamoDbStreamBatchProcessor.Instance),
            EventType.KinesisDataStream => CreateBatchProcessingAspectHandler(() => KinesisDataStreamBatchProcessor.Instance),
            EventType.Sqs => CreateBatchProcessingAspectHandler(() => SqsBatchProcessor.Instance),
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Unsupported event type.")
        };
    }

    private BatchProcessingAspectHandler<TEvent, TRecord> CreateBatchProcessingAspectHandler<TEvent, TRecord>(Func<IBatchProcessor<TEvent, TRecord>> defaultBatchProcessorProvider)
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

        return new BatchProcessingAspectHandler<TEvent, TRecord>(batchProcessor, recordHandler, new ProcessingOptions
        {
            CancellationToken = CancellationToken.None,
            ErrorHandlingPolicy = ErrorHandlingPolicy,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism
        });
    }
}
