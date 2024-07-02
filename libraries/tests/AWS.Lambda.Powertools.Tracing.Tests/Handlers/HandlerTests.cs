using System;
using System.Threading.Tasks;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests.Handlers;

[Collection("Sequential")]
public sealed class HandlerTests
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
        Assert.StartsWith("at AWS.Lambda.Powertools.Tracing.Tests.Handlers.ExceptionFunctionHandler.ThisThrows()", tracedException.StackTrace?.TrimStart());

    }
    
    [Fact]
    public async Task When_Decorator_Present_In_Generic_Method_Should_Not_Throw_When_Type_Changes()
    {
        // Arrange
        var handler = new FunctionHandlerForGeneric();

        // Act
        await handler.Handle("whatever");
        
        // Assert
    }
}