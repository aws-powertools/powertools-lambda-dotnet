using AWS.Lambda.Powertools.Common;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Internal;

public class IdempotencyTests
{
    [Fact]
    public void Idempotency_Set_Execution_Environment_Context()
    {
        // Arrange
        var assemblyName = "AWS.Lambda.Powertools.Idempotency";
        var assemblyVersion = "1.0.0";
        
        var env = new Mock<IPowertoolsEnvironment>();
        env.Setup(x => x.GetAssemblyName(It.IsAny<Idempotency>())).Returns(assemblyName);
        env.Setup(x => x.GetAssemblyVersion(It.IsAny<Idempotency>())).Returns(assemblyVersion);

        var conf = new PowertoolsConfigurations(new SystemWrapper(env.Object));
        
        // Act
        var xRayRecorder = new Idempotency(conf);

        // Assert
        env.Verify(v =>
            v.SetEnvironmentVariable(
                "AWS_EXECUTION_ENV", $"{Constants.FeatureContextIdentifier}/Idempotency/{assemblyVersion}"
            ), Times.Once);
            
        env.Verify(v =>
            v.GetEnvironmentVariable(
                "AWS_EXECUTION_ENV"
            ), Times.Once);
        
        Assert.NotNull(xRayRecorder);
    }
}