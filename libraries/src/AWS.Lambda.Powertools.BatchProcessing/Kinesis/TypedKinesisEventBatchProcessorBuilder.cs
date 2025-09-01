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

using Amazon.Lambda.KinesisEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Kinesis;

/// <summary>
/// Fluent API builder for configuring and executing Kinesis batch processing with strongly-typed record handlers.
/// </summary>
public class TypedKinesisEventBatchProcessorBuilder : BatchProcessorBuilder<KinesisEvent, KinesisEvent.KinesisEventRecord>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypedKinesisEventBatchProcessorBuilder"/> class.
    /// </summary>
    /// <param name="batchProcessor">The underlying Kinesis batch processor to use for processing.</param>
    public TypedKinesisEventBatchProcessorBuilder(ITypedBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord> batchProcessor)
        : base(batchProcessor)
    {
    }

    /// <summary>
    /// Creates a new TypedKinesisEventBatchProcessorBuilder using the default TypedKinesisEventBatchProcessor instance.
    /// </summary>
    /// <returns>A new TypedKinesisEventBatchProcessorBuilder instance.</returns>
    public static TypedKinesisEventBatchProcessorBuilder Create()
    {
        return new TypedKinesisEventBatchProcessorBuilder(TypedKinesisEventBatchProcessor.TypedInstance);
    }

    /// <summary>
    /// Creates a new TypedKinesisEventBatchProcessorBuilder using the specified TypedKinesisEventBatchProcessor instance.
    /// </summary>
    /// <param name="batchProcessor">The TypedKinesisEventBatchProcessor instance to use.</param>
    /// <returns>A new TypedKinesisEventBatchProcessorBuilder instance.</returns>
    public static TypedKinesisEventBatchProcessorBuilder Create(TypedKinesisEventBatchProcessor batchProcessor)
    {
        return new TypedKinesisEventBatchProcessorBuilder(batchProcessor);
    }
}