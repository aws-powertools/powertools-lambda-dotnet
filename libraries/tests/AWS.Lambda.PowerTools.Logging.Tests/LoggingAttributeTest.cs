using System;
using System.Linq;
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
            var serviceName = Guid.NewGuid().ToString();
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
            
            var handler = new LoggingAspectHandler(serviceName, logLevel, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);

            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey("ColdStart"));
            Assert.True((bool) allKeys["ColdStart"]);
            Assert.True(allKeys.ContainsKey("FunctionName"));
            Assert.Equal(allKeys["FunctionName"], lambdaContext.Object.FunctionName);
            Assert.True(allKeys.ContainsKey("FunctionVersion"));
            Assert.Equal(allKeys["FunctionVersion"], lambdaContext.Object.FunctionVersion);
            Assert.True(allKeys.ContainsKey("FunctionMemorySize"));
            Assert.Equal(allKeys["FunctionMemorySize"], lambdaContext.Object.MemoryLimitInMB);
            Assert.True(allKeys.ContainsKey("FunctionArn"));
            Assert.Equal(allKeys["FunctionArn"], lambdaContext.Object.InvokedFunctionArn);
            Assert.True(allKeys.ContainsKey("FunctionRequestId"));
            Assert.Equal(allKeys["FunctionRequestId"], lambdaContext.Object.AwsRequestId);
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
            var serviceName = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };
            
            var handler = new LoggingAspectHandler(serviceName, logLevel, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);

            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey("ColdStart"));
            Assert.True((bool) allKeys["ColdStart"]);
            Assert.False(allKeys.ContainsKey("FunctionName"));
            Assert.False(allKeys.ContainsKey("FunctionVersion"));
            Assert.False(allKeys.ContainsKey("FunctionMemorySize")); ;
            Assert.False(allKeys.ContainsKey("FunctionArn"));
            Assert.False(allKeys.ContainsKey("FunctionRequestId"));
            
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
            var serviceName = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };
            
            var handler = new LoggingAspectHandler(serviceName, logLevel, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);

            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey("ColdStart"));
            Assert.True((bool) allKeys["ColdStart"]);
            Assert.False(allKeys.ContainsKey("FunctionName"));
            Assert.False(allKeys.ContainsKey("FunctionVersion"));
            Assert.False(allKeys.ContainsKey("FunctionMemorySize")); ;
            Assert.False(allKeys.ContainsKey("FunctionArn"));
            Assert.False(allKeys.ContainsKey("FunctionRequestId"));
            
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
            var serviceName = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };
            
            var handler = new LoggingAspectHandler(serviceName, logLevel, null, true, configurations.Object,
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
            var serviceName = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };
            
            var handler = new LoggingAspectHandler(serviceName, logLevel, null, true, configurations.Object,
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
            var serviceName = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            
            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();
            
            var eventArgs = new AspectEventArgs
            {
                Name = methodName,
                Args = new object [] { }
            };
            
            var handler = new LoggingAspectHandler(serviceName, logLevel, null, true, configurations.Object,
                systemWrapper.Object);
            
            handler.ResetForTest();

            // Act
            handler.OnEntry(eventArgs);
            
            var allKeys = Logger.GetAllKeys()
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.True(allKeys.ContainsKey("ColdStart"));
            Assert.True((bool) allKeys["ColdStart"]);
            
            handler.OnExit(eventArgs);
            
            Assert.NotNull(Logger.LoggerProvider);
            Assert.False(Logger.GetAllKeys().Any());
        }
    }
}