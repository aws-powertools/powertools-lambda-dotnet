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

using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

/// <summary>
/// Fluent API builder for configuring and executing SQS batch processing with strongly-typed record handlers.
/// </summary>
public class TypedSqsBatchProcessorBuilder : BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypedSqsBatchProcessorBuilder"/> class.
    /// </summary>
    /// <param name="batchProcessor">The underlying SQS batch processor to use for processing.</param>
    public TypedSqsBatchProcessorBuilder(ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage> batchProcessor)
        : base(batchProcessor)
    {
    }

    /// <summary>
    /// Creates a new TypedSqsBatchProcessorBuilder using the default TypedSqsBatchProcessor instance.
    /// </summary>
    /// <returns>A new TypedSqsBatchProcessorBuilder instance.</returns>
    public static TypedSqsBatchProcessorBuilder Create()
    {
        return new TypedSqsBatchProcessorBuilder(TypedSqsBatchProcessor.TypedInstance);
    }

    /// <summary>
    /// Creates a new TypedSqsBatchProcessorBuilder using the specified TypedSqsBatchProcessor instance.
    /// </summary>
    /// <param name="batchProcessor">The TypedSqsBatchProcessor instance to use.</param>
    /// <returns>A new TypedSqsBatchProcessorBuilder instance.</returns>
    public static TypedSqsBatchProcessorBuilder Create(TypedSqsBatchProcessor batchProcessor)
    {
        return new TypedSqsBatchProcessorBuilder(batchProcessor);
    }
}