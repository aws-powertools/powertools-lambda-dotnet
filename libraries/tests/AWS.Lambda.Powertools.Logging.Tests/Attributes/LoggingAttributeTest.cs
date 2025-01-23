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
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Serializers;
using AWS.Lambda.Powertools.Logging.Tests.Handlers;
using AWS.Lambda.Powertools.Logging.Tests.Serializers;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Attributes
{
    [Collection("Sequential")]
    public class LoggingAttributeTests : IDisposable
    {
        private TestHandlers _testHandlers;

        public LoggingAttributeTests()
        {
            _testHandlers = new TestHandlers();
        }

        [Fact]
        public void OnEntry_WhenLambdaContextDoesNotExist_IgnoresLambdaContext()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);

            // Act
            _testHandlers.TestMethod();

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
            _testHandlers.TestMethodDebug();

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
            _testHandlers.LogEventNoArgs();

            consoleOut.DidNotReceive().WriteLine(
                Arg.Any<string>()
            );
        }
        
        [Fact]
        public void OnEntry_WhenEventArgExist_LogEvent()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);
            var correlationId = Guid.NewGuid().ToString();
                
#if NET8_0_OR_GREATER

            // Add seriolization context for AOT
            PowertoolsLoggingSerializer.AddSerializerContext(TestJsonContext.Default);
#endif
            var context = new TestLambdaContext()
            {
                FunctionName = "PowertoolsLoggingSample-HelloWorldFunction-Gg8rhPwO7Wa1"
            };

            var testObj = new TestObject
            {
                Headers = new Header
                {
                    MyRequestIdHeader = correlationId
                }
            };
            
            // Act
            _testHandlers.LogEvent(testObj, context);

            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i => i.Contains("FunctionName\":\"PowertoolsLoggingSample-HelloWorldFunction-Gg8rhPwO7Wa1"))
            );
        }
        
        [Fact]
        public void OnEntry_WhenEventArgExist_LogEvent_False_Should_Not_Log()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);
            
#if NET8_0_OR_GREATER

            // Add seriolization context for AOT
            PowertoolsLoggingSerializer.AddSerializerContext(TestJsonContext.Default);
#endif
            var context = new TestLambdaContext()
            {
                FunctionName = "PowertoolsLoggingSample-HelloWorldFunction-Gg8rhPwO7Wa1"
            };
            
            // Act
            _testHandlers.LogEventFalse(context);

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
            _testHandlers.LogEventDebug();

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
            _testHandlers.ClearState();

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
            PowertoolsLoggingSerializer.AddSerializerContext(TestJsonContext.Default);
#endif

            // Act
            switch (correlationIdPath)
            {
                case CorrelationIdPaths.ApiGatewayRest:
                    _testHandlers.CorrelationApiGatewayProxyRequest(new APIGatewayProxyRequest
                    {
                        RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                        {
                            RequestId = correlationId
                        }
                    });
                    break;
                case CorrelationIdPaths.ApplicationLoadBalancer:
                    _testHandlers.CorrelationApplicationLoadBalancerRequest(new ApplicationLoadBalancerRequest
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "x-amzn-trace-id", correlationId }
                        }
                    });
                    break;
                case CorrelationIdPaths.EventBridge:
                    _testHandlers.CorrelationCloudWatchEvent(new S3ObjectCreateEvent
                    {
                        Id = correlationId
                    });
                    break;
                case "/headers/my_request_id_header":
                    _testHandlers.CorrelationIdFromString(new TestObject
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
            PowertoolsLoggingSerializer.AddSerializerContext(TestJsonContext.Default);
#endif

            // Act
            switch (outputCase)
            {
                case LoggerOutputCase.CamelCase:
                    _testHandlers.CorrelationIdFromStringCamel(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
                case LoggerOutputCase.PascalCase:
                    _testHandlers.CorrelationIdFromStringPascal(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
                case LoggerOutputCase.SnakeCase:
                    _testHandlers.CorrelationIdFromStringSnake(new TestObject
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
            PowertoolsLoggingSerializer.AddSerializerContext(TestJsonContext.Default);
#endif

            // Act
            switch (outputCase)
            {
                case LoggerOutputCase.CamelCase:
                    Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_CASE", "CamelCase");
                    _testHandlers.CorrelationIdFromStringCamelEnv(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
                case LoggerOutputCase.PascalCase:
                    Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_CASE", "PascalCase");
                    _testHandlers.CorrelationIdFromStringPascalEnv(new TestObject
                    {
                        Headers = new Header
                        {
                            MyRequestIdHeader = correlationId
                        }
                    });
                    break;
                case LoggerOutputCase.SnakeCase:
                    _testHandlers.CorrelationIdFromStringSnakeEnv(new TestObject
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
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);
        
            // Act
            _testHandlers.HandlerSamplingRate();
        
            // Assert
        
            consoleOut.Received().WriteLine(
                Arg.Is<string>(i => i.Contains("\"message\":\"test\",\"samplingRate\":0.5"))
            );
        }
        
        [Fact]
        public void When_Setting_Service_Should_Update_Key()
        {
            // Arrange
            var consoleOut = new StringWriter();
            SystemWrapper.Instance.SetOut(consoleOut);
        
            // Act
            _testHandlers.HandlerService();
        
            // Assert
        
            var st = consoleOut.ToString();
            Assert.Contains("\"level\":\"Information\",\"service\":\"test\",\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":\"test\"", st);
        }
        
        [Fact]
        public void When_Setting_LogLevel_Should_Update_LogLevel()
        {
            // Arrange
            var consoleOut = new StringWriter();
            SystemWrapper.Instance.SetOut(consoleOut);

            // Act
            _testHandlers.TestLogLevelCritical();
        
            // Assert
        
            var st = consoleOut.ToString();
            Assert.Contains("\"level\":\"Critical\"", st);
        }
        
        [Fact]
        public void When_Setting_LogLevel_HigherThanInformation_Should_Not_LogEvent()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);
            var context = new TestLambdaContext()
            {
                FunctionName = "PowertoolsLoggingSample-HelloWorldFunction-Gg8rhPwO7Wa1"
            };
            
            // Act
            _testHandlers.TestLogLevelCriticalLogEvent(context);
        
            // Assert
            consoleOut.DidNotReceive().WriteLine(Arg.Any<string>());
        }
        
        [Fact]
        public void When_LogLevel_Debug_Should_Log_Message_When_No_Context_And_LogEvent_True()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);
            
            // Act
            _testHandlers.TestLogEventWithoutContext();
        
            // Assert
            consoleOut.Received(1).WriteLine(Arg.Is<string>(s => s == "Skipping Event Log because event parameter not found."));
        }
        
        [Fact]
        public void Should_Log_When_Not_Using_Decorator()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);
            
            var test = new TestHandlers();
            
            // Act
            test.TestLogNoDecorator();

            // Assert
            consoleOut.Received().WriteLine(
                Arg.Is<string>(i => i.Contains("\"level\":\"Information\",\"service\":\"service_undefined\",\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":\"test\"}"))
            );
        }
        
        public void Dispose()
        {
            Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_CASE", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "");
            LoggingAspect.ResetForTest();
            PowertoolsLoggingSerializer.ClearOptions();
        }
    }
    
    [Collection("A Sequential")]
    public class ServiceTests : IDisposable
    {
        private readonly TestServiceHandler _testHandler;
        
        public ServiceTests()
        {
            _testHandler = new TestServiceHandler();
        }
        
        [Fact]
        public void When_Setting_Service_Should_Override_Env()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);

            // Act
            _testHandler.LogWithEnv();
            _testHandler.Handler();
        
            // Assert
        
            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i => i.Contains("\"level\":\"Information\",\"service\":\"Environment Service\",\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":\"Service: Environment Service\""))
            );
            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i => i.Contains("\"level\":\"Information\",\"service\":\"Attribute Service\",\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":\"Service: Attribute Service\""))
            );            
        }
        
        [Fact]
        public void When_Setting_Service_Should_Override_Env_And_Empty()
        {
            // Arrange
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);

            // Act
            _testHandler.LogWithAndWithoutEnv();
            _testHandler.Handler();
        
            // Assert
            
            consoleOut.Received(2).WriteLine(
                Arg.Is<string>(i => i.Contains("\"level\":\"Information\",\"service\":\"service_undefined\",\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":\"Service: service_undefined\""))
            );
            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i => i.Contains("\"level\":\"Information\",\"service\":\"Attribute Service\",\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"message\":\"Service: Attribute Service\""))
            );            
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("POWERTOOLS_LOGGER_CASE", "");
            Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "");
            LoggingAspect.ResetForTest();
            PowertoolsLoggingSerializer.ClearOptions();
        }
    }
}