using System;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Tracing.Internal;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

// Tests that use XRayRecorder singleton - isolated to prevent test pollution
[Collection("XRayRecorderTests")]
public class XRayRecorderTests : IDisposable
{
    [Fact]
    public void Tracing_Instance()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        // Assert
        Assert.Equal(tracing, XRayRecorder.Instance);
    }

    [Fact]
    public void Tracing_Begin_Subsegment()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.BeginSubsegment("test");

        // Assert
        awsXray.Received(1).BeginSubsegment("test");
    }

    [Fact]
    public void Tracing_Set_Namespace()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.SetNamespace("test");

        // Assert
        awsXray.Received(1).SetNamespace("test");
    }

    [Fact]
    public void Tracing_Add_Annotation()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.AddAnnotation("key", "value");

        // Assert
        awsXray.Received(1).AddAnnotation("key", "value");
    }

    [Fact]
    public void Tracing_Add_Metadata()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.AddMetadata("nameSpace", "key", "value");

        // Assert
        awsXray.Received(1).AddMetadata("nameSpace", "key", "value");
    }
    
    [Fact]
    public void Tracing_End_Subsegment()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.EndSubsegment();

        // Assert
        awsXray.Received(1).EndSubsegment();
    }
    
    [Fact]
    public void Tracing_End_Subsegment_Failed_Should_Cath_And_AddException()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();
        var fail = true;
        var exception = new Exception("Oops, something went wrong");
        
        awsXray.When(x=> x.EndSubsegment()).Do(x => 
        {
            if (fail)
            {
                fail = false;
                throw exception;
            }
        });
        
        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.EndSubsegment();

        // Assert
        awsXray.Received(1).BeginSubsegment("Error in Tracing utility - see Exceptions tab");
        awsXray.Received(1).MarkError();
        awsXray.Received(1).AddException(exception);
        awsXray.Received(2).EndSubsegment();
    }

    [Fact]
    public void Tracing_Get_Entity_In_Lambda_Environment()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();
        awsXray.TraceContext.GetEntity().Returns(new Subsegment("root"));

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.GetEntity();

        // Assert
        awsXray.TraceContext.Received(1).GetEntity();
    }

    [Fact]
    public void Tracing_Get_Entity_Outside_Lambda_Environment()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(false);

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        var entity = tracing.GetEntity();

        // Assert
        Assert.Equal("Root", entity.Name);
    }

    [Fact]
    public void Tracing_Set_Entity()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var segment = new Segment("test");

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.SetEntity(segment);

        // Assert
        awsXray.TraceContext.Received(1).SetEntity(segment);
    }

    [Fact]
    public void Tracing_Add_Exception()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var ex = new ArgumentException("test");

        var awsXray = Substitute.For<IAWSXRayRecorder>();
        awsXray.When(x => x.AddException(ex));

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.AddException(ex);

        // Assert
        awsXray.Received(1).AddException(ex);
    }

    [Fact]
    public void Tracing_Add_Http_Information()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var key = "key";
        var value = "value";

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);

        tracing.AddHttpInformation(key, value);

        // Assert
        awsXray.Received(1).AddHttpInformation(key, value);
    }

    // Outside Lambda behavior is covered by integration tests

    // Annotation sanitization is now covered by XRayRecorderSanitizationTests.cs

    public void Dispose()
    {
        // Reset the singleton instance after each test to prevent test pollution
        XRayRecorder.ResetInstance();
    }
}