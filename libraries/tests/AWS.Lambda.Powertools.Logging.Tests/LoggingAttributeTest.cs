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
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.Powertools.Logging.Tests
{
    [Collection("Sequential")]
    public class LoggingAttributeTestWithLambdaContext
    {
        private class TestLambdaContext : PowertoolsLambdaContext
        {
            protected internal TestLambdaContext(string awsRequestId, string functionName, string functionVersion, string invokedFunctionArn, string logGroupName, string logStreamName, int memoryLimitInMB) : base(awsRequestId, functionName, functionVersion, invokedFunctionArn, logGroupName, logStreamName, memoryLimitInMB) { }
        }
        
        private static TestLambdaContext NewLambdaContext()
        {
            return new TestLambdaContext
            (
                awsRequestId: Guid.NewGuid().ToString(),
                functionName: Guid.NewGuid().ToString(),
                functionVersion: Guid.NewGuid().ToString(),
                invokedFunctionArn: Guid.NewGuid().ToString(),
                logGroupName: Guid.NewGuid().ToString(),
                logStreamName: Guid.NewGuid().ToString(),
                memoryLimitInMB: new Random().Next()
            );
        }
        
        [Fact]
        public void OnEntry_WhenHasLambdaContext_AppendLambdaContextKeys()
        {
            // Arrange
            
            var methodName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            
            var configurations = new Mock<IPowertoolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            var lambdaContext = NewLambdaContext();

            var eventArg = new {Source = "Test"};
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object []
                {
                    eventArg,
                    lambdaContext
                }
            };
            
            var handler = new LoggingAspectHandler(service, logLevel, null, true, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.NotNull(PowertoolsLambdaContext.Instance);
            Assert.Equal(PowertoolsLambdaContext.Instance.AwsRequestId, lambdaContext.AwsRequestId);
            Assert.Equal(PowertoolsLambdaContext.Instance.FunctionName, lambdaContext.FunctionName);
            Assert.Equal(PowertoolsLambdaContext.Instance.FunctionVersion, lambdaContext.FunctionVersion);
            Assert.Equal(PowertoolsLambdaContext.Instance.InvokedFunctionArn, lambdaContext.InvokedFunctionArn);
            Assert.Equal(PowertoolsLambdaContext.Instance.LogGroupName, lambdaContext.LogGroupName);
            Assert.Equal(PowertoolsLambdaContext.Instance.LogStreamName, lambdaContext.LogStreamName);
            Assert.Equal(PowertoolsLambdaContext.Instance.MemoryLimitInMB, lambdaContext.MemoryLimitInMB);
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
            
            var configurations = new Mock<IPowertoolsConfigurations>();
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
            
            var configurations = new Mock<IPowertoolsConfigurations>();
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
            
            var configurations = new Mock<IPowertoolsConfigurations>();
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
            
            var configurations = new Mock<IPowertoolsConfigurations>();
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
            
            var configurations = new Mock<IPowertoolsConfigurations>();
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

            var configurations = new Mock<IPowertoolsConfigurations>();
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