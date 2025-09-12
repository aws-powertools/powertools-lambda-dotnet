using AWS.Lambda.Powertools.Common;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests;

[Collection("Sequential")]
public class MetricsTests
{
    [Fact]
    public void Metrics_Set_Execution_Environment_Context()
    {
        // Arrange
        Metrics.ResetForTest();
        var assemblyName = "AWS.Lambda.Powertools.Metrics";
        var assemblyVersion = "1.0.0";

        var env = Substitute.For<IPowertoolsEnvironment>();
        env.GetAssemblyName(Arg.Any<Metrics>()).Returns(assemblyName);
        env.GetAssemblyVersion(Arg.Any<Metrics>()).Returns(assemblyVersion);
    
        var conf = new PowertoolsConfigurations(new SystemWrapper(env));
    
        var metrics = new Metrics(conf);
        
        // Assert
        env.Received(1).SetEnvironmentVariable(
            "AWS_EXECUTION_ENV", $"{Constants.FeatureContextIdentifier}/Metrics/{assemblyVersion}"
        );

        env.Received(1).GetEnvironmentVariable("AWS_EXECUTION_ENV");
    }
}