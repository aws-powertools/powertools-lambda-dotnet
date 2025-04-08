using System;
using System.IO;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class ConsoleWrapperTests : IDisposable
{
    private StringWriter _writer;

    public ConsoleWrapperTests()
    {
        // Setup a new StringWriter for each test
        _writer = new StringWriter();
        // Reset static state for clean testing
        ConsoleWrapper.ResetForTest();
    }

    [Fact]
    public void WriteLine_Should_Write_To_Console()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        ConsoleWrapper.SetOut(_writer);

        // Act
        consoleWrapper.WriteLine("test message");

        // Assert
        Assert.Equal($"test message{Environment.NewLine}", _writer.ToString());
    }

    [Fact]
    public void Error_Should_Write_To_Error_Console()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        ConsoleWrapper.SetOut(_writer);
        Console.SetError(_writer);

        // Act
        consoleWrapper.Error("error message");
        _writer.Flush();

        // Assert
        Assert.Equal($"error message{Environment.NewLine}", _writer.ToString());
    }

    [Fact]
    public void SetOut_Should_Override_Console_Output()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        ConsoleWrapper.SetOut(_writer);

        // Act
        consoleWrapper.WriteLine("test message");

        // Assert
        Assert.Equal($"test message{Environment.NewLine}", _writer.ToString());
    }

    [Fact]
    public void OverrideLambdaLogger_Should_Override_Console_Out()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        ConsoleWrapper.SetOut(_writer);

        // Act
        consoleWrapper.WriteLine("test message");

        // Assert
        Assert.Equal($"test message{Environment.NewLine}", _writer.ToString());
    }

    [Fact]
    public void WriteLine_WritesMessageToConsole()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        ConsoleWrapper.SetOut(_writer);

        // Act
        consoleWrapper.WriteLine("Test message");

        // Assert
        var output = _writer.ToString();
        Assert.Contains("Test message", output);
    }

    [Fact]
    public void SetOut_OverridesConsoleOutput()
    {
        // Act
        ConsoleWrapper.SetOut(_writer);
        Console.WriteLine("Test override");

        // Assert
        var output = _writer.ToString();
        Assert.Contains("Test override", output);
    }

    [Fact]
    public void StaticWriteLine_FormatsLogMessageCorrectly()
    {
        // Arrange
        ConsoleWrapper.SetOut(_writer);

        // Act - Using reflection to call internal static method
        typeof(ConsoleWrapper)
            .GetMethod("WriteLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null)
            ?.Invoke(null, new object[] { "INFO", "Test log message" });

        // Assert
        var output = _writer.ToString();
        Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z\tINFO\tTest log message", output);
    }

    [Fact]
    public void ClearOutputResetFlag_ResetsFlag()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        ConsoleWrapper.SetOut(_writer);
        
        // Act
        consoleWrapper.WriteLine("First message"); // Should set the reset flag
        ConsoleWrapper.ClearOutputResetFlag();
        consoleWrapper.WriteLine("Second message"); // Should set it again
        
        // Assert
        Assert.Equal($"First message{Environment.NewLine}Second message{Environment.NewLine}", _writer.ToString());
    }

    public void Dispose()
    {
        ConsoleWrapper.ResetForTest();
        _writer?.Dispose();
    }
}