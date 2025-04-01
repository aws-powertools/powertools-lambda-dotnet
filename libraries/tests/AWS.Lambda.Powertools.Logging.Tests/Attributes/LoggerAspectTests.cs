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
using AWS.Lambda.Powertools.Logging.Tests.Handlers;
using AWS.Lambda.Powertools.Logging.Tests.Serializers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Attributes;

[Collection("Sequential")]
public class LoggerAspectTests : IDisposable
{
    static LoggerAspectTests()
    {
        ResetAllState();
    }
    
    public LoggerAspectTests()
    {
        // Start each test with clean state
        ResetAllState();
    }
    
    [Fact]
    public void OnEntry_ShouldInitializeLogger_WhenCalledWithValidArguments()
    {
        // Arrange
        var consoleOut = Substitute.For<ISystemWrapper>();

        var config = new PowertoolsLoggerConfiguration
        {
            Service = "TestService",
            MinimumLogLevel = LogLevel.Information,
            LogOutput = consoleOut
        };

        var logger = PowertoolsLoggerFactory.Create(config).CreatePowertoolsLogger();

        var instance = new object();
        var name = "TestMethod";
        var args = new object[] { new TestObject { FullName = "Powertools", Age = 20 } };
        var hostType = typeof(string);
        var method = typeof(TestHandlers).GetMethod("TestMethod");
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
                CorrelationIdPath = "/Age",
                ClearState = true
            }
        };

        // Act        
        var loggingAspect = new LoggingAspect(logger);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);

        // Assert
        consoleOut.Received().LogLine(Arg.Is<string>(s =>
            s.Contains(
                "\"Level\":\"Information\",\"Service\":\"TestService\",\"Name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"Message\":{\"FullName\":\"Powertools\",\"Age\":20,\"Headers\":null},\"SamplingRate\":0.5}")
            && s.Contains("\"CorrelationId\":\"20\"")
        ));
    }

    [Fact]
    public void OnEntry_ShouldLog_Event_When_EnvironmentVariable_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable(Constants.LoggerLogEventNameEnv, "true");
        var consoleOut = Substitute.For<ISystemWrapper>();
        
        var config = new PowertoolsLoggerConfiguration
        {
            Service = "TestService",
            MinimumLogLevel = LogLevel.Information,
            LogEvent = true,
            LogOutput = consoleOut
        };
    
        var logger = PowertoolsLoggerFactory.Create(config).CreatePowertoolsLogger();
    
        var instance = new object();
        var name = "TestMethod";
        var args = new object[] { new TestObject { FullName = "Powertools", Age = 20 } };
        var hostType = typeof(string);
        var method = typeof(TestHandlers).GetMethod("TestMethod");
        var returnType = typeof(string);
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                Service = "TestService",
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                LogLevel = LogLevel.Information,
                CorrelationIdPath = "/Age",
                ClearState = true
            }
        };
    
        // Act
        var loggingAspect = new LoggingAspect(logger);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);
    
        var updatedConfig = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();
    
        // Assert
        Assert.Equal("TestService", updatedConfig.Service);
        Assert.Equal(LoggerOutputCase.PascalCase, updatedConfig.LoggerOutputCase);
        Assert.Equal(0, updatedConfig.SamplingRate);
        Assert.True(updatedConfig.LogEvent);
    
        consoleOut.Received().LogLine(Arg.Is<string>(s =>
            s.Contains(
                "\"Level\":\"Information\",\"Service\":\"TestService\",\"Name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"Message\":{\"FullName\":\"Powertools\",\"Age\":20,\"Headers\":null}}")
            && s.Contains("\"CorrelationId\":\"20\"")
        ));
    }
    
    [Fact]
    public void OnEntry_Should_NOT_Log_Event_When_EnvironmentVariable_Set_But_Attribute_False()
    {
        // Arrange
        Environment.SetEnvironmentVariable(Constants.LoggerLogEventNameEnv, "true");
        var consoleOut = Substitute.For<ISystemWrapper>();
        
        var config = new PowertoolsLoggerConfiguration
        {
            Service = "TestService",
            MinimumLogLevel = LogLevel.Information,
            LogEvent = true,
            LogOutput = consoleOut
        };
    
        var logger = PowertoolsLoggerFactory.Create(config).CreatePowertoolsLogger();
    
        var instance = new object();
        var name = "TestMethod";
        var args = new object[] { new TestObject { FullName = "Powertools", Age = 20 } };
        var hostType = typeof(string);
        var method = typeof(TestHandlers).GetMethod("TestMethod");
        var returnType = typeof(string);
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                Service = "TestService",
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                LogLevel = LogLevel.Information,
                LogEvent = false,
                CorrelationIdPath = "/Age",
                ClearState = true
            }
        };
    
        // Act
        var loggingAspect = new LoggingAspect(logger);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);
    
        var updatedConfig = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();
    
        // Assert
        Assert.Equal("TestService", updatedConfig.Service);
        Assert.Equal(LoggerOutputCase.PascalCase, updatedConfig.LoggerOutputCase);
        Assert.Equal(0, updatedConfig.SamplingRate);
        Assert.True(updatedConfig.LogEvent);

        consoleOut.DidNotReceive().LogLine(Arg.Any<string>());
    }
    
    [Fact]
    public void OnEntry_ShouldLog_SamplingRate_When_EnvironmentVariable_Set()
    {
        // Arrange
        var consoleOut = Substitute.For<ISystemWrapper>();
    
        var config = new PowertoolsLoggerConfiguration
        {
            Service = "TestService",
            MinimumLogLevel = LogLevel.Information,
            SamplingRate = 0.5,
            LogOutput = consoleOut
        };
    
        var logger = PowertoolsLoggerFactory.Create(config).CreatePowertoolsLogger();
    
        var instance = new object();
        var name = "TestMethod";
        var args = new object[] { new TestObject { FullName = "Powertools", Age = 20 } };
        var hostType = typeof(string);
        var method = typeof(TestHandlers).GetMethod("TestMethod");
        var returnType = typeof(string);
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                Service = "TestService",
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                LogLevel = LogLevel.Information,
                LogEvent = true,
                CorrelationIdPath = "/Age",
                ClearState = true
            }
        };

        // Act
        var loggingAspect = new LoggingAspect(logger);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);
    
        // Assert
        var updatedConfig = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();
    
        Assert.Equal("TestService", updatedConfig.Service);
        Assert.Equal(LoggerOutputCase.PascalCase, updatedConfig.LoggerOutputCase);
        Assert.Equal(0.5, updatedConfig.SamplingRate);
    
        consoleOut.Received().LogLine(Arg.Is<string>(s =>
            s.Contains(
                "\"Level\":\"Information\",\"Service\":\"TestService\",\"Name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"Message\":{\"FullName\":\"Powertools\",\"Age\":20,\"Headers\":null},\"SamplingRate\":0.5}")
            && s.Contains("\"CorrelationId\":\"20\"")
        ));
    }
    
    [Fact]
    public void OnEntry_ShouldLogEvent_WhenLogEventIsTrue()
    {
        // Arrange
        var consoleOut = Substitute.For<ISystemWrapper>();
    
        var config = new PowertoolsLoggerConfiguration
        {
            Service = "TestService",
            MinimumLogLevel = LogLevel.Information,
            LogOutput = consoleOut,
        };
    
        var logger = PowertoolsLoggerFactory.Create(config).CreatePowertoolsLogger();
    
        var eventObject = new { testData = "test-data" };
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                LogEvent = true
            }
        };
    
        // Act
    
        var loggingAspect = new LoggingAspect(logger);
        loggingAspect.OnEntry(null, null, new object[] { eventObject }, null, null, null, triggers);
    
        // Assert
        consoleOut.Received().LogLine(Arg.Is<string>(s =>
            s.Contains(
                "\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":{\"test_data\":\"test-data\"}}")
        ));
    }
    
    [Fact]
    public void OnEntry_ShouldNot_Log_Info_When_LogLevel_Higher_EnvironmentVariable()
    {
        // Arrange
        var consoleOut = Substitute.For<ISystemWrapper>();
    
        var config = new PowertoolsLoggerConfiguration
        {
            Service = "TestService",
            MinimumLogLevel = LogLevel.Error,
            LogOutput = consoleOut
        };
    
        var logger = PowertoolsLoggerFactory.Create(config).CreatePowertoolsLogger();
    
        var instance = new object();
        var name = "TestMethod";
        var args = new object[] { new TestObject { FullName = "Powertools", Age = 20 } };
        var hostType = typeof(string);
        var method = typeof(TestHandlers).GetMethod("TestMethod");
        var returnType = typeof(string);
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                Service = "TestService",
                LoggerOutputCase = LoggerOutputCase.PascalCase,
    
                LogEvent = true,
                CorrelationIdPath = "/age"
            }
        };
    
        // Act
        var loggingAspect = new LoggingAspect(logger);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);
    
        var updatedConfig = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();
    
        // Assert
        Assert.Equal("TestService", updatedConfig.Service);
        Assert.Equal(LoggerOutputCase.PascalCase, updatedConfig.LoggerOutputCase);
    
        consoleOut.DidNotReceive().LogLine(Arg.Any<string>());
    }
    
    [Fact]
    public void OnEntry_Should_LogDebug_WhenSet_EnvironmentVariable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", "Debug");
    
        var consoleOut = Substitute.For<ISystemWrapper>();
        var config = new PowertoolsLoggerConfiguration
        {
            LogOutput = consoleOut
        };
    
        var instance = new object();
        var name = "TestMethod";
        var args = new object[]
        {
            new TestObject { FullName = "Powertools", Age = 20, Headers = new Header { MyRequestIdHeader = "test" } }
        };
        var hostType = typeof(string);
        var method = typeof(TestHandlers).GetMethod("TestMethod");
        var returnType = typeof(string);
        var triggers = new Attribute[]
        {
            new LoggingAttribute
            {
                Service = "TestService",
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                LogEvent = true,
                CorrelationIdPath = "/Headers/MyRequestIdHeader"
            }
        };
    
        var logger = PowertoolsLoggerFactory.Create(config).CreatePowertoolsLogger();

    
        // Act
        var loggingAspect = new LoggingAspect(logger);
        loggingAspect.OnEntry(instance, name, args, hostType, method, returnType, triggers);
    
        // Assert
        var updatedConfig = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();
    
        Assert.Equal("TestService", updatedConfig.Service);
        Assert.Equal(LoggerOutputCase.PascalCase, updatedConfig.LoggerOutputCase);
        Assert.Equal(LogLevel.Debug, updatedConfig.MinimumLogLevel);
    
        consoleOut.Received(1).LogLine(Arg.Is<string>(s =>
            s.Contains(
                "\"Level\":\"Debug\",\"Service\":\"TestService\",\"Name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"Message\":\"Skipping Lambda Context injection because ILambdaContext context parameter not found.\"}")));
    
        consoleOut.Received(1).LogLine(Arg.Is<string>(s =>
            s.Contains("\"CorrelationId\":\"test\"") &&
            s.Contains(
                "\"Message\":{\"FullName\":\"Powertools\",\"Age\":20,\"Headers\":{\"MyRequestIdHeader\":\"test\"}")
        ));
    }

    public void Dispose()
    {
        ResetAllState();
    }
    
    private static void ResetAllState()
    {
        // Clear environment variables
        Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_CASE", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);
        Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", null);

        // Reset all logging components
        LoggingAspect.ResetForTest();
        Logger.Reset();
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
        LoggerFactoryHolder.Reset();

        // Force default configuration
        var config = new PowertoolsLoggerConfiguration
        {
            MinimumLogLevel = LogLevel.Information,
            LoggerOutputCase = LoggerOutputCase.SnakeCase
        };
        PowertoolsLoggingBuilderExtensions.UpdateConfiguration(config);
    }
}