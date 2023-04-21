using System;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Tracing.Internal;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

public class XRayRecorderTests
{
    [Fact]
    public void Tracing_Set_Execution_Environment_Context()
    {
        // Arrange
        var assemblyName = "AWS.Lambda.Powertools.Tracing";
        var assemblyVersion = "1.0.0";
        
        var env = new Mock<IPowertoolsEnvironment>();
        env.Setup(x => x.GetAssemblyName(It.IsAny<XRayRecorder>())).Returns(assemblyName);
        env.Setup(x => x.GetAssemblyVersion(It.IsAny<XRayRecorder>())).Returns(assemblyVersion);

        var conf = new PowertoolsConfigurations(new SystemWrapper(env.Object));
        var awsXray = new Mock<IAWSXRayRecorder>();
        
        // Act
        var xRayRecorder = new XRayRecorder(awsXray.Object, conf);

        // Assert
        env.Verify(v =>
            v.SetEnvironmentVariable(
                "AWS_EXECUTION_ENV", $"PTFeature/Tracing/{assemblyVersion}"
            ), Times.Once);
            
        env.Verify(v =>
            v.GetEnvironmentVariable(
                "AWS_EXECUTION_ENV"
            ), Times.Once);
        
        Assert.NotNull(xRayRecorder);
    }
    
    [Fact]
    public void Tracing_Instance()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        var awsXray = new Mock<IAWSXRayRecorder>();
    
        // Act
        
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
        
        // Assert
        Assert.Equal(tracing, XRayRecorder.Instance);
    }
    
    [Fact]
    public void Tracing_Being_Subsegment()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(true);
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        tracing.BeginSubsegment("test");
        
        // Assert
        awsXray.Verify(v =>
            v.BeginSubsegment("test", null
            ), Times.Once);
    }
    
    [Fact]
    public void Tracing_Set_Namespace()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(true);
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        tracing.SetNamespace("test");
        
        // Assert
        awsXray.Verify(v =>
            v.SetNamespace("test"
            ), Times.Once);
    }
    
    [Fact]
    public void Tracing_Add_Annotation()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(true);
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        tracing.AddAnnotation("key", "value");
        
        // Assert
        awsXray.Verify(v =>
            v.AddAnnotation("key", "value"
            ), Times.Once);
    }
    
    [Fact]
    public void Tracing_Add_Metadata()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(true);
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        tracing.AddMetadata("nameSpace","key", "value");
        
        // Assert
        awsXray.Verify(v =>
            v.AddMetadata("nameSpace","key", "value"
            ), Times.Once);
    }
    
    [Fact]
    public void Tracing_End_Subsegment()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(true);
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        tracing.EndSubsegment();
        
        // Assert
        awsXray.Verify(v =>
            v.EndSubsegment(null), Times.Once);
    }
    
    [Fact]
    public void Tracing_Get_Entity_In_Lambda_Environment()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(true);
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        awsXray.Setup(x => x.TraceContext.GetEntity()).Returns(new Subsegment("root"));
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        tracing.GetEntity();
        
        // Assert
        awsXray.Verify(v =>
            v.TraceContext.GetEntity(), Times.Once);
    }
    
    [Fact]
    public void Tracing_Get_Entity_Outside_Lambda_Environment()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(false);
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        var entity = tracing.GetEntity();
        
        // Assert
        Assert.Equivalent("Root",entity.Name);
    }
    
    [Fact]
    public void Tracing_Set_Entity()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(true);

        var segment = new Segment("test");
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        awsXray.Setup(x => x.TraceContext.SetEntity(segment));
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        tracing.SetEntity(segment);
        
        // Assert
        awsXray.Verify(v =>
            v.TraceContext.SetEntity(segment), Times.Once);
    }
    
    [Fact]
    public void Tracing_Add_Exception()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(true);

        var ex = new ArgumentException("test");
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        awsXray.Setup(x => x.AddException(ex));
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        tracing.AddException(ex);
        
        // Assert
        awsXray.Verify(v =>
            v.AddException(ex), Times.Once);
    }
    
    [Fact]
    public void Tracing_Add_Http_Information()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(true);

        var key = "key";
        var value = "value";
        
        var awsXray = new Mock<IAWSXRayRecorder>();
        awsXray.Setup(x => x.AddHttpInformation(key,value));
        
        // Act
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
    
        tracing.AddHttpInformation(key,value);
        
        // Assert
        awsXray.Verify(v =>
            v.AddHttpInformation(key,value), Times.Once);
    }
    
    [Fact]
    public void Tracing_All_When_Outside_Lambda()
    {
        // Arrange
        var conf = new Mock<IPowertoolsConfigurations>();
        conf.Setup(c => c.IsLambdaEnvironment).Returns(false);

        var awsXray = new Mock<IAWSXRayRecorder>();
        var tracing = new XRayRecorder(awsXray.Object, conf.Object);
        
        // Act

        tracing.AddHttpInformation(It.IsAny<string>(),It.IsAny<string>());
        tracing.AddException(It.IsAny<Exception>());
        tracing.SetEntity(It.IsAny<Entity>());
        tracing.EndSubsegment();
        tracing.AddMetadata(It.IsAny<string>(),It.IsAny<string>(), It.IsAny<string>());
        tracing.AddAnnotation(It.IsAny<string>(),It.IsAny<string>());
        tracing.SetNamespace(It.IsAny<string>());
        tracing.BeginSubsegment(It.IsAny<string>());
        
        // Assert
        awsXray.Verify(v =>
            v.AddHttpInformation(It.IsAny<string>(),It.IsAny<string>()), Times.Never);
        awsXray.Verify(v =>
            v.AddException(It.IsAny<Exception>()), Times.Never);
        awsXray.Verify(v =>
            v.TraceContext.SetEntity(It.IsAny<Entity>()), Times.Never);
        awsXray.Verify(v =>
            v.EndSubsegment(null), Times.Never);
        awsXray.Verify(v =>
            v.AddMetadata(It.IsAny<string>(),It.IsAny<string>(), It.IsAny<string>()
            ), Times.Never);
        awsXray.Verify(v =>
            v.AddAnnotation(It.IsAny<string>(),It.IsAny<string>()
            ), Times.Never);
        awsXray.Verify(v =>
            v.SetNamespace(It.IsAny<string>()
            ), Times.Never);
        awsXray.Verify(v =>
            v.BeginSubsegment(It.IsAny<string>(), null
            ), Times.Never);
    }
}