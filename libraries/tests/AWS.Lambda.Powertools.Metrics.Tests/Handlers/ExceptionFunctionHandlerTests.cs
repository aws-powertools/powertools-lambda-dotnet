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
    
    [Fact]
    public async Task Stack_Trace_Included_When_Decorator_Present_In_Method()
    {
        // Arrange
        Metrics.ResetForTest();
        var handler = new ExceptionFunctionHandler();

        // Act
        Task Handle() => handler.HandleDecoratorOutsideHandlerException("whatever");
        
        // Assert
        var tracedException = await Assert.ThrowsAsync<NullReferenceException>(Handle);
        Assert.StartsWith("at AWS.Lambda.Powertools.Metrics.Tests.Handlers.ExceptionFunctionHandler.__a$_around_ThisThrows", tracedException.StackTrace?.TrimStart());
    }
    
    [Fact]
    public async Task Decorator_In_Non_Handler_Method_Does_Not_Throw_Exception()
    {
        // Arrange
        Metrics.ResetForTest();
        var handler = new ExceptionFunctionHandler();

        // Act
        Task Handle() => handler.HandleDecoratorOutsideHandler("whatever");
        
        // Assert
        var tracedException = await Record.ExceptionAsync(Handle);
        Assert.Null(tracedException);
    }
}