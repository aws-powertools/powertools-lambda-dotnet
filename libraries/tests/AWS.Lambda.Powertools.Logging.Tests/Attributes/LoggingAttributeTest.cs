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
using System.IO;
using System.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.ApplicationLoadBalancerEvents;
using Amazon.Lambda.CloudWatchEvents.S3Events;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Serializers;
using AWS.Lambda.Powertools.Logging.Tests.Utilities;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Attributes
{
    [Collection("Attribute Tests")]
    public class LoggingAttributeTests : IDisposable
    {
        private TestClass _testClass;

        public LoggingAttributeTests()
        {
            _testClass = new TestClass();
        }

        [Fact]
        public void OnEntry_WhenLambdaContextDoesNotExist_IgnoresLambdaContext()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);

            // Act
            _testClass.TestMethod();

            // Assert
            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);

            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyColdStart));
            //Assert.True((bool)allKeys[LoggingConstants.KeyColdStart]);
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionName));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionVersion));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionMemorySize));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionArn));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionRequestId));

            consoleOut.DidNotReceive().WriteLine(Arg.Any<string>());
        }

        [Fact]
        public void OnEntry_WhenLambdaContextDoesNotExist_IgnoresLambdaContextAndLogDebug()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);

            // Act
            _testClass.TestMethodDebug();

            // Assert
            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);

            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyColdStart));
            //Assert.True((bool)allKeys[LoggingConstants.KeyColdStart]);
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionName));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionVersion));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionMemorySize));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionArn));
            Assert.False(allKeys.ContainsKey(LoggingConstants.KeyFunctionRequestId));

            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i =>
                    i == $"Skipping Lambda Context injection because ILambdaContext context parameter not found.")
            );
        }

        [Fact]
        public void OnEntry_WhenEventArgDoesNotExist_DoesNotLogEventArg()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);

            // Act
            _testClass.LogEvent();

            consoleOut.DidNotReceive().WriteLine(
                Arg.Any<string>()
            );
        }

        [Fact]
        public void OnEntry_WhenEventArgDoesNotExist_DoesNotLogEventArgAndLogDebug()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);

            // Act
            _testClass.LogEventDebug();

            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i => i == "Skipping Event Log because event parameter not found.")
            );
        }

        [Fact]
        public void OnExit_WhenHandler_ClearState_Enabled_ClearKeys()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);

            // Act
            _testClass.ClearState();

            Assert.NotNull(Logger.LoggerProvider);
            Assert.False(Logger.GetAllKeys().Any());
        }

        [Theory]
        [InlineData(CorrelationIdPaths.ApiGatewayRest)]
        [InlineData(CorrelationIdPaths.ApplicationLoadBalancer)]
        [InlineData(CorrelationIdPaths.EventBridge)]
        [InlineData("/headers/my_request_id_header")]
        public void OnEntry_WhenEventArgExists_CapturesCorrelationId(string correlationIdPath)
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();

#if NET8_0_OR_GREATER

            // Add seriolization context for AOT
            var _ = new PowertoolsLambdaSerializer(Utilities.TestJsonContext.Default);
#endif

            // Act
            switch (correlationIdPath)
            {
                case CorrelationIdPaths.ApiGatewayRest:
                    _testClass.CorrelationApiGatewayProxyRequest(new APIGatewayProxyRequest
                    {
                        RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                        {
                            RequestId = correlationId
                        }
                    });
                    break;
                case CorrelationIdPaths.ApplicationLoadBalancer:
                    _testClass.CorrelationApplicationLoadBalancerRequest(new ApplicationLoadBalancerRequest
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "x-amzn-trace-id", correlationId }
                        }
                    });
                    break;
                case CorrelationIdPaths.EventBridge:
                    _testClass.CorrelationCloudWatchEvent(new S3ObjectCreateEvent
                    {
                        Id = correlationId
                    });
                    break;
                case "/headers/my_request_id_header":
                    _testClass.CorrelationIdFromString(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
            }

            // Assert
            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);

            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyCorrelationId));
            Assert.Equal((string)allKeys[LoggingConstants.KeyCorrelationId], correlationId);
        }

        [Theory]
        [InlineData(LoggerOutputCase.SnakeCase)]
        [InlineData(LoggerOutputCase.PascalCase)]
        [InlineData(LoggerOutputCase.CamelCase)]
        public void When_Capturing_CorrelationId_Converts_To_Case(LoggerOutputCase outputCase)
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();

#if NET8_0_OR_GREATER

            // Add seriolization context for AOT
            var _ = new PowertoolsLambdaSerializer(Utilities.TestJsonContext.Default);
#endif

            // Act
            switch (outputCase)
            {
                case LoggerOutputCase.CamelCase:
                    _testClass.CorrelationIdFromStringCamel(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
                case LoggerOutputCase.PascalCase:
                    _testClass.CorrelationIdFromStringPascal(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
                case LoggerOutputCase.SnakeCase:
                    _testClass.CorrelationIdFromStringSnake(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
            }

            // Assert
            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);

            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyCorrelationId));
            Assert.Equal((string)allKeys[LoggingConstants.KeyCorrelationId], correlationId);
        }
        
        [Theory]
        [InlineData(LoggerOutputCase.SnakeCase)]
        [InlineData(LoggerOutputCase.PascalCase)]
        [InlineData(LoggerOutputCase.CamelCase)]
        public void When_Capturing_CorrelationId_Converts_To_Case_From_Environment_Var(LoggerOutputCase outputCase)
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();

#if NET8_0_OR_GREATER

            // Add seriolization context for AOT
            var _ = new PowertoolsLambdaSerializer(Utilities.TestJsonContext.Default);
#endif

            // Act
            switch (outputCase)
            {
                case LoggerOutputCase.CamelCase:
                    Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_CASE", "CamelCase");
                    _testClass.CorrelationIdFromStringCamelEnv(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
                case LoggerOutputCase.PascalCase:
                    Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_CASE", "PascalCase");
                    _testClass.CorrelationIdFromStringPascalEnv(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
                case LoggerOutputCase.SnakeCase:
                    _testClass.CorrelationIdFromStringSnakeEnv(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
            }

            // Assert
            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);

            Assert.True(allKeys.ContainsKey(LoggingConstants.KeyCorrelationId));
            Assert.Equal((string)allKeys[LoggingConstants.KeyCorrelationId], correlationId);
        }
        
        [Fact]
        public void When_Setting_SamplingRate_Should_Add_Key()
        {
            // Arrange
            var consoleOut = new StringWriter();
            SystemWrapper.Instance.SetOut(consoleOut);
        
            // Act
            _testClass.HandlerSamplingRate();
        
            // Assert
        
            var st = consoleOut.ToString().Split(Environment.NewLine);
            Assert.Contains("\"message\":\"test\",\"samplingRate\":0.5", st[st.Length -2]);
        }
        
        [Fact]
        public void When_Setting_Service_Should_Update_Key()
        {
            // Arrange
            var consoleOut = new StringWriter();
            SystemWrapper.Instance.SetOut(consoleOut);
        
            // Act
            _testClass.HandlerService();
        
            // Assert
        
            var st = consoleOut.ToString();
            Assert.Contains("\"level\":\"Information\",\"service\":\"test\",\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":\"test\"", st);
        }

        public void Dispose()
        {
            LoggingAspect.ResetForTest();
        }
    }
}