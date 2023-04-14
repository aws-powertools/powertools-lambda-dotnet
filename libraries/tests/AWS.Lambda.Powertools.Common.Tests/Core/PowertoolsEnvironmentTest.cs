using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class PowertoolsEnvironmentTest
{
    [Fact]
    public void Set_Execution_Environment()
    {
        // Arrange
        var systemWrapper = new SystemWrapper(new MockEnvironment());
        
        // Act
        systemWrapper.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal("Lambda_Powertools_1.0.0", systemWrapper.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Set_Execution_Environment_WhenEnvironmentHasValue()
    {
        // Arrange
        var systemWrapper = new SystemWrapper(new MockEnvironment());
        
        systemWrapper.SetEnvironmentVariable("AWS_EXECUTION_ENV", "ExistingValue");
        
        // Act
        systemWrapper.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal("ExistingValue_Lambda_Powertools_1.0.0", systemWrapper.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
}

class MockEnvironment : IPowertoolsEnvironment
{
    private readonly Dictionary<string, string> _mockEnvironment = new();
    
    public string GetEnvironmentVariable(string variableName)
    {
        return _mockEnvironment.TryGetValue(variableName, out var value) ? value : null;
    }

    public void SetEnvironmentVariable(string variableName, string value)
    {
        // Check for entry not existing and add to dictionary
        _mockEnvironment[variableName] = value;
    }

    public string GetAssemblyName<T>(T type)
    {
        return "Lambda_Powertools";
    }

    public string GetAssemblyVersion<T>(T type)
    {
        return "1.0.0";
    }
}