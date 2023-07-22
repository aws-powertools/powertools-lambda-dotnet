using System;
using System.Threading.Tasks;
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
}