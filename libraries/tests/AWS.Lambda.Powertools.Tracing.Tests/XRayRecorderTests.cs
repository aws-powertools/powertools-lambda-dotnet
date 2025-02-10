/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Tracing.Internal;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests;

// This has to be the last tests to run otherwise it will keep state and fail other random tests
[Collection("Sequential")]
public class XRayRecorderTests
{
    [Fact]
    public void Tracing_Set_Execution_Environment_Context()
    {
        // Arrange
        var assemblyName = "AWS.Lambda.Powertools.Tracing";
        var assemblyVersion = "1.0.0";

        var env = Substitute.For<IPowertoolsEnvironment>();
        env.GetAssemblyName(Arg.Any<XRayRecorder>()).Returns(assemblyName);
        env.GetAssemblyVersion(Arg.Any<XRayRecorder>()).Returns(assemblyVersion);

        var conf = new PowertoolsConfigurations(new SystemWrapper(env));
        var awsXray = Substitute.For<IAWSXRayRecorder>();

        // Act
        var xRayRecorder = new XRayRecorder(awsXray, conf);

        // Assert
        env.Received(1).SetEnvironmentVariable(
            "AWS_EXECUTION_ENV", $"{Constants.FeatureContextIdentifier}/Tracing/{assemblyVersion}"
        );

        env.Received(1).GetEnvironmentVariable(
            "AWS_EXECUTION_ENV"
        );

        Assert.NotNull(xRayRecorder);
    }

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
}