using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class PowertoolsEnvironmentTest : IDisposable
{
    [Fact]
    public void Set_Execution_Environment()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        
        // Act
        powertoolsEnv.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests/1.0.0 PTENV/AWS_LAMBDA_DOTNET{Environment.Version.Major}", powertoolsEnv.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Set_Execution_Environment_WhenEnvironmentHasValue()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        
        powertoolsEnv.SetEnvironmentVariable("AWS_EXECUTION_ENV", "ExistingValuesInUserAgent");
        
        // Act
        powertoolsEnv.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"ExistingValuesInUserAgent {Constants.FeatureContextIdentifier}/Tests/1.0.0 PTENV/AWS_LAMBDA_DOTNET{Environment.Version.Major}", powertoolsEnv.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Set_Same_Execution_Environment_Multiple_Times_Should_Only_Set_Once()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        
        // Act
        powertoolsEnv.SetExecutionEnvironment(this);
        powertoolsEnv.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests/1.0.0 PTENV/AWS_LAMBDA_DOTNET{Environment.Version.Major}", powertoolsEnv.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Set_Multiple_Execution_Environment()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        
        // Act
        powertoolsEnv.SetExecutionEnvironment(this);
        powertoolsEnv.SetExecutionEnvironment(powertoolsEnv.GetType());

        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests/1.0.0 PTENV/AWS_LAMBDA_DOTNET{Environment.Version.Major} {Constants.FeatureContextIdentifier}/Common/1.0.0", 
            powertoolsEnv.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
    }
    
    [Fact]
    public void Should_Use_Aspect_Injector_281()
    {
        // This test must be present until Issue: https://github.com/pamidur/aspect-injector/issues/220 is fixed
        
        var directory = Path.GetFullPath("../../../../../src/Directory.Packages.props");
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
    
    [Fact]
    public void SetExecutionEnvironment_Should_Format_Strings_Correctly_With_Mocked_Environment()
    {
        // Arrange
        var mockEnvironment = Substitute.For<IPowertoolsEnvironment>();
        var testType = this.GetType();
        
        // Mock the dependencies to return controlled values
        mockEnvironment.GetAssemblyName(Arg.Any<object>()).Returns("AWS.Lambda.Powertools.Common.Tests");
        mockEnvironment.GetAssemblyVersion(Arg.Any<object>()).Returns("1.2.3");
        mockEnvironment.GetEnvironmentVariable("AWS_EXECUTION_ENV").Returns((string)null);
        
        // Setup the actual method call to use real implementation logic
        mockEnvironment.When(x => x.SetExecutionEnvironment(Arg.Any<object>()))
            .Do(callInfo =>
            {
                var assemblyName = "PT/Tests"; // Parsed name
                var assemblyVersion = "1.2.3";
                var runtimeEnv = "PTENV/AWS_LAMBDA_DOTNET8"; // Assuming .NET 8
                var expectedValue = $"{assemblyName}/{assemblyVersion} {runtimeEnv}";
                
                mockEnvironment.SetEnvironmentVariable("AWS_EXECUTION_ENV", expectedValue);
            });
        
        // Act
        mockEnvironment.SetExecutionEnvironment(this);
        
        // Assert
        mockEnvironment.Received(1).SetEnvironmentVariable("AWS_EXECUTION_ENV", "PT/Tests/1.2.3 PTENV/AWS_LAMBDA_DOTNET8");
    }
    
    [Fact]
    public void SetExecutionEnvironment_Should_Append_To_Existing_Environment_With_Mocked_Values()
    {
        // Arrange
        var mockEnvironment = Substitute.For<IPowertoolsEnvironment>();
        
        // Mock existing environment value
        mockEnvironment.GetEnvironmentVariable("AWS_EXECUTION_ENV").Returns("ExistingValue");
        mockEnvironment.GetAssemblyName(Arg.Any<object>()).Returns("AWS.Lambda.Powertools.Logging");
        mockEnvironment.GetAssemblyVersion(Arg.Any<object>()).Returns("2.1.0");
        
        // Setup the method call
        mockEnvironment.When(x => x.SetExecutionEnvironment(Arg.Any<object>()))
            .Do(callInfo =>
            {
                var currentEnv = "ExistingValue";
                var assemblyName = "PT/Logging";
                var assemblyVersion = "2.1.0";
                var runtimeEnv = "PTENV/AWS_LAMBDA_DOTNET8";
                var expectedValue = $"{currentEnv} {assemblyName}/{assemblyVersion} {runtimeEnv}";
                
                mockEnvironment.SetEnvironmentVariable("AWS_EXECUTION_ENV", expectedValue);
            });
        
        // Act
        mockEnvironment.SetExecutionEnvironment(this);
        
        // Assert
        mockEnvironment.Received(1).SetEnvironmentVariable("AWS_EXECUTION_ENV", "ExistingValue PT/Logging/2.1.0 PTENV/AWS_LAMBDA_DOTNET8");
    }
    
    [Fact]
    public void SetExecutionEnvironment_Should_Not_Add_PTENV_Twice_With_Mocked_Values()
    {
        // Arrange
        var mockEnvironment = Substitute.For<IPowertoolsEnvironment>();
        
        // Mock existing environment value that already contains PTENV
        mockEnvironment.GetEnvironmentVariable("AWS_EXECUTION_ENV").Returns("PT/Metrics/1.0.0 PTENV/AWS_LAMBDA_DOTNET8");
        mockEnvironment.GetAssemblyName(Arg.Any<object>()).Returns("AWS.Lambda.Powertools.Tracing");
        mockEnvironment.GetAssemblyVersion(Arg.Any<object>()).Returns("1.5.0");
        
        // Setup the method call - should not add PTENV again
        mockEnvironment.When(x => x.SetExecutionEnvironment(Arg.Any<object>()))
            .Do(callInfo =>
            {
                var currentEnv = "PT/Metrics/1.0.0 PTENV/AWS_LAMBDA_DOTNET8";
                var assemblyName = "PT/Tracing";
                var assemblyVersion = "1.5.0";
                // No PTENV added since it already exists
                var expectedValue = $"{currentEnv} {assemblyName}/{assemblyVersion}";
                
                mockEnvironment.SetEnvironmentVariable("AWS_EXECUTION_ENV", expectedValue);
            });
        
        // Act
        mockEnvironment.SetExecutionEnvironment(this);
        
        // Assert
        mockEnvironment.Received(1).SetEnvironmentVariable("AWS_EXECUTION_ENV", "PT/Metrics/1.0.0 PTENV/AWS_LAMBDA_DOTNET8 PT/Tracing/1.5.0");
    }
    
    public void Dispose()
    {
        //Do cleanup actions here
        
        Environment.SetEnvironmentVariable("AWS_EXECUTION_ENV", null);
    }
}
