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
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.ApplicationLoadBalancerEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;
using AWS.Lambda.PowerTools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.PowerTools.Logging.Tests
{
    [Collection("Sequential")]
    public class LoggingAttributeTestWithLambdaContext
    {
        private static Mock<ILambdaContext> MockLambdaContext()
        {
            var lambdaContext = new Mock<ILambdaContext>();
            lambdaContext.Setup(c => c.FunctionName).Returns(Guid.NewGuid().ToString());
            lambdaContext.Setup(c => c.FunctionVersion).Returns(Guid.NewGuid().ToString());
            lambdaContext.Setup(c => c.MemoryLimitInMB).Returns(new Random().Next());
            lambdaContext.Setup(c => c.InvokedFunctionArn).Returns(Guid.NewGuid().ToString());
            lambdaContext.Setup(c => c.AwsRequestId).Returns(Guid.NewGuid().ToString());
            return lambdaContext;
        }
        
        [Fact]
        public void OnEntry_WhenHasLambdaContext_AppendLambdaContextKeys()
        {
            // Arrange
            
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            var lambdaContext = MockLambdaContext();

            var eventArg = new {Source = "Test"};
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object []
                {
                    eventArg,
                    lambdaContext.Object
                }
            };
            
            var handler = new LoggingAspectHandler(service, logLevel, null, true, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);

            var allKeys = LoggingAspectHandler.GetLambdaContextKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyFunctionName));
            Assert.Equal(allKeys[LoggingConstants.KeyFunctionName], lambdaContext.Object.FunctionName);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyFunctionVersion));
            Assert.Equal(allKeys[LoggingConstants.KeyFunctionVersion], lambdaContext.Object.FunctionVersion);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyFunctionMemorySize));
            Assert.Equal(allKeys[LoggingConstants.KeyFunctionMemorySize], lambdaContext.Object.MemoryLimitInMB);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyFunctionArn));
            Assert.Equal(allKeys[LoggingConstants.KeyFunctionArn], lambdaContext.Object.InvokedFunctionArn);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyFunctionRequestId));
            Assert.Equal(allKeys[LoggingConstants.KeyFunctionRequestId], lambdaContext.Object.AwsRequestId);
        }
    }
    
    [Collection("Sequential")]
    public class LoggingAttributeTestWithoutLambdaContext
    {
        [Fact]
        public void OnEntry_WhenLambdaContextDoesNotExist_IgnoresLambdaContext()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };
            
            var handler = new LoggingAspectHandler(service, logLevel, null, true, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);

            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyColdStart));
            Assert.True((bool) allKeys[LoggingConstants.KeyColdStart]);
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionName));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionVersion));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionMemorySize));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionArn));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionRequestId));
            
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.IsAny<string>()
                ), Times.Never);
        }
    }
    
    [Collection("Sequential")]
    public class LoggingAttributeTestWithoutLambdaContextDebug
    {
        [Fact]
        public void OnEntry_WhenLambdaContextDoesNotExist_IgnoresLambdaContextAndLogDebug()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };
            
            var handler = new LoggingAspectHandler(service, logLevel, null, true, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);

            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyColdStart));
            Assert.True((bool) allKeys[LoggingConstants.KeyColdStart]);
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionName));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionVersion));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionMemorySize));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionArn));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionRequestId));
            
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>(i => i == $"Skipping Lambda Context injection because ILambdaContext context parameter not found.")
                ), Times.Once);
        }
    }
    
    [Collection("Sequential")]
    public class LoggingAttributeTestWithoutEventArg
    {
        [Fact]
        public void OnEntry_WhenEventArgDoesNotExist_DoesNotLogEventArg()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };
            
            var handler = new LoggingAspectHandler(service, logLevel, null, true, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);
            
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.IsAny<string>()
                ), Times.Never);
        }
    }
    
    [Collection("Sequential")]
    public class LoggingAttributeTestWithoutEventArgDebug
    {
        [Fact]
        public void OnEntry_WhenEventArgDoesNotExist_DoesNotLogEventArgAndLogDebug()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };
            
            var handler = new LoggingAspectHandler(service, logLevel, null, true, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);

            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>(i => i ==  $"Skipping Event Log because event parameter not found.")
                ), Times.Once);
        }
    }
    
    [Collection("Sequential")]
    public class LoggingAttributeTestForClearContext
    {
        [Fact]
        public void OnExit_WhenHandler_ClearKeys()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };

            var handler = new LoggingAspectHandler(service, logLevel, null, true, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);
            
            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyColdStart));
            Assert.True((bool) allKeys[LoggingConstants.KeyColdStart]);
            
            handler.OnExit(eventArgs);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.False(Logger.GetAllKeys().Any());
        }
    }

    public abstract class LoggingAttributeTestWithEventArgCorrelationId
    {
        protected void OnEntry_WhenEventArgExists_CapturesCorrelationIdBase(string correlationId, string correlationIdPath, object eventArg)
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();

            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new[] { eventArg }
            };

            var handler = new LoggingAspectHandler(service, logLevel, null, false, correlationIdPath,
                true, configurations.Object,
                systemWrapper.Object);

            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);

            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);

            // Assert
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyCorrelationId));
            Assert.Equal((string) allKeys[LoggingConstants.KeyCorrelationId], correlationId);
        }
    }

    [Collection("Sequential")]
    public class LoggingAttributeTestWithEventArgCorrelationIdAPIGateway : LoggingAttributeTestWithEventArgCorrelationId
    {
        [Fact]
        public void OnEntry_WhenEventArgExists_CapturesCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString();
            OnEntry_WhenEventArgExists_CapturesCorrelationIdBase
            (
                correlationId,
                CorrelationIdPaths.ApiGatewayRest,
                new APIGatewayProxyRequest
                {
                    RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                    {
                        RequestId = correlationId
                    }
                }
            );
        }
    }
    
    [Collection("Sequential")]
    public class LoggingAttributeTestWithEventArgCorrelationIdApplicationLoadBalancer : LoggingAttributeTestWithEventArgCorrelationId
    {
        [Fact]
        public void OnEntry_WhenEventArgExists_CapturesCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString();
            OnEntry_WhenEventArgExists_CapturesCorrelationIdBase
            (
                correlationId,
                CorrelationIdPaths.ApplicationLoadBalancer,
                new ApplicationLoadBalancerRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        {"x-amzn-trace-id", correlationId}
                    }
                }
            );
        }
    }
}