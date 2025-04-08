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
            .GetMethod("WriteLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                null, new[] { typeof(string), typeof(string) }, null)
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

    [Fact]
    public void Debug_InTestMode_WritesToTestOutputStream()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        ConsoleWrapper.SetOut(_writer);

        // Act
        consoleWrapper.Debug("debug message");

        // Assert
        Assert.Equal($"debug message{Environment.NewLine}", _writer.ToString());
    }

    [Fact]
    public void Debug_NotInTestMode_WritesToDebugConsole()
    {
        // Since capturing Debug output is difficult in a unit test
        // We'll use a mock or just verify the path doesn't throw
    
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        ConsoleWrapper.ResetForTest(); // Ensure we're not in test mode

        // Act & Assert - Just verify it doesn't throw
        var exception = Record.Exception(() => consoleWrapper.Debug("debug message"));
        Assert.Null(exception);
    }

    [Fact]
    public void Error_DoesNotThrowWhenNotOverridden()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
        ConsoleWrapper.ResetForTest(); // Reset to ensure _override is false
    
        // Act & Assert - Just verify it doesn't throw
        var exception = Record.Exception(() => consoleWrapper.Error("error without override"));
        Assert.Null(exception);
    }

    [Fact]
    public void Error_UsesTestOutputStreamWhenInTestMode()
    {
        // Arrange
        var consoleWrapper = new ConsoleWrapper();
    
        // Set test mode
        ConsoleWrapper.SetOut(_writer);
    
        // Act
        consoleWrapper.Error("error in test mode");

        // Assert
        Assert.Contains("error in test mode", _writer.ToString());
    }

    public void Dispose()
    {
        ConsoleWrapper.ResetForTest();
        _writer?.Dispose();
    }
}