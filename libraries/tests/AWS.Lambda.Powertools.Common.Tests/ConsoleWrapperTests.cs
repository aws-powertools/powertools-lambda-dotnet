using System;
using System.IO;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class ConsoleWrapperTests : IDisposable
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
            ConsoleWrapper.SetOut(writer);

            consoleWrapper.WriteLine("test message");

            // Assert
            Assert.Equal($"test message{Environment.NewLine}", writer.ToString());
        }
        finally
        {
            // Restore original console out
            ConsoleWrapper.ResetForTest();
        }
    }
    
    [Fact]
        public void WriteLine_WritesMessageToConsole()
        {
            // Arrange
            var consoleWrapper = new ConsoleWrapper();
            var originalOutput = Console.Out;
            using var stringWriter = new StringWriter();
            ConsoleWrapper.SetOut(stringWriter);
            
            try
            {
                // Act
                consoleWrapper.WriteLine("Test message");
                
                // Assert
                var output = stringWriter.ToString();
                Assert.Contains("Test message", output);
            }
            finally
            {
                // Restore original output
                ConsoleWrapper.ResetForTest();
            }
        }
        
        [Fact]
        public void SetOut_OverridesConsoleOutput()
        {
            // Arrange
            var originalOutput = Console.Out;
            using var stringWriter = new StringWriter();
            
            try
            {
                // Act
                typeof(ConsoleWrapper)
                    .GetMethod("SetOut", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.Invoke(null, new object[] { stringWriter });
                
                Console.WriteLine("Test override");
                
                // Assert
                var output = stringWriter.ToString();
                Assert.Contains("Test override", output);
            }
            finally
            {
                // Restore original output
                Console.SetOut(originalOutput);
            }
        }
        
        [Fact]
        public void StaticWriteLine_FormatsLogMessageCorrectly()
        {
            // Arrange
            var originalOutput = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            
            try
            {
                // Act
                typeof(ConsoleWrapper)
                    .GetMethod("WriteLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null)
                    ?.Invoke(null, new object[] { "INFO", "Test log message" });
                
                // Assert
                var output = stringWriter.ToString();
                Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z\tINFO\tTest log message", output);
            }
            finally
            {
                // Restore original output
                Console.SetOut(originalOutput);
            }
        }

        public void Dispose()
        {
            ConsoleWrapper.ResetForTest();
        }
}