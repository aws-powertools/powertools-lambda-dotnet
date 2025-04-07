using System;
using System.IO;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class ConsoleWrapperTests
{
    [Fact]
    public void WriteLine_Should_Write_To_Console()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        var writer = new StringWriter();
        ConsoleWrapper.SetOut(writer);

        // Act
        consoleWrapper.WriteLine("test message");

        // Assert
        Assert.Equal($"test message{Environment.NewLine}", writer.ToString());
    }

    [Fact]
    public void Error_Should_Write_To_Error_Console()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        var writer = new StringWriter();
        ConsoleWrapper.SetOut(writer);
        Console.SetError(writer);

        // Act
        consoleWrapper.Error("error message");
        writer.Flush();

        // Assert
        Assert.Equal($"error message{Environment.NewLine}", writer.ToString());
    }

    [Fact]
    public void SetOut_Should_Override_Console_Output()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        var writer = new StringWriter();
        ConsoleWrapper.SetOut(writer);

        // Act
        consoleWrapper.WriteLine("test message");

        // Assert
        Assert.Equal($"test message{Environment.NewLine}", writer.ToString());
    }

    [Fact]
    public void OverrideLambdaLogger_Should_Override_Console_Out()
    {
// Arrange
        var originalOut = Console.Out;
        try
        {
            var consoleWrapper = new ConsoleWrapper();

            // Act - create a custom StringWriter and set it after constructor 
            // but before WriteLine (which triggers OverrideLambdaLogger)
            var writer = new StringWriter();
            Console.SetOut(writer);

            consoleWrapper.WriteLine("test message");

            // Assert
            Assert.Equal($"test message{Environment.NewLine}", writer.ToString());
        }
        finally
        {
            // Restore original console out
            Console.SetOut(originalOut);
        }
    }
}