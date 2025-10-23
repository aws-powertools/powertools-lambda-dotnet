using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.ApplicationLoadBalancerEvents;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.CloudWatchEvents.S3Events;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Core;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Tests.Handlers;
using AWS.Lambda.Powertools.Logging.Tests.Serializers;
using Microsoft.Extensions.Logging;
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
        public void OnEntry_WhenLambdaContextDoesNotExist_IgnoresLambdaContext_No_Debug()
        {
            // Arrange
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            // Act
            _testHandlers.TestMethod();
    
            // Assert
            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
    
            Assert.Empty(allKeys);
            
            var st = stringWriter.ToString();
            Assert.Empty(st);
        }
    
        [Fact]
        public void OnEntry_WhenLambdaContextDoesNotExist_IgnoresLambdaContextAndLogDebug()
        {
            // Arrange
            var consoleOut = GetConsoleOutput();
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
            
            // Act
            _testHandlers.TestMethodDebug();
    
            // Assert
            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
    
            Assert.Empty(allKeys);
    
            var st = stringWriter.ToString();
            Assert.Contains("Skipping Lambda Context injection because ILambdaContext context parameter not found", st);
        }
    
        [Fact]
        public void OnEntry_WhenEventArgDoesNotExist_DoesNotLogEventArg()
        {
            // Arrange
            var consoleOut = GetConsoleOutput();
    
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
            var consoleOut = GetConsoleOutput();
            var correlationId = Guid.NewGuid().ToString();
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
            
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
            var consoleOut = GetConsoleOutput();
    
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
            var consoleOut = GetConsoleOutput();
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
            
            // Act
            _testHandlers.LogEventDebug();
    
            // Assert
            var st = stringWriter.ToString();
            Assert.Contains("Skipping Event Log because event parameter not found.", st);
            Assert.Contains("Skipping Lambda Context injection because ILambdaContext context parameter not found", st);
        }
    
        [Fact]
        public void OnExit_WhenHandler_ClearState_Enabled_ClearKeys()
        {
            // Act
            _testHandlers.ClearState();
    
            Assert.False(Logger.GetAllKeys().Any());
        }
    
        [Theory]
        [InlineData(CorrelationIdPaths.ApiGatewayRest)]
        [InlineData(CorrelationIdPaths.ApplicationLoadBalancer)]
        [InlineData(CorrelationIdPaths.EventBridge)]
        [InlineData("/headers/my_request_id_header")]
        [InlineData("/detail/correlationId")]
        public void OnEntry_WhenEventArgExists_CapturesCorrelationId(string correlationIdPath)
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
    
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
                case "/detail/correlationId":
                    _testHandlers.CorrelationCloudWatchEventCustomPath(new CloudWatchEvent<CwEvent>
                    {
                        Detail = new CwEvent
                        {
                            CorrelationId = correlationId
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
            var consoleOut = GetConsoleOutput();
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
            
            // Act
            _testHandlers.HandlerSamplingRate();
        
            // Assert
            consoleOut.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"level\":\"Information\"") && 
                s.Contains("\"service\":\"service_undefined\"") && 
                s.Contains("\"name\":\"AWS.Lambda.Powertools.Logging.Logger\"") && 
                s.Contains("\"message\":\"test\"") &&
                s.Contains("\"samplingRate\":0.5")
            ));
        }
        
        [Fact]
        public void CorrelationId_Property_Should_Return_CorrelationId()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            
            // Act
            _testHandlers.CorrelationCloudWatchEventCustomPath(new CloudWatchEvent<CwEvent>
            {
                Detail = new CwEvent
                {
                    CorrelationId = correlationId
                }
            });
            
            // Assert - Static Logger property
            Assert.Equal(correlationId, Logger.CorrelationId);
        }
        
        [Fact]
        public void CorrelationId_Extension_Should_Return_CorrelationId_Via_ILogger()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            
            // Act
            _testHandlers.CorrelationIdExtensionTest(new CloudWatchEvent<CwEvent>
            {
                Detail = new CwEvent
                {
                    CorrelationId = correlationId
                }
            });
            
            // Assert - The test handler will verify the extension method works
            Assert.Equal(correlationId, Logger.CorrelationId);
        }
        
        [Fact]
        public void CorrelationId_Should_Return_Null_When_Not_Set()
        {
            // Arrange - no correlation ID set
            
            // Act & Assert
            Assert.Null(Logger.CorrelationId);
        }
        
        [Fact]
        public void When_Setting_Service_Should_Update_Key()
        {
            // Arrange
            var consoleOut = new TestLoggerOutput();
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
            
            // Act
            _testHandlers.HandlerService();
        
            // Assert
        
            var st = consoleOut.ToString();

            Assert.Contains("\"level\":\"Information\"", st);
            Assert.Contains("\"service\":\"test\"", st);
            Assert.Contains("\"name\":\"AWS.Lambda.Powertools.Logging.Logger\"", st);
            Assert.Contains("\"message\":\"test\"", st);
        }
        
        [Fact]
        public void When_Setting_LogLevel_Should_Update_LogLevel()
        {
            // Arrange
            var consoleOut = new TestLoggerOutput();;
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
            
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
            var consoleOut = GetConsoleOutput();
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
            
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
            var consoleOut = GetConsoleOutput();
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
            
            // Act
            _testHandlers.TestLogEventWithoutContext();
            
            // Assert
            var st = stringWriter.ToString();
            Assert.Contains("Skipping Event Log because event parameter not found.", st);
            Assert.Contains("Skipping Lambda Context injection because ILambdaContext context parameter not found", st);
           
        }
        
        [Fact]
        public void Should_Log_When_Not_Using_Decorator()
        {
            // Arrange
            var consoleOut = GetConsoleOutput();
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
            
            var test = new TestHandlers();
            
            // Act
            test.TestLogNoDecorator();
    
            // Assert
            consoleOut.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"level\":\"Information\"") && 
                s.Contains("\"service\":\"service_undefined\"") && 
                s.Contains("\"name\":\"AWS.Lambda.Powertools.Logging.Logger\"") && 
                s.Contains("\"message\":\"test\"")
            ));
        }
        
        [Fact]
        public void LoggingAspect_ShouldRespectDynamicLogLevelChanges()
        {
            // Arrange
            var consoleOut = GetConsoleOutput();
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
                options.MinimumLogLevel = LogLevel.Warning;
            });
    
            // Act
            _testHandlers.TestMethodDebug(); // Uses LogLevel.Debug attribute
    
            // Assert
            var st = stringWriter.ToString();
            Assert.Contains("Skipping Lambda Context injection because ILambdaContext context parameter not found", st);
        }
        
        [Fact]
        public void LoggingAspect_ShouldCorrectlyResetLogLevelAfterExecution()
        {
            // Arrange
            var consoleOut = GetConsoleOutput();
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
                options.MinimumLogLevel = LogLevel.Warning;
            });
    
            // Act - First call with Debug level attribute
            _testHandlers.TestMethodDebug();
            consoleOut.ClearReceivedCalls();
    
            // Act - Then log directly at Debug level (should still work)
            Logger.LogDebug("This should be logged");
    
            // Assert
            consoleOut.Received(1).WriteLine(Arg.Is<string>(s => 
                s.Contains("\"level\":\"Debug\"") && 
                s.Contains("\"message\":\"This should be logged\"")));
        }
        
        [Fact]
        public void LoggingAspect_ShouldRespectAttributePrecedenceOverEnvironment()
        {
            // Arrange
            Environment.SetEnvironmentVariable("POWERTOOLS_LOG_LEVEL", "Error");
            var consoleOut = GetConsoleOutput();
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
            });
    
            // Act
            _testHandlers.TestMethodDebug(); // Uses LogLevel.Debug attribute
    
            // Assert
            var st = stringWriter.ToString();
            Assert.Contains("Skipping Lambda Context injection because ILambdaContext context parameter not found", st);
        }
        
        [Fact]
        public void LoggingAspect_ShouldImmediatelyApplyFilterLevelChanges()
        {
            // Arrange
            var consoleOut = GetConsoleOutput();
            
            Logger.Configure(options =>
            {
                options.LogOutput = consoleOut;
                options.MinimumLogLevel = LogLevel.Error;
            });
    
            // Act
            Logger.LogInformation("This should NOT be logged");
            _testHandlers.TestMethodDebug(); // Should change level to Debug
            Logger.LogInformation("This should be logged");
            
            // Assert
            
            consoleOut.Received(1).WriteLine(Arg.Is<string>(s => 
                s.Contains("\"message\":\"This should be logged\"")));
            consoleOut.DidNotReceive().WriteLine(Arg.Is<string>(s => 
                s.Contains("\"message\":\"This should NOT be logged\"")));
        }
        
        public void Dispose()
        {
            ResetAllState();
        }
        
        private IConsoleWrapper GetConsoleOutput()
        {
            // Create a new mock each time
            var output = Substitute.For<IConsoleWrapper>();
            return output;
        }
        
        private void ResetAllState()
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
            LambdaLifecycleTracker.Reset();
        }
    }
}