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

    [Fact]
    public void Tracing_All_When_Outside_Lambda()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(false);

        var awsXray = Substitute.For<IAWSXRayRecorder>();
        var tracing = new XRayRecorder(awsXray, conf);

        // Act
        tracing.AddHttpInformation(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        tracing.AddException(new AggregateException("Test"));
        tracing.SetEntity(new Segment("test"));
        tracing.EndSubsegment();
        tracing.AddMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        tracing.AddAnnotation(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        tracing.SetNamespace(Guid.NewGuid().ToString());
        tracing.BeginSubsegment(Guid.NewGuid().ToString());

        // Assert
        awsXray.DidNotReceive().AddHttpInformation(Arg.Any<string>(), Arg.Any<string>());
        awsXray.DidNotReceive().AddException(Arg.Any<Exception>());
        awsXray.DidNotReceive().TraceContext.SetEntity(Arg.Any<Entity>());
        awsXray.DidNotReceive().EndSubsegment();
        awsXray.DidNotReceive().AddMetadata(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        awsXray.DidNotReceive().AddAnnotation(Arg.Any<string>(), Arg.Any<string>());
        awsXray.DidNotReceive().SetNamespace(Arg.Any<string>());
        awsXray.DidNotReceive().BeginSubsegment(Arg.Any<string>());
    }

    [Theory]
    [InlineData("string", "string")] // string should remain unchanged
    [InlineData(true, true)] // bool should remain unchanged
    [InlineData(42, 42)] // int should remain unchanged
    [InlineData(42L, 42L)] // long should remain unchanged
    [InlineData(3.14, 3.14)] // double should remain unchanged
    [InlineData(3.14f, 3.14f)] // float should remain unchanged
    [InlineData(null, null)] // null should remain unchanged
    public void Tracing_Add_Annotation_Supported_Types_Remain_Unchanged(object input, object expected)
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);
        tracing.AddAnnotation("key", input);

        // Assert
        awsXray.Received(1).AddAnnotation("key", expected);
    }

    [Theory]
    [InlineData((byte)255)] // byte
    [InlineData((short)32767)] // short
    [InlineData((uint)42)] // uint
    [InlineData((ulong)42)] // ulong
    [InlineData((ushort)42)] // ushort
    [InlineData((sbyte)127)] // sbyte
    public void Tracing_Add_Annotation_Unsupported_ValueTypes_Converted_To_String(object input)
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);
        tracing.AddAnnotation("key", input);

        // Assert
        awsXray.Received(1).AddAnnotation("key", input.ToString());
    }

    [Fact]
    public void Tracing_Add_Annotation_Decimal_Converted_To_String()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();
        var decimalValue = 13.14m;

        // Act
        var tracing = new XRayRecorder(awsXray, conf);
        tracing.AddAnnotation("key", decimalValue);

        // Assert
        awsXray.Received(1).AddAnnotation("key", "13.14");
    }

    [Fact]
    public void Tracing_Add_Annotation_DateTime_Converted_To_String()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();
        var dateTime = DateTime.Now;

        // Act
        var tracing = new XRayRecorder(awsXray, conf);
        tracing.AddAnnotation("key", dateTime);

        // Assert
        awsXray.Received(1).AddAnnotation("key", dateTime.ToString());
    }

    [Fact]
    public void Tracing_Add_Annotation_Guid_Converted_To_String()
    {
        // Arrange
        var conf = Substitute.For<IPowertoolsConfigurations>();
        conf.IsLambdaEnvironment.Returns(true);

        var awsXray = Substitute.For<IAWSXRayRecorder>();
        var guid = Guid.NewGuid();

        // Act
        var tracing = new XRayRecorder(awsXray, conf);
        tracing.AddAnnotation("key", guid);

        // Assert
        awsXray.Received(1).AddAnnotation("key", guid.ToString());
    }

    public void Dispose()
    {
        // Reset the singleton instance after each test to prevent test pollution
        XRayRecorder.ResetInstance();
    }
}