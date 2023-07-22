using System;
using System.Threading.Tasks;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

[Collection("Sequential")]
public sealed class ExceptionFunctionHandlerTests
{
    [Fact]
    public async Task Stack_Trace_Included_When_Decorator_Present()
    {
        // Arrange
        Metrics.ResetForTest();
        var handler = new ExceptionFunctionHandler();

        // Act
        Task Handle() => handler.Handle("whatever");
        
        // Assert
        var tracedException = await Assert.ThrowsAsync<NullReferenceException>(Handle);
        Assert.StartsWith("at AWS.Lambda.Powertools.Metrics.Tests.Handlers.ExceptionFunctionHandler.ThisThrows()", tracedException.StackTrace?.TrimStart());

    }
}