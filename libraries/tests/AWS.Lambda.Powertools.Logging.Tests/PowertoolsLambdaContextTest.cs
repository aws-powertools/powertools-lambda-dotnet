using System;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Logging.Internal;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class PowertoolsLambdaContextTest
{
    
    
    [Fact]
    public void Extract_WhenHasLambdaContextArgument_InitializesLambdaContextInfo()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext
        {
            AwsRequestId = Guid.NewGuid().ToString(),
            FunctionName = Guid.NewGuid().ToString(),
            FunctionVersion = Guid.NewGuid().ToString(),
            InvokedFunctionArn = Guid.NewGuid().ToString(),
            LogGroupName = Guid.NewGuid().ToString(),
            LogStreamName = Guid.NewGuid().ToString(),
            MemoryLimitInMB = new Random().Next()
        };

        var eventArg = new {Source = "Test"};
        var eventArgs = new AspectEventArgs
        {
            Method = GetType().GetMethod(nameof(Extract_WhenHasLambdaContextArgument_InitializesLambdaContextInfo)),
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