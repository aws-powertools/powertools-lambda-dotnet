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
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Tests for ITypedBatchProcessor interface contracts.
/// </summary>
public class ITypedBatchProcessorTests
{
    /// <summary>
    /// Test data class for testing typed record handlers.
    /// </summary>
    public class TestData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
    }

    /// <summary>
    /// Mock implementation of ITypedBatchProcessor for testing interface contracts.
    /// </summary>
    public class MockTypedBatchProcessor : ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage>
    {
        public ProcessingResult<SQSEvent.SQSMessage> ProcessingResult { get; private set; }

        public MockTypedBatchProcessor()
        {
            ProcessingResult = new ProcessingResult<SQSEvent.SQSMessage>();
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler, DeserializationOptions deserializationOptions)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler, DeserializationOptions deserializationOptions, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandler<T> recordHandler, DeserializationOptions deserializationOptions, ProcessingOptions processingOptions)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, DeserializationOptions deserializationOptions)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, DeserializationOptions deserializationOptions, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessingResult);
        }

        public Task<ProcessingResult<SQSEvent.SQSMessage>> ProcessAsync<T>(SQSEvent @event, ITypedRecordHandlerWithContext<T> recordHandler, ILambdaContext context, DeserializationOptions deserializationOptions, ProcessingOptions processingOptions)
        {
            return Task.FromResult(ProcessingResult);
        }
    }

    /// <summary>
    /// Mock typed record handler for testing.
    /// </summary>
    public class MockTypedRecordHandler : ITypedRecordHandler<TestData>
    {
        public Task<RecordHandlerResult> HandleAsync(TestData data, CancellationToken cancellationToken)
        {
            return Task.FromResult(RecordHandlerResult.FromData($"Processed: {data?.Name}"));
        }
    }

    /// <summary>
    /// Mock typed record handler with context for testing.
    /// </summary>
    public class MockTypedRecordHandlerWithContext : ITypedRecordHandlerWithContext<TestData>
    {
        public Task<RecordHandlerResult> HandleAsync(TestData data, ILambdaContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(RecordHandlerResult.FromData($"Processed: {data?.Name} with context"));
        }
    }

    /// <summary>
    /// Mock Lambda context for testing.
    /// </summary>
    public class MockLambdaContext : ILambdaContext
    {
        public string AwsRequestId { get; set; } = "test-request-id";
        public IClientContext ClientContext { get; set; }
        public string FunctionName { get; set; } = "test-function";
        public string FunctionVersion { get; set; } = "1.0";
        public ICognitoIdentity Identity { get; set; }
        public string InvokedFunctionArn { get; set; } = "arn:aws:lambda:us-east-1:123456789012:function:test-function";
        public ILambdaLogger Logger { get; set; }
        public string LogGroupName { get; set; } = "/aws/lambda/test-function";
        public string LogStreamName { get; set; } = "2023/01/01/[$LATEST]abcdef123456";
        public int MemoryLimitInMB { get; set; } = 128;
        public TimeSpan RemainingTime { get; set; } = TimeSpan.FromMinutes(5);
    }

    [Fact]
    public void ProcessingResult_Property_ShouldBeAccessible()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();

        // Act
        var result = processor.ProcessingResult;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandler_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandler();
        var testEvent = new SQSEvent();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerAndDeserializationOptions_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandler();
        var testEvent = new SQSEvent();
        var deserializationOptions = new DeserializationOptions();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler, deserializationOptions);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerAndCancellationToken_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandler();
        var testEvent = new SQSEvent();
        var cancellationToken = new CancellationToken();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerDeserializationOptionsAndCancellationToken_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandler();
        var testEvent = new SQSEvent();
        var deserializationOptions = new DeserializationOptions();
        var cancellationToken = new CancellationToken();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler, deserializationOptions, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerDeserializationOptionsAndProcessingOptions_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandler();
        var testEvent = new SQSEvent();
        var deserializationOptions = new DeserializationOptions();
        var processingOptions = new ProcessingOptions();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler, deserializationOptions, processingOptions);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerWithContext_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandlerWithContext();
        var testEvent = new SQSEvent();
        var context = new MockLambdaContext();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler, context);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerWithContextAndDeserializationOptions_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandlerWithContext();
        var testEvent = new SQSEvent();
        var context = new MockLambdaContext();
        var deserializationOptions = new DeserializationOptions();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler, context, deserializationOptions);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerWithContextAndCancellationToken_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandlerWithContext();
        var testEvent = new SQSEvent();
        var context = new MockLambdaContext();
        var cancellationToken = new CancellationToken();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler, context, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerWithContextDeserializationOptionsAndCancellationToken_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandlerWithContext();
        var testEvent = new SQSEvent();
        var context = new MockLambdaContext();
        var deserializationOptions = new DeserializationOptions();
        var cancellationToken = new CancellationToken();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler, context, deserializationOptions, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public async Task ProcessAsync_WithTypedHandlerWithContextDeserializationOptionsAndProcessingOptions_ShouldReturnProcessingResult()
    {
        // Arrange
        var processor = new MockTypedBatchProcessor();
        var handler = new MockTypedRecordHandlerWithContext();
        var testEvent = new SQSEvent();
        var context = new MockLambdaContext();
        var deserializationOptions = new DeserializationOptions();
        var processingOptions = new ProcessingOptions();

        // Act
        var result = await processor.ProcessAsync(testEvent, handler, context, deserializationOptions, processingOptions);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingResult<SQSEvent.SQSMessage>>(result);
    }

    [Fact]
    public void Interface_ShouldHaveCorrectGenericConstraints()
    {
        // Arrange & Act
        var interfaceType = typeof(ITypedBatchProcessor<,>);
        var genericParameters = interfaceType.GetGenericArguments();

        // Assert
        Assert.Equal(2, genericParameters.Length);
        
        // TEvent should be contravariant (in)
        Assert.True(genericParameters[0].GenericParameterAttributes.HasFlag(System.Reflection.GenericParameterAttributes.Contravariant));
        
        // TRecord should be invariant (no variance)
        Assert.False(genericParameters[1].GenericParameterAttributes.HasFlag(System.Reflection.GenericParameterAttributes.Contravariant));
        Assert.False(genericParameters[1].GenericParameterAttributes.HasFlag(System.Reflection.GenericParameterAttributes.Covariant));
    }

    [Fact]
    public void Interface_ShouldInheritFromCorrectNamespace()
    {
        // Arrange & Act
        var interfaceType = typeof(ITypedBatchProcessor<,>);

        // Assert
        Assert.Equal("AWS.Lambda.Powertools.BatchProcessing", interfaceType.Namespace);
    }

    [Fact]
    public void Interface_ShouldHaveCorrectMethodSignatures()
    {
        // Arrange & Act
        var interfaceType = typeof(ITypedBatchProcessor<,>);
        var methods = interfaceType.GetMethods();

        // Assert
        Assert.True(methods.Length >= 10); // Should have at least 10 ProcessAsync overloads plus ProcessingResult property getter
        
        // Check that all ProcessAsync methods are generic
        var processAsyncMethods = Array.FindAll(methods, m => m.Name == "ProcessAsync");
        Assert.True(processAsyncMethods.Length >= 10);
        
        foreach (var method in processAsyncMethods)
        {
            Assert.True(method.IsGenericMethodDefinition);
            Assert.Equal(1, method.GetGenericArguments().Length); // Should have one generic parameter T
        }
    }

    [Fact]
    public void Interface_ProcessingResult_Property_ShouldBeReadOnly()
    {
        // Arrange & Act
        var interfaceType = typeof(ITypedBatchProcessor<,>);
        var property = interfaceType.GetProperty("ProcessingResult");

        // Assert
        Assert.NotNull(property);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite); // Should be read-only
    }
}