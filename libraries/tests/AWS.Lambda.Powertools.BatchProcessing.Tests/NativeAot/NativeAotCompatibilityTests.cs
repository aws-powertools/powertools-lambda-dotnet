using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Common.Utils;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.NativeAot;

/// <summary>
///     Covers the code paths BatchProcessing takes when dynamic code isn't supported (Native AOT).
///     <see cref="RuntimeFeatureWrapper" /> simulates Native AOT; the real thing is verified with Native AOT publishes.
/// </summary>
[Collection("Sequential")]
public class NativeAotCompatibilityTests : IDisposable
{
    public void Dispose()
    {
        RuntimeFeatureWrapper.Reset();
        TypedSqsBatchProcessor.Result?.Clear();
    }

    [Fact]
    public void DynamoDbRecordDataExtractor_ExtractData_WritesTheRecordShapeTypedHandlersDeserialize()
    {
        var record = new DynamoDBEvent.DynamodbStreamRecord
        {
            EventName = "MODIFY",
            Dynamodb = new DynamoDBEvent.StreamRecord
            {
                Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["id"] = new() { S = "42" }
                },
                NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["id"] = new() { S = "42" },
                    ["qty"] = new() { N = "3" },
                    ["active"] = new() { BOOL = true },
                    ["tags"] = new() { SS = new List<string> { "a", "b" } },
                    ["nested"] = new()
                    {
                        M = new Dictionary<string, DynamoDBEvent.AttributeValue> { ["k"] = new() { NULL = true } }
                    },
                    ["list"] = new() { L = new List<DynamoDBEvent.AttributeValue> { new() { NS = new List<string> { "1" } } } }
                },
                OldImage = null,
                SequenceNumber = "111",
                SizeBytes = 26,
                StreamViewType = "NEW_AND_OLD_IMAGES"
            }
        };

        var json = DynamoDbRecordDataExtractor.Instance.ExtractData(record);

        // Same JSON the extractor wrote before it moved from an anonymous type to a source-generated context.
        Assert.Equal(ExpectedDynamoDbRecordJson, json);
    }

    // Captured from the reflection-based serialization of the previous anonymous type.
    private const string ExpectedDynamoDbRecordJson =
        """{"EventName":"MODIFY","Keys":{"id":{"B":null,"BOOL":null,"BS":null,"L":null,"M":null,"N":null,"NS":null,"NULL":null,"S":"42","SS":null}},"NewImage":{"id":{"B":null,"BOOL":null,"BS":null,"L":null,"M":null,"N":null,"NS":null,"NULL":null,"S":"42","SS":null},"qty":{"B":null,"BOOL":null,"BS":null,"L":null,"M":null,"N":"3","NS":null,"NULL":null,"S":null,"SS":null},"active":{"B":null,"BOOL":true,"BS":null,"L":null,"M":null,"N":null,"NS":null,"NULL":null,"S":null,"SS":null},"tags":{"B":null,"BOOL":null,"BS":null,"L":null,"M":null,"N":null,"NS":null,"NULL":null,"S":null,"SS":["a","b"]},"nested":{"B":null,"BOOL":null,"BS":null,"L":null,"M":{"k":{"B":null,"BOOL":null,"BS":null,"L":null,"M":null,"N":null,"NS":null,"NULL":true,"S":null,"SS":null}},"N":null,"NS":null,"NULL":null,"S":null,"SS":null},"list":{"B":null,"BOOL":null,"BS":null,"L":[{"B":null,"BOOL":null,"BS":null,"L":null,"M":null,"N":null,"NS":["1"],"NULL":null,"S":null,"SS":null}],"M":null,"N":null,"NS":null,"NULL":null,"S":null,"SS":null}},"OldImage":null,"SequenceNumber":"111","SizeBytes":26,"StreamViewType":"NEW_AND_OLD_IMAGES"}""";

    [Fact]
    public void Deserialize_WithoutJsonSerializerContext_WhenDynamicCodeIsNotSupported_ThrowsAotCompatibilityException()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var ex = Assert.Throws<AotCompatibilityException>(() =>
            JsonDeserializationService.Instance.Deserialize<Order>("{\"Id\":\"1\"}"));

        Assert.Equal(typeof(Order), ex.TargetType);
        Assert.Contains("JsonSerializerContext", ex.Message);
    }

    [Fact]
    public void TryDeserialize_WithoutJsonSerializerContext_WhenDynamicCodeIsNotSupported_ReturnsFalse()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var ok = JsonDeserializationService.Instance.TryDeserialize<Order>("{\"Id\":\"1\"}", out var result,
            out var exception);

        Assert.False(ok);
        Assert.Null(result);
        Assert.IsType<AotCompatibilityException>(exception);
    }

    [Fact]
    public void Deserialize_WithJsonSerializerContext_WhenDynamicCodeIsNotSupported_UsesTheContext()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);

        var result = JsonDeserializationService.Instance.Deserialize<Order>("{\"Id\":\"1\"}",
            new DeserializationOptions(NativeAotTestContext.Default));

        Assert.Equal("1", result.Id);
    }

    public static TheoryData<string> TypedHandlerFunctions => new()
    {
        nameof(AotFunctions.WithTypedHandler),
        nameof(AotFunctions.WithTypedHandlerProvider),
        nameof(AotFunctions.WithTypedHandlerWithContext),
        nameof(AotFunctions.WithTypedHandlerWithContextProvider)
    };

    [Theory]
    [MemberData(nameof(TypedHandlerFunctions))]
    public void TypedHandler_WhenDynamicCodeIsNotSupported_ProcessesBatchAndReportsFailures(string function)
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        var functions = new AotFunctions();
        var sqsEvent = CreateSqsEvent("""{"Id":"1"}""", """{"Id":"fail"}""", """{"Id":"3"}""");
        var context = Substitute.For<ILambdaContext>();

        var response = function switch
        {
            nameof(AotFunctions.WithTypedHandler) => functions.WithTypedHandler(sqsEvent),
            nameof(AotFunctions.WithTypedHandlerProvider) => functions.WithTypedHandlerProvider(sqsEvent),
            nameof(AotFunctions.WithTypedHandlerWithContext) => functions.WithTypedHandlerWithContext(sqsEvent, context),
            _ => functions.WithTypedHandlerWithContextProvider(sqsEvent, context)
        };

        Assert.Equal(new[] { "fail" }, response.BatchItemFailures.Select(f => f.ItemIdentifier));
        Assert.Equal(new[] { "1", "3" }, TypedSqsBatchProcessor.Result.SuccessRecords.Select(r => r.Record.MessageId));
    }

    [Fact]
    public async Task TypedHandler_WithValueTypeData_WhenDynamicCodeIsNotSupported_ThrowsNotSupportedException()
    {
        RuntimeFeatureWrapper.SetIsDynamicCodeSupported(false);
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandler = typeof(ValueTypeHandler),
            JsonSerializerContext = typeof(NativeAotTestContext)
        };
        var args = new object[] { CreateSqsEvent("""{"Value":1}""") };

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() =>
            attribute.CreateAspectHandler(args).HandleAsync(args));

        Assert.Contains(nameof(ValueTypeHandler), ex.Message);
        Assert.Contains(nameof(ValueTypeData), ex.Message);
    }

    [Fact]
    public async Task TypedHandler_WithValueTypeData_WhenDynamicCodeIsSupported_ProcessesBatch()
    {
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandler = typeof(ValueTypeHandler)
        };
        var args = new object[] { CreateSqsEvent("""{"Value":1}""") };

        await attribute.CreateAspectHandler(args).HandleAsync(args);

        Assert.Single(TypedSqsBatchProcessor.Result.SuccessRecords);
    }

    [Fact]
    public async Task TypedHandlerWithContext_WithoutLambdaContextArgument_ThrowsInvalidOperationException()
    {
        var attribute = new BatchProcessorAttribute
        {
            TypedRecordHandlerWithContext = typeof(OrderHandlerWithContext)
        };
        var args = new object[] { CreateSqsEvent("""{"Id":"1"}""") };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            attribute.CreateAspectHandler(args).HandleAsync(args));

        Assert.Contains(nameof(OrderHandlerWithContext), ex.Message);
        Assert.Contains(nameof(ILambdaContext), ex.Message);
    }

    private static SQSEvent CreateSqsEvent(params string[] bodies)
    {
        return new SQSEvent
        {
            Records = bodies.Select((body, i) => new SQSEvent.SQSMessage
            {
                MessageId = body.Contains("fail") ? "fail" : (i + 1).ToString(),
                Body = body,
                EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:queue"
            }).ToList()
        };
    }

    public class Order
    {
        public string Id { get; set; }
    }

    public struct ValueTypeData
    {
        public int Value { get; set; }
    }

    public class OrderHandler : ITypedRecordHandler<Order>
    {
        public Task<RecordHandlerResult> HandleAsync(Order data, CancellationToken cancellationToken)
        {
            if (data.Id == "fail") throw new InvalidOperationException("failed");
            return Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class OrderHandlerWithContext : ITypedRecordHandlerWithContext<Order>
    {
        public Task<RecordHandlerResult> HandleAsync(Order data, ILambdaContext context,
            CancellationToken cancellationToken)
        {
            if (context == null || data.Id == "fail") throw new InvalidOperationException("failed");
            return Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class OrderHandlerProvider : ITypedRecordHandlerProvider<Order>
    {
        public ITypedRecordHandler<Order> Create() => new OrderHandler();
    }

    public class OrderHandlerWithContextProvider : ITypedRecordHandlerWithContextProvider<Order>
    {
        public ITypedRecordHandlerWithContext<Order> Create() => new OrderHandlerWithContext();
    }

    public class ValueTypeHandler : ITypedRecordHandler<ValueTypeData>
    {
        public Task<RecordHandlerResult> HandleAsync(ValueTypeData data, CancellationToken cancellationToken)
        {
            return Task.FromResult(RecordHandlerResult.None);
        }
    }

    public class AotFunctions
    {
        [BatchProcessor(TypedRecordHandler = typeof(OrderHandler), JsonSerializerContext = typeof(NativeAotTestContext),
            ThrowOnFullBatchFailure = false)]
        public BatchItemFailuresResponse WithTypedHandler(SQSEvent _) =>
            TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;

        [BatchProcessor(TypedRecordHandlerProvider = typeof(OrderHandlerProvider),
            JsonSerializerContext = typeof(NativeAotTestContext), ThrowOnFullBatchFailure = false)]
        public BatchItemFailuresResponse WithTypedHandlerProvider(SQSEvent _) =>
            TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;

        [BatchProcessor(TypedRecordHandlerWithContext = typeof(OrderHandlerWithContext),
            JsonSerializerContext = typeof(NativeAotTestContext), ThrowOnFullBatchFailure = false)]
        public BatchItemFailuresResponse WithTypedHandlerWithContext(SQSEvent _, ILambdaContext context) =>
            TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;

        [BatchProcessor(TypedRecordHandlerWithContextProvider = typeof(OrderHandlerWithContextProvider),
            JsonSerializerContext = typeof(NativeAotTestContext), ThrowOnFullBatchFailure = false)]
        public BatchItemFailuresResponse WithTypedHandlerWithContextProvider(SQSEvent _, ILambdaContext context) =>
            TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
    }
}

[JsonSerializable(typeof(NativeAotCompatibilityTests.Order))]
[JsonSerializable(typeof(NativeAotCompatibilityTests.ValueTypeData))]
public partial class NativeAotTestContext : JsonSerializerContext;
