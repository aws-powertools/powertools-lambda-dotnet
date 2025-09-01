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
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using HelloWorld.Data;
using HelloWorld.TypedHandlers;

namespace HelloWorld;

/// <summary>
/// Examples of using typed batch processing with automatic deserialization
/// </summary>
public class TypedFunction
{
    #region Attribute-based Examples

    /// <summary>
    /// Example using BatchProcessor attribute with typed record handler
    /// </summary>
    [Logging(LogEvent = true)]
    [BatchProcessor(TypedRecordHandler = typeof(TypedSqsRecordHandler))]
    public BatchItemFailuresResponse SqsTypedHandlerUsingAttribute(SQSEvent _)
    {
        return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
    }

    /// <summary>
    /// Example using BatchProcessor attribute with typed record handler that needs context
    /// </summary>
    [Logging(LogEvent = true)]
    [BatchProcessor(TypedRecordHandler = typeof(TypedSqsRecordHandlerWithContext))]
    public BatchItemFailuresResponse SqsTypedHandlerWithContextUsingAttribute(SQSEvent _, ILambdaContext context)
    {
        return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
    }

    /// <summary>
    /// Example using BatchProcessor attribute with AOT JsonSerializerContext
    /// </summary>
    [Logging(LogEvent = true)]
    [BatchProcessor(
        TypedRecordHandler = typeof(TypedSqsRecordHandler),
        JsonSerializerContext = typeof(ExampleJsonSerializerContext))]
    public BatchItemFailuresResponse SqsTypedHandlerWithAotUsingAttribute(SQSEvent _)
    {
        return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
    }

    /// <summary>
    /// Example using BatchProcessor attribute for Kinesis with typed handler
    /// </summary>
    [Logging(LogEvent = true)]
    [BatchProcessor(TypedRecordHandler = typeof(TypedKinesisRecordHandler))]
    public BatchItemFailuresResponse KinesisTypedHandlerUsingAttribute(KinesisEvent _)
    {
        return TypedKinesisEventBatchProcessor.Result.BatchItemFailuresResponse;
    }

    /// <summary>
    /// Example using BatchProcessor attribute for DynamoDB with typed handler
    /// </summary>
    [Logging(LogEvent = true)]
    [BatchProcessor(TypedRecordHandler = typeof(TypedDynamoDbRecordHandler))]
    public BatchItemFailuresResponse DynamoDbTypedHandlerUsingAttribute(DynamoDBEvent _)
    {
        return TypedDynamoDbStreamBatchProcessor.Result.BatchItemFailuresResponse;
    }

    #endregion

    #region Fluent API Examples

    /// <summary>
    /// Example using fluent API with inline typed handler
    /// </summary>
    [Logging(LogEvent = true)]
    public async Task<BatchItemFailuresResponse> SqsTypedHandlerUsingFluentApi(SQSEvent sqsEvent, ILambdaContext context)
    {
        var result = await TypedSqsBatchProcessor.Instance
            .Handler<Product>((product, ct) =>
            {
                Logger.LogInformation($"Processing product {product.Id} - {product.Name} (${product.Price})");
                
                if (product.Price > 1000)
                {
                    throw new ArgumentException("Product price too high");
                }
                
                return Task.FromResult(RecordHandlerResult.None);
            })
            .ProcessAsync(sqsEvent, context);

        return result.BatchItemFailuresResponse;
    }

    /// <summary>
    /// Example using fluent API with context injection
    /// </summary>
    [Logging(LogEvent = true)]
    public async Task<BatchItemFailuresResponse> SqsTypedHandlerWithContextUsingFluentApi(SQSEvent sqsEvent, ILambdaContext context)
    {
        var result = await TypedSqsBatchProcessor.Instance
            .Handler<Product>((product, lambdaContext, ct) =>
            {
                Logger.LogInformation($"Processing product {product.Id} in request {lambdaContext.AwsRequestId}");
                Logger.LogInformation($"Remaining time: {lambdaContext.RemainingTime.TotalSeconds}s");
                
                return Task.FromResult(RecordHandlerResult.None);
            })
            .ProcessAsync(sqsEvent, context);

        return result.BatchItemFailuresResponse;
    }

    /// <summary>
    /// Example using fluent API with AOT JsonSerializerContext
    /// </summary>
    [Logging(LogEvent = true)]
    public async Task<BatchItemFailuresResponse> SqsTypedHandlerWithAotUsingFluentApi(SQSEvent sqsEvent, ILambdaContext context)
    {
        var result = await TypedSqsBatchProcessor.Instance
            .WithJsonSerializerContext(ExampleJsonSerializerContext.Default)
            .Handler<Product>((product, ct) =>
            {
                Logger.LogInformation($"AOT processing product {product.Id} - {product.Name}");
                return Task.FromResult(RecordHandlerResult.None);
            })
            .ProcessAsync(sqsEvent, context);

        return result.BatchItemFailuresResponse;
    }

    /// <summary>
    /// Example using fluent API for Kinesis events
    /// </summary>
    [Logging(LogEvent = true)]
    public async Task<BatchItemFailuresResponse> KinesisTypedHandlerUsingFluentApi(KinesisEvent kinesisEvent, ILambdaContext context)
    {
        var result = await TypedKinesisEventBatchProcessor.Instance
            .Handler<Order>((order, ct) =>
            {
                Logger.LogInformation($"Processing order {order.OrderId} with {order.Items.Count} items");
                
                if (order.TotalAmount <= 0)
                {
                    throw new ArgumentException("Invalid order total");
                }
                
                return Task.FromResult(RecordHandlerResult.None);
            })
            .ProcessAsync(kinesisEvent, context);

        return result.BatchItemFailuresResponse;
    }

    /// <summary>
    /// Example using fluent API for DynamoDB stream events
    /// </summary>
    [Logging(LogEvent = true)]
    public async Task<BatchItemFailuresResponse> DynamoDbTypedHandlerUsingFluentApi(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
    {
        var result = await TypedDynamoDbStreamBatchProcessor.Instance
            .Handler<Customer>((customer, ct) =>
            {
                Logger.LogInformation($"Processing customer change: {customer.CustomerId} - {customer.Name}");
                
                if (string.IsNullOrEmpty(customer.Email))
                {
                    throw new ArgumentException("Customer email is required");
                }
                
                return Task.FromResult(RecordHandlerResult.None);
            })
            .ProcessAsync(dynamoDbEvent, context);

        return result.BatchItemFailuresResponse;
    }

    #endregion

    #region Advanced Examples

    /// <summary>
    /// Example with multiple handler types and error handling
    /// </summary>
    [Logging(LogEvent = true)]
    public async Task<BatchItemFailuresResponse> SqsAdvancedTypedHandlerExample(SQSEvent sqsEvent, ILambdaContext context)
    {
        var result = await TypedSqsBatchProcessor.Instance
            .WithJsonSerializerContext(ExampleJsonSerializerContext.Default)
            .IgnoreDeserializationErrors(false) // Fail on deserialization errors
            .Handler<Product>((product, lambdaContext, ct) =>
            {
                Logger.LogInformation($"Processing product {product.Id}");
                
                // Simulate different processing based on product type
                if (product.Price > 500)
                {
                    Logger.LogInformation("High-value product processing");
                }
                
                return Task.FromResult(RecordHandlerResult.None);
            })
            .ProcessAsync(sqsEvent, context);

        return result.BatchItemFailuresResponse;
    }

    /// <summary>
    /// Example showing migration from traditional to typed handler
    /// </summary>
    [Logging(LogEvent = true)]
    public async Task<BatchItemFailuresResponse> MigrationExample(SQSEvent sqsEvent, ILambdaContext context)
    {
        // New typed approach (with automatic deserialization)
        var result = await TypedSqsBatchProcessor.Instance
            .Handler<Product>((product, ct) =>
            {
                Logger.LogInformation($"Processing product {product.Id}");
                return Task.FromResult(RecordHandlerResult.None);
            })
            .ProcessAsync(sqsEvent, context);

        return result.BatchItemFailuresResponse;
    }

    #endregion
}