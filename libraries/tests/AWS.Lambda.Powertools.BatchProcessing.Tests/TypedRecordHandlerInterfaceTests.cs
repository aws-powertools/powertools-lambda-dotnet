

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

/// <summary>
/// Tests for typed record handler interfaces and delegates.
/// </summary>
public class TypedRecordHandlerInterfaceTests
{
    /// <summary>
    /// Test data model for testing typed record handlers.
    /// </summary>
    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Mock implementation of ITypedRecordHandler for testing.
    /// </summary>
    public class MockTypedRecordHandler : ITypedRecordHandler<TestData>
    {
        public bool WasCalled { get; private set; }
        public TestData ReceivedData { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<RecordHandlerResult> HandleAsync(TestData data, CancellationToken cancellationToken)
        {
            WasCalled = true;
            ReceivedData = data;
            ReceivedCancellationToken = cancellationToken;
            
            return Task.FromResult(RecordHandlerResult.FromData($"Processed: {data.Name}"));
        }
    }

    /// <summary>
    /// Mock implementation of ITypedRecordHandlerWithContext for testing.
    /// </summary>
    public class MockTypedRecordHandlerWithContext : ITypedRecordHandlerWithContext<TestData>
    {
        public bool WasCalled { get; private set; }
        public TestData ReceivedData { get; private set; }
        public ILambdaContext ReceivedContext { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<RecordHandlerResult> HandleAsync(TestData data, ILambdaContext context, CancellationToken cancellationToken)
        {
            WasCalled = true;
            ReceivedData = data;
            ReceivedContext = context;
            ReceivedCancellationToken = cancellationToken;
            
            return Task.FromResult(RecordHandlerResult.FromData($"Processed: {data.Name} with context: {context?.AwsRequestId}"));
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
    public async Task ITypedRecordHandler_HandleAsync_Should_Process_Data_Correctly()
    {
        // Arrange
        var handler = new MockTypedRecordHandler();
        var testData = new TestData
        {
            Id = 1,
            Name = "Test Item",
            Timestamp = DateTime.UtcNow
        };
        var cancellationToken = new CancellationToken();

        // Act
        var result = await handler.HandleAsync(testData, cancellationToken);

        // Assert
        Assert.True(handler.WasCalled);
        Assert.Equal(testData, handler.ReceivedData);
        Assert.Equal(cancellationToken, handler.ReceivedCancellationToken);
        Assert.NotNull(result);
        Assert.Equal("Processed: Test Item", result.Data);
    }

    [Fact]
    public async Task ITypedRecordHandlerWithContext_HandleAsync_Should_Process_Data_And_Context_Correctly()
    {
        // Arrange
        var handler = new MockTypedRecordHandlerWithContext();
        var testData = new TestData
        {
            Id = 2,
            Name = "Test Item With Context",
            Timestamp = DateTime.UtcNow
        };
        var context = new MockLambdaContext();
        var cancellationToken = new CancellationToken();

        // Act
        var result = await handler.HandleAsync(testData, context, cancellationToken);

        // Assert
        Assert.True(handler.WasCalled);
        Assert.Equal(testData, handler.ReceivedData);
        Assert.Equal(context, handler.ReceivedContext);
        Assert.Equal(cancellationToken, handler.ReceivedCancellationToken);
        Assert.NotNull(result);
        Assert.Equal("Processed: Test Item With Context with context: test-request-id", result.Data);
    }

    [Fact]
    public async Task ITypedRecordHandlerWithContext_HandleAsync_Should_Handle_Null_Context()
    {
        // Arrange
        var handler = new MockTypedRecordHandlerWithContext();
        var testData = new TestData
        {
            Id = 3,
            Name = "Test Item Null Context",
            Timestamp = DateTime.UtcNow
        };
        var cancellationToken = new CancellationToken();

        // Act
        var result = await handler.HandleAsync(testData, null, cancellationToken);

        // Assert
        Assert.True(handler.WasCalled);
        Assert.Equal(testData, handler.ReceivedData);
        Assert.Null(handler.ReceivedContext);
        Assert.Equal(cancellationToken, handler.ReceivedCancellationToken);
        Assert.NotNull(result);
        Assert.Equal("Processed: Test Item Null Context with context: ", result.Data);
    }

    [Fact]
    public async Task TypedRecordHandler_Delegate_Should_Work_Correctly()
    {
        // Arrange
        bool wasCalled = false;
        TestData receivedData = null;
        CancellationToken receivedToken = default;

        TypedRecordHandler<TestData> handler = (data, cancellationToken) =>
        {
            wasCalled = true;
            receivedData = data;
            receivedToken = cancellationToken;
            return Task.FromResult(RecordHandlerResult.FromData($"Delegate processed: {data.Name}"));
        };

        var testData = new TestData
        {
            Id = 4,
            Name = "Delegate Test",
            Timestamp = DateTime.UtcNow
        };
        var cancellationToken = new CancellationToken();

        // Act
        var result = await handler(testData, cancellationToken);

        // Assert
        Assert.True(wasCalled);
        Assert.Equal(testData, receivedData);
        Assert.Equal(cancellationToken, receivedToken);
        Assert.NotNull(result);
        Assert.Equal("Delegate processed: Delegate Test", result.Data);
    }

    [Fact]
    public async Task TypedRecordHandlerWithContext_Delegate_Should_Work_Correctly()
    {
        // Arrange
        bool wasCalled = false;
        TestData receivedData = null;
        ILambdaContext receivedContext = null;
        CancellationToken receivedToken = default;

        TypedRecordHandlerWithContext<TestData> handler = (data, context, cancellationToken) =>
        {
            wasCalled = true;
            receivedData = data;
            receivedContext = context;
            receivedToken = cancellationToken;
            return Task.FromResult(RecordHandlerResult.FromData($"Delegate with context processed: {data.Name}"));
        };

        var testData = new TestData
        {
            Id = 5,
            Name = "Delegate Context Test",
            Timestamp = DateTime.UtcNow
        };
        var context = new MockLambdaContext();
        var cancellationToken = new CancellationToken();

        // Act
        var result = await handler(testData, context, cancellationToken);

        // Assert
        Assert.True(wasCalled);
        Assert.Equal(testData, receivedData);
        Assert.Equal(context, receivedContext);
        Assert.Equal(cancellationToken, receivedToken);
        Assert.NotNull(result);
        Assert.Equal("Delegate with context processed: Delegate Context Test", result.Data);
    }

    [Fact]
    public async Task SimpleTypedRecordHandler_Delegate_Should_Work_Correctly()
    {
        // Arrange
        bool wasCalled = false;
        TestData receivedData = null;

        SimpleTypedRecordHandler<TestData> handler = (data) =>
        {
            wasCalled = true;
            receivedData = data;
            return Task.FromResult(RecordHandlerResult.FromData($"Simple delegate processed: {data.Name}"));
        };

        var testData = new TestData
        {
            Id = 6,
            Name = "Simple Delegate Test",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await handler(testData);

        // Assert
        Assert.True(wasCalled);
        Assert.Equal(testData, receivedData);
        Assert.NotNull(result);
        Assert.Equal("Simple delegate processed: Simple Delegate Test", result.Data);
    }

    [Fact]
    public async Task SimpleTypedRecordHandlerWithContext_Delegate_Should_Work_Correctly()
    {
        // Arrange
        bool wasCalled = false;
        TestData receivedData = null;
        ILambdaContext receivedContext = null;

        SimpleTypedRecordHandlerWithContext<TestData> handler = (data, context) =>
        {
            wasCalled = true;
            receivedData = data;
            receivedContext = context;
            return Task.FromResult(RecordHandlerResult.FromData($"Simple delegate with context processed: {data.Name}"));
        };

        var testData = new TestData
        {
            Id = 7,
            Name = "Simple Delegate Context Test",
            Timestamp = DateTime.UtcNow
        };
        var context = new MockLambdaContext();

        // Act
        var result = await handler(testData, context);

        // Assert
        Assert.True(wasCalled);
        Assert.Equal(testData, receivedData);
        Assert.Equal(context, receivedContext);
        Assert.NotNull(result);
        Assert.Equal("Simple delegate with context processed: Simple Delegate Context Test", result.Data);
    }

    [Fact]
    public void TypedRecordHandler_Delegate_Should_Be_Assignable_From_Method()
    {
        // Arrange & Act
        TypedRecordHandler<TestData> handler = ProcessTestDataAsync;

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void TypedRecordHandlerWithContext_Delegate_Should_Be_Assignable_From_Method()
    {
        // Arrange & Act
        TypedRecordHandlerWithContext<TestData> handler = ProcessTestDataWithContextAsync;

        // Assert
        Assert.NotNull(handler);
    }

    private static async Task<RecordHandlerResult> ProcessTestDataAsync(TestData data, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        return RecordHandlerResult.FromData($"Method processed: {data.Name}");
    }

    private static async Task<RecordHandlerResult> ProcessTestDataWithContextAsync(TestData data, ILambdaContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        return RecordHandlerResult.FromData($"Method with context processed: {data.Name}");
    }
}