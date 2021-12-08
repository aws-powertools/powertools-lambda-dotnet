using System;
using AWS.Lambda.PowerTools.Core;
using AWS.Lambda.PowerTools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AWS.Lambda.PowerTools.Logging.Tests
{
    public class PowerToolsLoggerTest
    {
        private void Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel logLevel, LogLevel minimumLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var serviceName = Guid.NewGuid().ToString();

            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowerToolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    ServiceName = serviceName,
                    MinimumLevel = minimumLevel
                });

            switch (logLevel)
            {
                // Act
                case LogLevel.Critical:
                    logger.LogCritical("Test");
                    break;
                case LogLevel.Debug:
                    logger.LogDebug("Test");
                    break;
                case LogLevel.Error:
                    logger.LogError("Test");
                    break;
                case LogLevel.Information:
                    logger.LogInformation("Test");
                    break;
                case LogLevel.Trace:
                    logger.LogTrace("Test");
                    break;
                case LogLevel.Warning:
                    logger.LogWarning("Test");
                    break;
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
            
            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>(s=> s.Contains(serviceName))
                ), Times.Once);
           
        }
        
        private void Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel logLevel, LogLevel minimumLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var serviceName = Guid.NewGuid().ToString();

            var configurations = new Mock<IPowerToolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowerToolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    ServiceName = serviceName,
                    MinimumLevel = minimumLevel
                });

            switch (logLevel)
            {
                // Act
                case LogLevel.Critical:
                    logger.LogCritical("Test");
                    break;
                case LogLevel.Debug:
                    logger.LogDebug("Test");
                    break;
                case LogLevel.Error:
                    logger.LogError("Test");
                    break;
                case LogLevel.Information:
                    logger.LogInformation("Test");
                    break;
                case LogLevel.Trace:
                    logger.LogTrace("Test");
                    break;
                case LogLevel.Warning:
                    logger.LogWarning("Test");
                    break;
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
            
            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.IsAny<string>()
                ), Times.Never);
           
        }
        
        [Theory]
        [InlineData(LogLevel.Trace)]
        public void LogTrace_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Trace, minimumLevel);
        }
        
        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        public void LogDebug_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Debug, minimumLevel);
        }
        
        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        public void LogInformation_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Information, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        public void LogWarning_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Warning, minimumLevel);
        }
        
        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        public void LogError_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Error, minimumLevel);
        }
        
        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogCritical_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Critical, minimumLevel);
        }
        
        
        [Theory]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogTrace_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Trace, minimumLevel);
        }
        
        
        [Theory]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogDebug_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Debug, minimumLevel);
        }
        
        [Theory]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogInformation_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Information, minimumLevel);
        }
        
        [Theory]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogWarning_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Warning, minimumLevel);
        }
        
        [Theory]
        [InlineData(LogLevel.Critical)]
        public void LogError_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Error, minimumLevel);
        }
        
        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogNone_WithAnyMinimumLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.None, minimumLevel);
        }
    }
}