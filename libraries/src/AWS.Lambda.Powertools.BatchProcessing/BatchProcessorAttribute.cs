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
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Internal;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
///     Enables batch processing with support for reporting partial batch item failures.              <br/>
///                                                                                                   <br/>
///     Key features                                                                                  <br/>
///     ---------------------                                                                         <br/>
///     <list type="bullet">
///         <item>
///             <description>Automatically reports partial failures when processing batch items from Amazon SQS Queues, Amazon Kinesis Data Streams and Amazon DynamoDB Streams.</description>
///         </item>
///         <item>
///             <description>Batch items are processed in isolation. One item failing processing will not cause the Lambda function to immediately fail.</description>
///         </item>
///         <item>
///             <description>Ease of use (simple to setup and configure).</description>
///         </item>
///         <item>
///             <description>Extensible by design. Use batch processing hooks to add customized functionality (i.e. publish custom metrics).</description>
///         </item>
///         <item>
///             <description>Support for enabling and configuring parallel processing of batch items.</description>
///         </item>
///     </list>
///
///     Example                                                                                       <br/>
///     ---------------------                                                                         <br/>
///     <code>
///         [BatchProcesser(RecordHandler = typeof(CustomSqsRecordHandler))]
///         public BatchItemFailuresResponse SqsHandlerUsingAttribute(SQSEvent _)
///         {
///             return SqsBatchProcessor.Instance.ProcessingResult.BatchItemFailuresResponse;
///         }
///
///         public class CustomSqsRecordHandler : IRecordHandler&lt;SQSEvent.SQSMessage&gt;
///         {
///             public async Task&lt;RecordHandlerResult&gt; HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
///             {
///                 /*
///                  * Your business logic.
///                  * If an exception is thrown, the item will be marked as a partial batch item failure.
///                  */
///                 return await Task.FromResult(RecordHandlerResult.None);
///             }
///         }
///     </code>
///                                                                                                   <br/>
///     Environment variables                                                                         <br/>
///     ---------------------                                                                         <br/>
///     <list type="table">
///         <listheader>
///           <term>Variable name</term>
///           <description>Description</description>
///         </listheader>
///         <item>
///             <term>POWERTOOLS_BATCH_PROCESSING_ERROR_HANDLING_POLICY</term>
///             <description>string, the error handling policy to apply (i.e. continue or stop on first batch item failure).</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_BATCH_PROCESSING_MAX_DEGREE_OF_PARALLELISM</term>
///             <description>int, defaults to 1 (no parallelism). Specify -1 to automatically use the value of <see cref="System.Environment.ProcessorCount">ProcessorCount</see>.</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_BATCH_THROW_ON_FULL_BATCH_FAILURE</term>
///             <description>bool, defaults to true. Controls if an exception is thrown on full batch failure.</description>
///         </item>
///     </list>
///                                                                                                   <br/>
///     Parameters                                                                                    <br/>
///     -----------                                                                                   <br/>
///     <list type="table">
///         <listheader>
///           <term>Parameter name</term>
///           <description>Description</description>
///         </listheader>
///         <item>
///             <term>BatchProcessor</term>
///             <description>Type, (optional) set the BatchProcessor to use.</description>
///         </item>
///         <item>
///             <term>BatchProcessorProvider</term>
///             <description>Type, (optional) set the BatchProcessor provider / factory to use (useful with dependency injection).</description>
///         </item>
///         <item>
///             <term>RecordHandler</term>
///             <description>Type, (required, conditional) set the record handler to use for batch item processing.</description>
///         </item>
///         <item>
///             <term>RecordHandlerProvider</term>
///             <description>Type, (required, conditional) set the record handler provider / factory to use (useful with dependency injection).</description>
///         </item>
///         <item>
///             <term>ErrorHandlingPolicy</term>
///             <description>string, the error handling policy to apply (i.e. continue or stop on first batch item failure).</description>
///         </item>
///         <item>
///             <term>MaxDegreeOfParallelism</term>
///             <description>int, defaults to 1 (no parallelism). Specify -1 to automatically use the value of <see cref="System.Environment.ProcessorCount">ProcessorCount</see>.</description>
///         </item>
///         <item>
///             <term>ThrowOnFullBatchFailure</term>
///             <description>bool, defaults to true. Controls if an exception is thrown on full batch failure.</description>
///         </item>
///     </list>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[Injection(typeof(UniversalWrapperAspect), Inherited = true)]
public class BatchProcessorAttribute : UniversalWrapperAttribute
{
    /// <summary>
    /// Type of batch processor.
    /// </summary>
    public Type BatchProcessor { get; set; }

    /// <summary>
    /// Type of batch processor provider.
    /// </summary>
    public Type BatchProcessorProvider { get; set; }

    /// <summary>
    /// Type of record handler.
    /// </summary>
    public Type RecordHandler { get; set; }

    /// <summary>
    /// Type of record handler provider.
    /// </summary>
    public Type RecordHandlerProvider { get; set; }

    /// <summary>
    /// Error handling policy.
    /// </summary>
    public BatchProcessorErrorHandlingPolicy ErrorHandlingPolicy { get; set; }

    /// <summary>
    /// Batch processing enabled (default false)
    /// </summary>
    public bool BatchParallelProcessingEnabled { get; set; } = PowertoolsConfigurations.Instance.BatchParallelProcessingEnabled;

    /// <summary>
    /// The maximum degree of parallelism to apply during batch processing.
    /// Must enable BatchParallelProcessingEnabled
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = PowertoolsConfigurations.Instance.BatchProcessingMaxDegreeOfParallelism;

    /// <summary>
    /// By default, the Batch processor throws a <see cref="BatchProcessingException"/> on full batch failure.
    /// This behaviour can be disabled by setting this value to false.
    /// </summary>
    public bool ThrowOnFullBatchFailure { get; set; } = PowertoolsConfigurations.Instance.BatchThrowOnFullBatchFailureEnabled;

    private static readonly Dictionary<Type, BatchEventType> EventTypes = new()
    {
        {typeof(DynamoDBEvent), BatchEventType.DynamoDbStream},
        {typeof(KinesisEvent),  BatchEventType.KinesisDataStream},
        {typeof(SQSEvent),      BatchEventType.Sqs}
    };
    private static readonly Dictionary<BatchEventType, Type> BatchProcessorTypes = new()
    {
        {BatchEventType.DynamoDbStream,    typeof(IBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>)},
        {BatchEventType.KinesisDataStream, typeof(IBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord>)},
        {BatchEventType.Sqs,               typeof(IBatchProcessor<SQSEvent, SQSEvent.SQSMessage>)}
    };
    private static readonly Dictionary<BatchEventType, Type> BatchProcessorProviderTypes = new()
    {
        {BatchEventType.DynamoDbStream,    typeof(IBatchProcessorProvider<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>)},
        {BatchEventType.KinesisDataStream, typeof(IBatchProcessorProvider<KinesisEvent, KinesisEvent.KinesisEventRecord>)},
        {BatchEventType.Sqs,               typeof(IBatchProcessorProvider<SQSEvent, SQSEvent.SQSMessage>)}
    };
    private static readonly Dictionary<BatchEventType, Type> RecordHandlerTypes = new()
    {
        {BatchEventType.DynamoDbStream,    typeof(IRecordHandler<DynamoDBEvent.DynamodbStreamRecord>)},
        {BatchEventType.KinesisDataStream, typeof(IRecordHandler<KinesisEvent.KinesisEventRecord>)},
        {BatchEventType.Sqs,               typeof(IRecordHandler<SQSEvent.SQSMessage>)}
    };
    private static readonly Dictionary<BatchEventType, Type> RecordHandlerProviderTypes = new()
    {
        {BatchEventType.DynamoDbStream,    typeof(IRecordHandlerProvider<DynamoDBEvent.DynamodbStreamRecord>)},
        {BatchEventType.KinesisDataStream, typeof(IRecordHandlerProvider<KinesisEvent.KinesisEventRecord>)},
        {BatchEventType.Sqs,               typeof(IRecordHandlerProvider<SQSEvent.SQSMessage>)}
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
            BatchEventType.DynamoDbStream => CreateBatchProcessingAspectHandler(() => DynamoDbStreamBatchProcessor.Instance),
            BatchEventType.KinesisDataStream => CreateBatchProcessingAspectHandler(() => KinesisEventBatchProcessor.Instance),
            BatchEventType.Sqs => CreateBatchProcessingAspectHandler(() => SqsBatchProcessor.Instance),
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

        var errorHandlingPolicy = Enum.TryParse(PowertoolsConfigurations.Instance.BatchProcessingErrorHandlingPolicy, true, out BatchProcessorErrorHandlingPolicy errHandlingPolicy)
            ? errHandlingPolicy
            : ErrorHandlingPolicy;
        if (ErrorHandlingPolicy != BatchProcessorErrorHandlingPolicy.DeriveFromEvent)
        {
            errorHandlingPolicy = ErrorHandlingPolicy;
        }

        return new BatchProcessingAspectHandler<TEvent, TRecord>(batchProcessor, recordHandler, new ProcessingOptions
        {
            CancellationToken = CancellationToken.None,
            ErrorHandlingPolicy = errorHandlingPolicy,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            BatchParallelProcessingEnabled = BatchParallelProcessingEnabled,
            ThrowOnFullBatchFailure = ThrowOnFullBatchFailure
        });
    }
}
