using AWS.Lambda.Powertools.Common;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests;

[Collection("Sequential")]
public class MetricsTests
{
    [Fact]
    public void Metrics_Set_Execution_Environment_Context()
    {
        // Arrange
        var assemblyName = "AWS.Lambda.Powertools.Metrics";
        var assemblyVersion = "1.0.0";
            
        var env = new Mock<IPowertoolsEnvironment>();
        env.Setup(x => x.GetAssemblyName(It.IsAny<Metrics>())).Returns(assemblyName);
        env.Setup(x => x.GetAssemblyVersion(It.IsAny<Metrics>())).Returns(assemblyVersion);
    
        var conf = new PowertoolsConfigurations(new SystemWrapper(env.Object));
    
        var metrics = new Metrics(conf);
        
        // Assert
        env.Verify(v =>
            v.SetEnvironmentVariable(
                "AWS_EXECUTION_ENV", $"PTFeature/Metrics/{assemblyVersion}"
            ), Times.Once);
            
        env.Verify(v =>
            v.GetEnvironmentVariable(
                "AWS_EXECUTION_ENV"
            ), Times.Once);
    }
}