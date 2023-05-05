using System;
using System.Collections.Generic;
using System.Reflection;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class PowertoolsEnvironmentTest : IDisposable
{
    [Fact]
    public void Set_Execution_Environment()
    {
        // Arrange
        var systemWrapper = new SystemWrapper(new MockEnvironment());
        
        // Act
        systemWrapper.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Fake/1.0.0", systemWrapper.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Set_Execution_Environment_WhenEnvironmentHasValue()
    {
        // Arrange
        var systemWrapper = new SystemWrapper(new MockEnvironment());
        
        systemWrapper.SetEnvironmentVariable("AWS_EXECUTION_ENV", "ExistingValuesInUserAgent");
        
        // Act
        systemWrapper.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"ExistingValuesInUserAgent {Constants.FeatureContextIdentifier}/Fake/1.0.0", systemWrapper.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Set_Multiple_Execution_Environment()
    {
        // Arrange
        var systemWrapper = new SystemWrapper(new MockEnvironment());
        
        // Act
        systemWrapper.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Fake/1.0.0", systemWrapper.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Set_Execution_Real_Environment()
    {
        // Arrange
        var systemWrapper = new SystemWrapper(new PowertoolsEnvironment());
        
        // Act
        systemWrapper.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests/1.0.0", systemWrapper.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Set_Execution_Real_Environment_Multiple()
    {
        // Arrange
        var systemWrapper = new SystemWrapper(new PowertoolsEnvironment());

        // Act
        systemWrapper.SetExecutionEnvironment(this);
        systemWrapper.SetExecutionEnvironment(systemWrapper);

        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests/1.0.0 {Constants.FeatureContextIdentifier}/Common/0.0.1", systemWrapper.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Set_Execution_Real_Environment_Multiple_Avoid_Duplicate()
    {
        // Arrange
        var systemWrapper = new SystemWrapper(new PowertoolsEnvironment());
        
        // Act
        systemWrapper.SetExecutionEnvironment(this);
        systemWrapper.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests/1.0.0", systemWrapper.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }

    public void Dispose()
    {
        //Do cleanup actions here
        
        Environment.SetEnvironmentVariable("AWS_EXECUTION_ENV", null);
    }
}

/// <summary>
/// Fake Environment for testing
/// </summary>
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
        return "AWS.Lambda.Powertools.Fake";
    }

    public string GetAssemblyVersion<T>(T type)
    {
        return "1.0.0";
    }
}