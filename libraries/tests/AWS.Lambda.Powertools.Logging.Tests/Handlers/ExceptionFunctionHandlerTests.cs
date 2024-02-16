using System;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Handlers;

public sealed class ExceptionFunctionHandlerTests
{
    [Fact]
    public async Task Stack_Trace_Included_When_Decorator_Present()
    {
        // Arrange
        var handler = new ExceptionFunctionHandler();

        // Act
        Task Handle() => handler.Handle("whatever");
        
        // Assert
        var tracedException = await Assert.ThrowsAsync<NullReferenceException>(Handle);
        Assert.StartsWith("at AWS.Lambda.Powertools.Logging.Tests.Handlers.ExceptionFunctionHandler.ThisThrows()", tracedException.StackTrace?.TrimStart());

    }
    
    [Fact]
    public void Utility_Should_Not_Throw_Exceptions_To_Client()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        
        var handler = new ExceptionFunctionHandler();

        // Act
        var res = handler.HandlerLoggerForExceptions("aws",lambdaContext);
        
        // Assert
        Assert.Equal("OK", res);
    }
}