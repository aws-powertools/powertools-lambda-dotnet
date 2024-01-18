using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.Common.Tests;

public class PowertoolsEnvironmentTest : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;

    public PowertoolsEnvironmentTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

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
    public void Should_Use_Aspect_Injector_281()
    {
        // This test must be present until Issue: https://github.com/pamidur/aspect-injector/issues/220 is fixed
        
        var path = "../../../../../src/Directory.Packages.props";
        // Test to see if running in CI/CD and add an extra ../
        if (Environment.CurrentDirectory.EndsWith("codecov"))
        {
            path = "../../../../../../src/Directory.Packages.props";
        }
        
        var directory = Path.GetFullPath(path);
        var doc = XDocument.Load(directory);

        var packageReference = doc.XPathSelectElements("//PackageVersion")
            .Select(pr => new
            {
                Include = pr.Attribute("Include")!.Value,
                Version = new Version(pr.Attribute("Version")!.Value)
            }).FirstOrDefault(x => x.Include == "AspectInjector");

        Assert.NotNull(packageReference);
        Assert.Equal("2.8.1", packageReference.Version.ToString());
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
