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
        Console.SetOut(writer);

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
        Console.SetError(writer);

        // Act
        consoleWrapper.Error("error message");

        // Assert
        Assert.Equal($"error message{Environment.NewLine}", writer.ToString());
    }

    [Fact]
    public void ReadLine_Should_Read_From_Console()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        var reader = new StringReader("input text");
        Console.SetIn(reader);

        // Act
        var result = consoleWrapper.ReadLine();

        // Assert
        Assert.Equal("input text", result);
    }
}