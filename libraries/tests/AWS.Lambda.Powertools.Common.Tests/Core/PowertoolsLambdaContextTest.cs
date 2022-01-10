using System;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class PowertoolsLambdaContextTest
{
    private class TestLambdaContext : PowertoolsLambdaContext
    {
        protected internal TestLambdaContext(string awsRequestId, string functionName, string functionVersion, string invokedFunctionArn, string logGroupName, string logStreamName, int memoryLimitInMB) : base(awsRequestId, functionName, functionVersion, invokedFunctionArn, logGroupName, logStreamName, memoryLimitInMB) { }
    }
        
    private static TestLambdaContext NewLambdaContext()
    {
        return new TestLambdaContext
        (
            awsRequestId: Guid.NewGuid().ToString(),
            functionName: Guid.NewGuid().ToString(),
            functionVersion: Guid.NewGuid().ToString(),
            invokedFunctionArn: Guid.NewGuid().ToString(),
            logGroupName: Guid.NewGuid().ToString(),
            logStreamName: Guid.NewGuid().ToString(),
            memoryLimitInMB: new Random().Next()
        );
    }
    
    [Fact]
    public void Extract_WhenHasLambdaContextArgument_InitializesLambdaContextInfo()
    {
        // Arrange
        var lambdaContext = NewLambdaContext();
        var eventArg = new {Source = "Test"};
        var eventArgs = new AspectEventArgs
        {
            Name = Guid.NewGuid().ToString(),
            Args = new object []
            {
                eventArg,
                lambdaContext
            }
        };

        // Act && Assert
        PowertoolsLambdaContext.Clear();
        Assert.Null(PowertoolsLambdaContext.Instance);
        Assert.True(PowertoolsLambdaContext.Extract(eventArgs));
        Assert.NotNull(PowertoolsLambdaContext.Instance);
        Assert.False(PowertoolsLambdaContext.Extract(eventArgs));
        Assert.Equal(PowertoolsLambdaContext.Instance.AwsRequestId, lambdaContext.AwsRequestId);
        Assert.Equal(PowertoolsLambdaContext.Instance.FunctionName, lambdaContext.FunctionName);
        Assert.Equal(PowertoolsLambdaContext.Instance.FunctionVersion, lambdaContext.FunctionVersion);
        Assert.Equal(PowertoolsLambdaContext.Instance.InvokedFunctionArn, lambdaContext.InvokedFunctionArn);
        Assert.Equal(PowertoolsLambdaContext.Instance.LogGroupName, lambdaContext.LogGroupName);
        Assert.Equal(PowertoolsLambdaContext.Instance.LogStreamName, lambdaContext.LogStreamName);
        Assert.Equal(PowertoolsLambdaContext.Instance.MemoryLimitInMB, lambdaContext.MemoryLimitInMB);
        PowertoolsLambdaContext.Clear();
        Assert.Null(PowertoolsLambdaContext.Instance);
    }
}