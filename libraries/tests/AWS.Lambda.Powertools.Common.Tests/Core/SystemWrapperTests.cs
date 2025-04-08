using System;
using System.IO;
using System.Reflection;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

[Collection("Sequential")]
public class SystemWrapperTests : IDisposable
{
    private readonly IPowertoolsEnvironment _mockEnvironment;
    private readonly StringWriter _testWriter;
    private readonly FieldInfo _outputResetPerformedField;


    public SystemWrapperTests()
    {
        _mockEnvironment = Substitute.For<IPowertoolsEnvironment>();
        _testWriter = new StringWriter();
        
        // Get access to private field for testing
        _outputResetPerformedField = typeof(SystemWrapper).GetField("_outputResetPerformed", 
            BindingFlags.NonPublic | BindingFlags.Static);

        // Reset static state between tests
        SystemWrapper.ResetTestMode();
        _outputResetPerformedField.SetValue(null, false);
    }

    [Fact]
    public void Log_InProductionMode_ResetsOutputOnce()
    {
        // Arrange
        var wrapper = new SystemWrapper(_mockEnvironment);
        var message1 = "First message";
        var message2 = "Second message";
        _outputResetPerformedField.SetValue(null, false);

        // Act
        wrapper.Log(message1);
        bool afterFirstLog = (bool)_outputResetPerformedField.GetValue(null);
        wrapper.Log(message2);
        bool afterSecondLog = (bool)_outputResetPerformedField.GetValue(null);

        // Assert
        Assert.True(afterFirstLog, "Flag should be set after first log");
        Assert.True(afterSecondLog, "Flag should remain set after second log");
    }

    [Fact]
    public void LogLine_InProductionMode_ResetsOutputOnce()
    {
        // Arrange
        var wrapper = new SystemWrapper(_mockEnvironment);
        var message1 = "First line";
        var message2 = "Second line";
        _outputResetPerformedField.SetValue(null, false);

        // Act
        wrapper.LogLine(message1);
        bool afterFirstLog = (bool)_outputResetPerformedField.GetValue(null);
        wrapper.LogLine(message2);
        bool afterSecondLog = (bool)_outputResetPerformedField.GetValue(null);

        // Assert
        Assert.True(afterFirstLog, "Flag should be set after first LogLine");
        Assert.True(afterSecondLog, "Flag should remain set after second LogLine");
    }

    [Fact]
    public void ClearOutputResetFlag_ResetsFlag_AllowsSubsequentReset()
    {
        // Arrange
        var wrapper = new SystemWrapper(_mockEnvironment);
        _outputResetPerformedField.SetValue(null, false);
        
        // Act
        wrapper.Log("First message"); // This should cause a reset
        bool afterFirstLog = (bool)_outputResetPerformedField.GetValue(null);
        
        SystemWrapper.ClearOutputResetFlag();
        bool afterClear = (bool)_outputResetPerformedField.GetValue(null);
        
        wrapper.Log("After clear"); // This should cause another reset
        bool afterSecondLog = (bool)_outputResetPerformedField.GetValue(null);

        // Assert
        Assert.True(afterFirstLog, "Flag should be set after first log");
        Assert.False(afterClear, "Flag should be cleared after ClearOutputResetFlag");
        Assert.True(afterSecondLog, "Flag should be set again after second log");
    }

    [Fact]
    public void Log_InTestMode_WritesToTestOutput()
    {
        // Arrange
        var wrapper = new SystemWrapper(_mockEnvironment);
        SystemWrapper.SetOut(_testWriter);
        var message = "Test message";

        // Act
        wrapper.Log(message);

        // Assert
        Assert.Equal(message, _testWriter.ToString());
    }

    [Fact]
    public void LogLine_InTestMode_WritesToTestOutput()
    {
        // Arrange
        var wrapper = new SystemWrapper(_mockEnvironment);
        SystemWrapper.SetOut(_testWriter);
        var message = "Test line";

        // Act
        wrapper.LogLine(message);

        // Assert
        Assert.Equal(message + Environment.NewLine, _testWriter.ToString());
    }

    [Fact]
    public void ResetTestMode_ResetsTestState()
    {
        // Arrange
        var wrapper = new SystemWrapper(_mockEnvironment);
        SystemWrapper.SetOut(_testWriter);
        var message = "This should go to console";

        // Act
        SystemWrapper.ResetTestMode();

        // Can't directly test that this goes to console, but we can verify
        // it doesn't go to the test writer
        wrapper.Log(message);

        // Assert
        Assert.Equal("", _testWriter.ToString());
    }

    [Fact]
    public void SetOut_EnablesTestMode()
    {
        // Arrange
        var wrapper = new SystemWrapper(_mockEnvironment);
        var message = "Test output";

        // Act
        SystemWrapper.SetOut(_testWriter);
        wrapper.Log(message);

        // Assert
        Assert.Equal(message, _testWriter.ToString());
    }

    [Fact]
    public void Log_InTestMode_DoesNotCallResetConsoleOutput()
    {
        // Arrange
        var wrapper = new SystemWrapper(_mockEnvironment);
        SystemWrapper.SetOut(_testWriter);
        var message1 = "First test message";
        var message2 = "Second test message";

        // Act
        wrapper.Log(message1);
        wrapper.Log(message2);

        // Assert
        Assert.Equal(message1 + message2, _testWriter.ToString());
    }

    [Fact]
    public void Log_AfterClearingFlag_ResetsOutputAgain()
    {
        // Arrange
        var wrapper = new SystemWrapper(_mockEnvironment);
        _outputResetPerformedField.SetValue(null, false);

        // Act
        wrapper.Log("First message"); // Should reset output
        bool afterFirstLog = (bool)_outputResetPerformedField.GetValue(null);
        
        SystemWrapper.ClearOutputResetFlag();
        bool afterClear = (bool)_outputResetPerformedField.GetValue(null);
        
        wrapper.Log("Second message"); // Should reset again
        bool afterSecondLog = (bool)_outputResetPerformedField.GetValue(null);

        // Assert
        Assert.True(afterFirstLog, "Flag should be set after first log");
        Assert.False(afterClear, "Flag should be reset after clearing");
        Assert.True(afterSecondLog, "Flag should be set after second log");
    }

    public void Dispose()
    {
        _testWriter?.Dispose();
        SystemWrapper.ResetTestMode();
        _outputResetPerformedField.SetValue(null, false);
    }
}