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
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Serializers;
using AWS.Lambda.Powertools.Logging.Tests.Serializers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Attributes;

[Collection("Sequential")]
public class LoggerAspectTests : IDisposable
{
    private ISystemWrapper _mockSystemWrapper;
    private readonly IPowertoolsConfigurations _mockPowertoolsConfigurations;

    public LoggerAspectTests()
    {
        _mockSystemWrapper = Substitute.For<ISystemWrapper>();
        _mockPowertoolsConfigurations =  Substitute.For<IPowertoolsConfigurations>();
    }

    [Fact]
    public void OnEntry_ShouldInitializeLogger_WhenCalledWithValidArguments()
    {
        // Arrange
#if NET8_0_OR_GREATER
        // Add seriolization context for AOT
        var _ = new PowertoolsLambdaSerializer(TestJsonContext.Default);
#endif

        var instance = new object();
        var name = "TestMethod";
        var args = new object[] { new TestObject { FullName = "Powertools", Age = 20 } };
        var hostType = typeof(string);
        var method = typeof(TestClass).GetMethod("TestMethod");
        var returnType = typeof(string);
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                Service = "TestService",
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                SamplingRate = 0.5,
                LogLevel = LogLevel.Information,
                LogEvent = true,
                CorrelationIdPath = "/age",
                ClearState = true
            }
        };

        _mockSystemWrapper.GetRandom().Returns(0.7);

        // Act        
        var loggingAspect = new LoggingAspect(_mockPowertoolsConfigurations, _mockSystemWrapper);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);

        // Assert
        _mockSystemWrapper.Received().LogLine(Arg.Is<string>(s =>
            s.Contains(
                "\"Level\":\"Information\",\"Service\":\"TestService\",\"Name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"Message\":{\"FullName\":\"Powertools\",\"Age\":20,\"Headers\":null},\"SamplingRate\":0.5}")
            && s.Contains("\"CorrelationId\":\"20\"")
        ));
    }
    
    [Fact]
    public void OnEntry_ShouldLog_Event_When_EnvironmentVariable_Set()
    {
        // Arrange
#if NET8_0_OR_GREATER

        // Add seriolization context for AOT
        var _ = new PowertoolsLambdaSerializer(TestJsonContext.Default);
#endif

        var instance = new object();
        var name = "TestMethod";
        var args = new object[] { new TestObject { FullName = "Powertools", Age = 20 } };
        var hostType = typeof(string);
        var method = typeof(TestClass).GetMethod("TestMethod");
        var returnType = typeof(string);
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                Service = "TestService",
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                LogLevel = LogLevel.Information,
                LogEvent = false,
                CorrelationIdPath = "/age",
                ClearState = true
            }
        };
        
        // Env returns true
        _mockPowertoolsConfigurations.LoggerLogEvent.Returns(true);

        // Act
        var loggingAspect = new LoggingAspect(_mockPowertoolsConfigurations, _mockSystemWrapper);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);

        // Assert
        var config = _mockPowertoolsConfigurations.CurrentConfig();
        Assert.NotNull(Logger.LoggerProvider);
        Assert.Equal("TestService", config.Service);
        Assert.Equal(LoggerOutputCase.PascalCase, config.LoggerOutputCase);
        Assert.Equal(0, config.SamplingRate);

        _mockSystemWrapper.Received().LogLine(Arg.Is<string>(s =>
            s.Contains(
                "\"Level\":\"Information\",\"Service\":\"TestService\",\"Name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"Message\":{\"FullName\":\"Powertools\",\"Age\":20,\"Headers\":null}}")
            && s.Contains("\"CorrelationId\":\"20\"")
        ));
    }
    
    [Fact]
    public void OnEntry_ShouldLog_SamplingRate_When_EnvironmentVariable_Set()
    {
        // Arrange
#if NET8_0_OR_GREATER

        // Add seriolization context for AOT
        var _ = new PowertoolsLambdaSerializer(TestJsonContext.Default);
#endif

        var instance = new object();
        var name = "TestMethod";
        var args = new object[] { new TestObject { FullName = "Powertools", Age = 20 } };
        var hostType = typeof(string);
        var method = typeof(TestClass).GetMethod("TestMethod");
        var returnType = typeof(string);
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                Service = "TestService",
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                LogLevel = LogLevel.Information,
                LogEvent = true,
                CorrelationIdPath = "/age",
                ClearState = true
            }
        };

        // Env returns true
        _mockPowertoolsConfigurations.LoggerSampleRate.Returns(0.5);

        // Act
        var loggingAspect = new LoggingAspect(_mockPowertoolsConfigurations, _mockSystemWrapper);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);

        // Assert
        var config = _mockPowertoolsConfigurations.CurrentConfig();
        Assert.NotNull(Logger.LoggerProvider);
        Assert.Equal("TestService", config.Service);
        Assert.Equal(LoggerOutputCase.PascalCase, config.LoggerOutputCase);
        Assert.Equal(0.5, config.SamplingRate);

        _mockSystemWrapper.Received().LogLine(Arg.Is<string>(s =>
            s.Contains(
                "\"Level\":\"Information\",\"Service\":\"TestService\",\"Name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"Message\":{\"FullName\":\"Powertools\",\"Age\":20,\"Headers\":null},\"SamplingRate\":0.5}")
            && s.Contains("\"CorrelationId\":\"20\"")
        ));
    }

    [Fact]
    public void OnEntry_ShouldLogEvent_WhenLogEventIsTrue()
    {
        // Arrange
        var eventObject = new { testData = "test-data" };
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                LogEvent = true
            }
        };
    
        // Act

        var loggingAspect = new LoggingAspect(_mockPowertoolsConfigurations, _mockSystemWrapper);
        loggingAspect.OnEntry(null, null, new object[] { eventObject }, null, null, null, triggers);
    
        // Assert
        _mockSystemWrapper.Received().LogLine(Arg.Is<string>(s =>
            s.Contains(
                "\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":{\"test_data\":\"test-data\"}}")
        ));
    }
    
    [Fact]
    public void OnEntry_ShouldNot_Log_Info_When_LogLevel_Higher_EnvironmentVariable()
    {
        // Arrange
#if NET8_0_OR_GREATER

        // Add seriolization context for AOT
        var _ = new PowertoolsLambdaSerializer(TestJsonContext.Default);
#endif

        var instance = new object();
        var name = "TestMethod";
        var args = new object[] { new TestObject { FullName = "Powertools", Age = 20 } };
        var hostType = typeof(string);
        var method = typeof(TestClass).GetMethod("TestMethod");
        var returnType = typeof(string);
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                Service = "TestService",
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                //LogLevel = LogLevel.Information,
                LogEvent = true,
                CorrelationIdPath = "/age"
            }
        };

        // Env returns true
        _mockPowertoolsConfigurations.LogLevel.Returns(LogLevel.Error.ToString());

        // Act
        var loggingAspect = new LoggingAspect(_mockPowertoolsConfigurations, _mockSystemWrapper);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);

        // Assert
        var config = _mockPowertoolsConfigurations.CurrentConfig();
        Assert.NotNull(Logger.LoggerProvider);
        Assert.Equal("TestService", config.Service);
        Assert.Equal(LoggerOutputCase.PascalCase, config.LoggerOutputCase);

        _mockSystemWrapper.DidNotReceive().LogLine(Arg.Any<string>());
    }

    public void Dispose()
    {
        LoggingAspect.ResetForTest();
    }
}