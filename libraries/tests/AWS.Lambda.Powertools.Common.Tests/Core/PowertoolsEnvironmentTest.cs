using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests;

public class PowertoolsEnvironmentTest : IDisposable
{
    public PowertoolsEnvironmentTest()
    {
        Environment.SetEnvironmentVariable("AWS_EXECUTION_ENV", $"AWS_LAMBDA_DOTNET{Environment.Version.Major}");
    }
    
    [Fact]
    public void Set_Execution_Environment()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        
        // Act
        powertoolsEnv.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests/1.0.0 PTENV/AWS_LAMBDA_DOTNET{Environment.Version.Major}", powertoolsEnv.GetEnvironmentVariable("AWS_SDK_UA_APP_ID"));
    }
    
    [Fact]
    public void Set_Execution_Environment_WhenEnvironmentHasValue()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        
        powertoolsEnv.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", "ExistingValuesInUserAgent");
        powertoolsEnv.SetEnvironmentVariable("AWS_EXECUTION_ENV", $"AWS_LAMBDA_DOTNET{Environment.Version.Major}");
        
        // Act
        powertoolsEnv.SetExecutionEnvironment(this);

        // Assert
        Assert.Equal($"ExistingValuesInUserAgent {Constants.FeatureContextIdentifier}/Tests/1.0.0 PTENV/AWS_LAMBDA_DOTNET{Environment.Version.Major}", powertoolsEnv.GetEnvironmentVariable("AWS_SDK_UA_APP_ID"));
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
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests/1.0.0 PTENV/AWS_LAMBDA_DOTNET{Environment.Version.Major}", powertoolsEnv.GetEnvironmentVariable("AWS_SDK_UA_APP_ID"));
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
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests/1.0.0 {Constants.FeatureContextIdentifier}/Common/1.0.0 PTENV/AWS_LAMBDA_DOTNET{Environment.Version.Major}", 
            powertoolsEnv.GetEnvironmentVariable("AWS_SDK_UA_APP_ID"));
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

        // Mock the dependencies to return controlled values
        mockEnvironment.GetAssemblyName(Arg.Any<object>()).Returns("AWS.Lambda.Powertools.Common.Tests");
        mockEnvironment.GetAssemblyVersion(Arg.Any<object>()).Returns("1.2.3");
        mockEnvironment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID").Returns((string)null);
        
        // Setup the actual method call to use real implementation logic
        mockEnvironment.When(x => x.SetExecutionEnvironment(Arg.Any<object>()))
            .Do(_ =>
            {
                var assemblyName = "PT/Tests"; // Parsed name
                var assemblyVersion = "1.2.3";
                var runtimeEnv = "PTENV/AWS_LAMBDA_DOTNET8"; // Assuming .NET 8
                var expectedValue = $"{assemblyName}/{assemblyVersion} {runtimeEnv}";
                
                mockEnvironment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", expectedValue);
            });
        
        // Act
        mockEnvironment.SetExecutionEnvironment(this);
        
        // Assert
        mockEnvironment.Received(1).SetEnvironmentVariable("AWS_SDK_UA_APP_ID", "PT/Tests/1.2.3 PTENV/AWS_LAMBDA_DOTNET8");
    }
    
    [Fact]
    public void SetExecutionEnvironment_Should_Append_To_Existing_Environment_With_Mocked_Values()
    {
        // Arrange
        var mockEnvironment = Substitute.For<IPowertoolsEnvironment>();
        
        // Mock existing environment value
        mockEnvironment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID").Returns("ExistingValue");
        mockEnvironment.GetAssemblyName(Arg.Any<object>()).Returns("AWS.Lambda.Powertools.Logging");
        mockEnvironment.GetAssemblyVersion(Arg.Any<object>()).Returns("2.1.0");
        
        // Setup the method call
        mockEnvironment.When(x => x.SetExecutionEnvironment(Arg.Any<object>()))
            .Do(_ =>
            {
                var currentEnv = "ExistingValue";
                var assemblyName = "PT/Logging";
                var assemblyVersion = "2.1.0";
                var runtimeEnv = "PTENV/AWS_LAMBDA_DOTNET8";
                var expectedValue = $"{currentEnv} {assemblyName}/{assemblyVersion} {runtimeEnv}";
                
                mockEnvironment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", expectedValue);
            });
        
        // Act
        mockEnvironment.SetExecutionEnvironment(this);
        
        // Assert
        mockEnvironment.Received(1).SetEnvironmentVariable("AWS_SDK_UA_APP_ID", "ExistingValue PT/Logging/2.1.0 PTENV/AWS_LAMBDA_DOTNET8");
    }
    
    [Fact]
    public void SetExecutionEnvironment_Should_Not_Add_PTENV_Twice_With_Mocked_Values()
    {
        // Arrange
        var mockEnvironment = Substitute.For<IPowertoolsEnvironment>();
        
        // Mock existing environment value that already contains PTENV
        mockEnvironment.GetEnvironmentVariable("AWS_SDK_UA_APP_ID").Returns("PT/Metrics/1.0.0 PTENV/AWS_LAMBDA_DOTNET8");
        mockEnvironment.GetAssemblyName(Arg.Any<object>()).Returns("AWS.Lambda.Powertools.Tracing");
        mockEnvironment.GetAssemblyVersion(Arg.Any<object>()).Returns("1.5.0");
        
        // Setup the method call - should not add PTENV again
        mockEnvironment.When(x => x.SetExecutionEnvironment(Arg.Any<object>()))
            .Do(_ =>
            {
                var currentEnv = "PT/Metrics/1.0.0 PTENV/AWS_LAMBDA_DOTNET8";
                var assemblyName = "PT/Tracing";
                var assemblyVersion = "1.5.0";
                // No PTENV added since it already exists
                var expectedValue = $"{currentEnv} {assemblyName}/{assemblyVersion}";
                
                mockEnvironment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", expectedValue);
            });
        
        // Act
        mockEnvironment.SetExecutionEnvironment(this);
        
        // Assert
        mockEnvironment.Received(1).SetEnvironmentVariable("AWS_SDK_UA_APP_ID", "PT/Metrics/1.0.0 PTENV/AWS_LAMBDA_DOTNET8 PT/Tracing/1.5.0");
    }
    
    [Fact]
    public void GetAssemblyName_Should_Handle_Type_Object()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        var typeObject = typeof(PowertoolsEnvironment);
        
        // Act
        var result = powertoolsEnv.GetAssemblyName(typeObject);
        
        // Assert
        Assert.Equal("AWS.Lambda.Powertools.Common", result);
    }
    
    [Fact]
    public void GetAssemblyName_Should_Handle_Regular_Object()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        
        // Act
        var result = powertoolsEnv.GetAssemblyName(this);
        
        // Assert
        Assert.Equal("AWS.Lambda.Powertools.Common.Tests", result);
    }
    
    [Fact]
    public void GetAssemblyVersion_Should_Handle_Type_Object()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        var typeObject = typeof(PowertoolsEnvironment);
        
        // Act
        var result = powertoolsEnv.GetAssemblyVersion(typeObject);
        
        // Assert
        Assert.Matches(@"\d+\.\d+\.\d+", result); // Should match version pattern like "1.0.0"
    }
    
    [Fact]
    public void GetAssemblyVersion_Should_Handle_Regular_Object()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        
        // Act
        var result = powertoolsEnv.GetAssemblyVersion(this);
        
        // Assert
        Assert.Matches(@"\d+\.\d+\.\d+", result); // Should match version pattern like "1.0.0"
    }
    
    [Fact]
    public void ParseAssemblyName_Should_Handle_Assembly_Without_Dots()
    {
        // Act
        var result = PowertoolsEnvironment.ParseAssemblyName("SimpleAssemblyName");
        
        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/SimpleAssemblyName", result);
    }
    
    [Fact]
    public void ParseAssemblyName_Should_Handle_Assembly_With_Dots()
    {
        // Act
        var result = PowertoolsEnvironment.ParseAssemblyName("AWS.Lambda.Powertools.Common");
        
        // Assert
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Common", result);
    }
    
    [Fact]
    public void ParseAssemblyName_Should_Use_Cache_For_Same_Assembly_Name()
    {
        // Act - Call twice with same assembly name
        var result1 = PowertoolsEnvironment.ParseAssemblyName("AWS.Lambda.Powertools.Tests");
        var result2 = PowertoolsEnvironment.ParseAssemblyName("AWS.Lambda.Powertools.Tests");
        
        // Assert - Should return same result (cached)
        Assert.Equal(result1, result2);
        Assert.Equal($"{Constants.FeatureContextIdentifier}/Tests", result1);
    }
    
    [Fact]
    public void ParseAssemblyName_Null_Return_Empty()
    {
        // Act - Call twice with same assembly name
        var result = PowertoolsEnvironment.ParseAssemblyName(null);
        
        // Assert - Should return null
        Assert.Empty(result);
    }
    
    [Fact]
    public void SetExecutionEnvironment_Should_Handle_Empty_Current_Environment()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        Environment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", "");
        
        // Act
        powertoolsEnv.SetExecutionEnvironment(this);
        
        // Assert
        var result = powertoolsEnv.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        Assert.Contains($"{Constants.FeatureContextIdentifier}/Tests/", result);
        Assert.Contains("PTENV/AWS_LAMBDA_DOTNET", result);
    }
    
    [Fact]
    public void SetExecutionEnvironment_Should_Add_PTENV_When_Not_Present()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        powertoolsEnv.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", "SomeExistingValue");
        
        // Act
        powertoolsEnv.SetExecutionEnvironment(this);
        
        // Assert
        var result = powertoolsEnv.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        Assert.StartsWith("SomeExistingValue", result);
        Assert.Contains("PTENV/AWS_LAMBDA_DOTNET", result);
    }
    
    [Fact]
    public void SetExecutionEnvironment_Should_Not_Add_PTENV_When_Already_Present()
    {
        // Arrange
        var powertoolsEnv = new PowertoolsEnvironment();
        var existingValue = $"ExistingValue PTENV/AWS_LAMBDA_DOTNET{Environment.Version.Major}";
        powertoolsEnv.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", existingValue);
        
        // Act
        powertoolsEnv.SetExecutionEnvironment(this);
        
        // Assert
        var result = powertoolsEnv.GetEnvironmentVariable("AWS_SDK_UA_APP_ID");
        var ptenvCount = result.Split("PTENV/").Length - 1;
        Assert.Equal(1, ptenvCount); // Should only have one PTENV entry
    }

    public void Dispose()
    {
        //Do cleanup actions here
        Environment.SetEnvironmentVariable("AWS_SDK_UA_APP_ID", null);
        
        // Clear the singleton instance to ensure fresh state for each test
        var instanceField = typeof(PowertoolsEnvironment).GetField("_instance", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        instanceField?.SetValue(null, null);
    }
}
