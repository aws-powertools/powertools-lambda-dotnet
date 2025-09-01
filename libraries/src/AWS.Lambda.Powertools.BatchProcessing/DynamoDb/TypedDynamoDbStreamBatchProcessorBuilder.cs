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

using Amazon.Lambda.DynamoDBEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.DynamoDb;

/// <summary>
/// Fluent API builder for configuring and executing DynamoDB stream batch processing with strongly-typed record handlers.
/// </summary>
public class TypedDynamoDbStreamBatchProcessorBuilder : BatchProcessorBuilder<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypedDynamoDbStreamBatchProcessorBuilder"/> class.
    /// </summary>
    /// <param name="batchProcessor">The underlying DynamoDB stream batch processor to use for processing.</param>
    public TypedDynamoDbStreamBatchProcessorBuilder(ITypedBatchProcessor<DynamoDBEvent, DynamoDBEvent.DynamodbStreamRecord> batchProcessor)
        : base(batchProcessor)
    {
    }

    /// <summary>
    /// Creates a new TypedDynamoDbStreamBatchProcessorBuilder using the default TypedDynamoDbStreamBatchProcessor instance.
    /// </summary>
    /// <returns>A new TypedDynamoDbStreamBatchProcessorBuilder instance.</returns>
    public static TypedDynamoDbStreamBatchProcessorBuilder Create()
    {
        return new TypedDynamoDbStreamBatchProcessorBuilder(TypedDynamoDbStreamBatchProcessor.TypedInstance);
    }

    /// <summary>
    /// Creates a new TypedDynamoDbStreamBatchProcessorBuilder using the specified TypedDynamoDbStreamBatchProcessor instance.
    /// </summary>
    /// <param name="batchProcessor">The TypedDynamoDbStreamBatchProcessor instance to use.</param>
    /// <returns>A new TypedDynamoDbStreamBatchProcessorBuilder instance.</returns>
    public static TypedDynamoDbStreamBatchProcessorBuilder Create(TypedDynamoDbStreamBatchProcessor batchProcessor)
    {
        return new TypedDynamoDbStreamBatchProcessorBuilder(batchProcessor);
    }
}