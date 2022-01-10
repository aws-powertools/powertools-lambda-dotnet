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
using System.Globalization;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests
{
    public class PowertoolsLoggerTest
    {
        private void Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel logLevel, LogLevel minimumLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();

            var configurations = new Mock<IPowertoolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = minimumLevel
                });

            switch (logLevel)
            {
                // Act
                case LogLevel.Critical:
                    LoggerExtensions.LogCritical(logger, "Test");
                    break;
                case LogLevel.Debug:
                    LoggerExtensions.LogDebug(logger, "Test");
                    break;
                case LogLevel.Error:
                    LoggerExtensions.LogError(logger, "Test");
                    break;
                case LogLevel.Information:
                    LoggerExtensions.LogInformation(logger, "Test");
                    break;
                case LogLevel.Trace:
                    LoggerExtensions.LogTrace(logger, "Test");
                    break;
                case LogLevel.Warning:
                    LoggerExtensions.LogWarning(logger, "Test");
                    break;
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
            
            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>(s=> s.Contains(service))
                ), Times.Once);
           
        }
        
        private void Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel logLevel, LogLevel minimumLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();

            var configurations = new Mock<IPowertoolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = minimumLevel
                });

            switch (logLevel)
            {
                // Act
                case LogLevel.Critical:
                    LoggerExtensions.LogCritical(logger, "Test");
                    break;
                case LogLevel.Debug:
                    LoggerExtensions.LogDebug(logger, "Test");
                    break;
                case LogLevel.Error:
                    LoggerExtensions.LogError(logger, "Test");
                    break;
                case LogLevel.Information:
                    LoggerExtensions.LogInformation(logger, "Test");
                    break;
                case LogLevel.Trace:
                    LoggerExtensions.LogTrace(logger, "Test");
                    break;
                case LogLevel.Warning:
                    LoggerExtensions.LogWarning(logger, "Test");
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
        
        [Fact]
        public void Log_ConfigurationIsNotProvided_ReadsFromEnvironmentVariables()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            var loggerSampleRate = 0.7;
            var randomSampleRate = 0.5;
           
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerSampleRate).Returns(loggerSampleRate);
            
            var systemWrapper = new Mock<ISystemWrapper>();
            systemWrapper.Setup(c => c.GetRandom()).Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });
            
            LoggerExtensions.LogInformation(logger, "Test");

            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s=> 
                        s.Contains(service) &&
                        s.Contains(loggerSampleRate.ToString(CultureInfo.InvariantCulture))
                    )
                ), Times.Once);
        }

        [Fact]
        public void Log_SamplingRateGreaterThanRandom_ChangedLogLevelToDebug()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            var loggerSampleRate = 0.7;
            var randomSampleRate = 0.5;
           
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerSampleRate).Returns(loggerSampleRate);
            
            var systemWrapper = new Mock<ISystemWrapper>();
            systemWrapper.Setup(c => c.GetRandom()).Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });
            
            LoggerExtensions.LogInformation(logger, "Test");

            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s=> 
                        s == $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {loggerSampleRate}, Sampler Value: {randomSampleRate}."
                    )
                ), Times.Once);
           
        }
        
        [Fact]
        public void Log_SamplingRateGreaterThanOne_SkipsSamplingRateConfiguration()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            var loggerSampleRate = 2;

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerSampleRate).Returns(loggerSampleRate);
            
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });
            
            LoggerExtensions.LogInformation(logger, "Test");

            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s=> 
                        s == $"Skipping sampling rate configuration because of invalid value. Sampling rate: {loggerSampleRate}"
                    )
                ), Times.Once);
           
        }
    }
}